using System;
using System.Collections.Generic;
using System.Linq;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>
/// ModelPropertyの生成と登録を管理するハブ。
/// </summary>
public partial class ModelPropertyLoadHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Signals

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        Application.Model.Entity.State.StatusNotified += OnEntityStatusNotified;
    }

    public override void _ExitTree()
    {
        Application.Model.Entity.State.StatusNotified -= OnEntityStatusNotified;

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// Entityのシーンロード完了通知を受け、AABBが確定したため所属Propertyのパーティクルを再配置する。
    /// </summary>
    /// <param name="entityId">状態が変化したEntity識別子</param>
    private void OnEntityStatusNotified(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId))
        {
            return;
        }

        ModelEntity entity = Application.Model.Entity.Registry.Get(parsedEntityId);
        if (entity == null || entity.Status != ModelStatus.Loaded)
        {
            return;
        }

        var createdIds = new HashSet<Guid>();
        SyncParticles(createdIds);
        foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
        {
            // 新規生成分は Bind 時に反映済み
            if (property.GetEntity() == entity && !createdIds.Contains(property.Id))
            {
                property.ParticleNode?.Refresh();
            }
        }
    }

    #endregion

    #region Methods

    /// <summary>
    /// 検証済みPropertyを追加し、同じ識別子がある場合は置換する。
    /// </summary>
    /// <param name="properties">追加または置換するProperty</param>
    internal void AddOrReplacePrepared(IReadOnlyList<ModelProperty> properties)
    {
        RegisterPrepared(properties);
        Application.Model.Property.Registry.ResolveHierarchy();
        SyncParticles();
    }

    /// <summary>
    /// 登録済みPropertyのパーティクルを所属Entityの描画Nodeへ同期する。
    /// </summary>
    /// <remarks>
    /// Entityの置換でNodeが作り直された場合も、現在のNode配下へ再生成する。
    /// </remarks>
    /// <param name="createdIds">指定時、今回新規生成したパーティクルのProperty識別子を追加する</param>
    internal void SyncParticles(ISet<Guid> createdIds = null)
    {
        foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
        {
            ModelEntity entity = property.GetEntity();
            ModelNode entityNode = entity?.Node;
            bool canAttach = property.ParticleEmitterType != ModelParticleEmitterType.None
                && entity != null
                && entity.Status == ModelStatus.Loaded
                && entityNode != null
                && GodotObject.IsInstanceValid(entityNode)
                && Application.Model.Entity.Registry.IsRootConnected(entity.Id);

            ModelParticle existing = property.ParticleNode;
            if (existing != null && (!canAttach || existing.GetParent() != entityNode))
            {
                property.DetachParticle();
                existing = null;
            }

            if (!canAttach || existing != null)
            {
                continue;
            }

            ModelParticle particle = ModelParticle.Create(
                property.ParticleEmitterType,
                property.ParticleBehaviorType);
            if (particle == null)
            {
                continue;
            }

            particle.Name = property.Id.ToString();
            entityNode.AddChild(particle);
            particle.Bind(property.Id);
            property.AttachParticle(particle);
            createdIds?.Add(property.Id);
            Application.Model.Property.Visual.ResolveAndApply(property);
        }
    }

    /// <summary>
    /// 指定Propertyとその子孫を削除する。
    /// </summary>
    /// <param name="propertyIds">削除対象のProperty識別子</param>
    /// <returns>削除したProperty識別子</returns>
    internal IReadOnlyCollection<Guid> DeleteCascade(IReadOnlyCollection<Guid> propertyIds)
    {
        if (propertyIds == null)
        {
            throw new ArgumentNullException(nameof(propertyIds));
        }

        var idsToDelete = new HashSet<Guid>(propertyIds);
        bool added;
        do
        {
            added = false;
            foreach (ModelProperty property in Application.Model.Property.Registry.Items.Values)
            {
                if (idsToDelete.Contains(property.ParentId) && idsToDelete.Add(property.Id))
                {
                    added = true;
                }
            }
        }
        while (added);

        foreach (Guid propertyId in idsToDelete.OrderByDescending(GetDepth))
        {
            Application.Model.Property.Registry.Dispose(propertyId);
        }

        Application.Model.Property.Registry.ResolveHierarchy();
        return idsToDelete;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定Propertyの親階層の深さを取得する。
    /// </summary>
    private static int GetDepth(Guid propertyId)
    {
        int depth = 0;
        ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
        var visited = new HashSet<Guid>();
        while (property != null && property.ParentId != Guid.Empty && visited.Add(property.Id))
        {
            depth++;
            property = Application.Model.Property.Registry.Get(property.ParentId);
        }

        return depth;
    }

    /// <summary>
    /// 事前生成済みのPropertyをRegistryへ登録する。
    /// </summary>
    /// <param name="properties">登録する検証済みモデル属性</param>
    private void RegisterPrepared(IReadOnlyList<ModelProperty> properties)
    {
        if (properties == null)
        {
            throw new ArgumentNullException(nameof(properties));
        }

        // 全件を登録してから階層を解決し、入力順に依存せず親子関係を確定する。
        foreach (ModelProperty property in properties)
        {
            if (property == null)
            {
                throw new ArgumentException("ModelProperty must not be null.", nameof(properties));
            }

            Application.Model.Property.Registry.Register(property);
        }
    }

    #endregion
}
