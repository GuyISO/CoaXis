using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// ModelEntityに紐づくModelPropertyTreeをビューポート上に表示する。
/// </summary>
public partial class ViewportPropertyOverlay : Control
{
    #region Fields

    private readonly PackedScene _modelPropertyTreeScene = ResourceLoader.Load<PackedScene>("res://scenes/model_property_tree.tscn");
    private readonly Dictionary<Guid, ModelPropertyTree> _propertyTrees = new();
    private readonly Dictionary<Guid, Vector3> _localAabbCenters = new();

    #endregion

    #region Lifecycle

    /// <summary>
    /// 表示要求、カメラ状態、ModelEntity状態の変更を購読する。
    /// </summary>
    public override void _Ready()
    {
        SubscribeEvents();
    }

    /// <summary>
    /// 購読中のイベントを解除し、生成済みツリーの参照を解放する。
    /// </summary>
    public override void _ExitTree()
    {
        UnsubscribeEvents();
        _propertyTrees.Clear();
        _localAabbCenters.Clear();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 表示要求と表示位置の再計算が必要なイベントの購読を開始する。
    /// </summary>
    private void SubscribeEvents()
    {
        Resized += OnResized;
        Application.Ui.Property.ShowRequested += OnShowRequested;
        Application.Ui.Property.HideRequested += OnHideRequested;
        Application.Viewport.Camera.PositionNotified += OnCameraChanged;
        Application.Viewport.Camera.RotationNotified += OnCameraChanged;
        Application.Viewport.Camera.DistanceNotified += OnCameraChanged;
        Application.Viewport.Camera.SizeNotified += OnCameraChanged;
        Application.Viewport.Camera.FovNotified += OnCameraChanged;
        Application.Viewport.Camera.ProjectionTypeNotified += OnCameraChanged;
        Application.Viewport.Camera.CameraNearNotified += OnCameraChanged;
        Application.Viewport.Camera.CameraFarNotified += OnCameraChanged;
        Application.Model.Entity.State.PositionNotified += OnEntityPositionNotified;
        Application.Model.Entity.State.RotationNotified += OnEntityRotationNotified;
        Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
        Application.Model.Entity.State.StatusNotified += OnEntityStatusNotified;
    }

    /// <summary>
    /// 表示要求と表示位置の再計算が必要なイベントの購読を解除する。
    /// </summary>
    private void UnsubscribeEvents()
    {
        Resized -= OnResized;
        Application.Ui.Property.ShowRequested -= OnShowRequested;
        Application.Ui.Property.HideRequested -= OnHideRequested;
        Application.Viewport.Camera.PositionNotified -= OnCameraChanged;
        Application.Viewport.Camera.RotationNotified -= OnCameraChanged;
        Application.Viewport.Camera.DistanceNotified -= OnCameraChanged;
        Application.Viewport.Camera.SizeNotified -= OnCameraChanged;
        Application.Viewport.Camera.FovNotified -= OnCameraChanged;
        Application.Viewport.Camera.ProjectionTypeNotified -= OnCameraChanged;
        Application.Viewport.Camera.CameraNearNotified -= OnCameraChanged;
        Application.Viewport.Camera.CameraFarNotified -= OnCameraChanged;
        Application.Model.Entity.State.PositionNotified -= OnEntityPositionNotified;
        Application.Model.Entity.State.RotationNotified -= OnEntityRotationNotified;
        Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
        Application.Model.Entity.State.StatusNotified -= OnEntityStatusNotified;
    }

    /// <summary>
    /// ModelEntityのプロパティ表示要求を受け、対応するツリーを表示する。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    private void OnShowRequested(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            Application.Log.Warn($"ViewportPropertyOverlay: invalid property display target. entityId='{entityId}'");
            return;
        }

        ShowPropertyTree(parsedEntityId);
    }

    /// <summary>
    /// ModelEntityのプロパティ非表示要求を受け、対応するツリーを破棄する。
    /// </summary>
    /// <param name="entityId">非表示対象のModelEntity識別子。</param>
    private void OnHideRequested(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            Application.Log.Warn($"ViewportPropertyOverlay: invalid property hide target. entityId='{entityId}'");
            return;
        }

        HidePropertyTree(parsedEntityId);
    }

    /// <summary>
    /// オーバーレイの表示領域が変化したとき、すべてのツリー位置を再計算する。
    /// </summary>
    private void OnResized()
    {
        UpdateAllTreePositions();
    }

    /// <summary>
    /// カメラ状態が変化したとき、すべてのツリー位置を再計算する。
    /// </summary>
    private void OnCameraChanged()
    {
        UpdateAllTreePositions();
    }

    /// <summary>
    /// ModelEntityの位置が変化したとき、すべてのツリー位置を再計算する。
    /// </summary>
    /// <param name="entityId">位置が変化したModelEntity識別子。</param>
    private void OnEntityPositionNotified(string entityId)
    {
        // 親Entityの移動は子Entityのグローバル座標にも影響するため、表示済み全Treeを更新する。
        UpdateAllTreePositions();
    }

    /// <summary>
    /// ModelEntityの回転が変化したとき、すべてのツリー位置を再計算する。
    /// </summary>
    /// <param name="entityId">回転が変化したModelEntity識別子。</param>
    private void OnEntityRotationNotified(string entityId)
    {
        // 親Entityの回転は子Entityのグローバル座標にも影響するため、表示済み全Treeを更新する。
        UpdateAllTreePositions();
    }

    /// <summary>
    /// ModelEntityの実効可視性が更新されたとき、対応するツリー表示を更新する。
    /// </summary>
    /// <param name="entityId">可視性が更新されたModelEntity識別子。</param>
    private void OnEntityVisibilityResolved(string entityId)
    {
        UpdateTreePositionByEntityId(entityId);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定したModelEntityのModelPropertyTreeを生成または再利用して表示する。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    private void ShowPropertyTree(Guid entityId)
    {
        ModelEntity entity = Application.Model.Entity.Registry.Get(entityId);
        if (entity == null)
        {
            Application.Log.Warn($"ViewportPropertyOverlay: property display target not found. entityId='{entityId}'");
            return;
        }

        if (!_propertyTrees.TryGetValue(entityId, out ModelPropertyTree propertyTree)
            || !GodotObject.IsInstanceValid(propertyTree))
        {
            propertyTree = CreatePropertyTree(entityId);
            if (propertyTree == null)
            {
                return;
            }

            _propertyTrees[entityId] = propertyTree;
        }

        propertyTree.Show(entityId);
        UpdateTreePosition(entityId, propertyTree);
    }

    /// <summary>
    /// 指定したModelEntityに対応するModelPropertyTreeを破棄する。
    /// </summary>
    /// <param name="entityId">非表示対象のModelEntity識別子。</param>
    private void HidePropertyTree(Guid entityId)
    {
        if (!_propertyTrees.TryGetValue(entityId, out ModelPropertyTree propertyTree))
        {
            return;
        }

        _propertyTrees.Remove(entityId);
        _localAabbCenters.Remove(entityId);
        if (GodotObject.IsInstanceValid(propertyTree))
        {
            propertyTree.QueueFree();
        }
    }

    /// <summary>
    /// 生成済みのすべてのModelPropertyTree位置を再計算する。
    /// </summary>
    private void UpdateAllTreePositions()
    {
        foreach ((Guid entityId, ModelPropertyTree propertyTree) in _propertyTrees)
        {
            if (GodotObject.IsInstanceValid(propertyTree))
            {
                UpdateTreePosition(entityId, propertyTree);
            }
        }
    }

    /// <summary>
    /// 文字列で通知されたModelEntity識別子に対応するツリー位置を再計算する。
    /// </summary>
    /// <param name="entityId">位置を再計算するModelEntity識別子。</param>
    private void UpdateTreePositionByEntityId(string entityId)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId)
            || !_propertyTrees.TryGetValue(parsedEntityId, out ModelPropertyTree propertyTree)
            || !GodotObject.IsInstanceValid(propertyTree))
        {
            return;
        }

        UpdateTreePosition(parsedEntityId, propertyTree);
    }

    /// <summary>
    /// ModelEntityの位置をスクリーン座標へ投影して、対応するツリー表示を更新する。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    /// <param name="propertyTree">更新対象のModelPropertyTree。</param>
    private void UpdateTreePosition(Guid entityId, ModelPropertyTree propertyTree)
    {
        ModelEntity entity = Application.Model.Entity.Registry.Get(entityId);
        ModelNode modelNode = entity?.Node;
        if (entity == null || modelNode == null || !GodotObject.IsInstanceValid(modelNode)
            || !entity.IsVisible)
        {
            propertyTree.Hide();
            return;
        }

        Vector3 anchor = modelNode.GlobalTransform * GetLocalAabbCenter(entityId, modelNode);

        if (!Application.Viewport.Camera.TryProjectToScreen(anchor, Size, out Vector2 screenPosition))
        {
            propertyTree.Hide();
            return;
        }

        propertyTree.Position = screenPosition;
        propertyTree.Show();
    }

    /// <summary>
    /// ModelNodeローカル空間のAABB中心を返す。メッシュ走査は高コストなためEntityごとにキャッシュし、移動・回転・カメラ変化ではグローバル変換のみ再適用する。
    /// </summary>
    /// <param name="entityId">対象のModelEntity識別子。</param>
    /// <param name="modelNode">対象のModelNode。</param>
    /// <returns>ModelNodeローカル空間のAABB中心。メッシュが無い場合は原点。</returns>
    private Vector3 GetLocalAabbCenter(Guid entityId, ModelNode modelNode)
    {
        if (_localAabbCenters.TryGetValue(entityId, out Vector3 cached))
        {
            return cached;
        }

        Vector3 localCenter = WorldAabbUtility.TryGetWorldAabb(new[] { modelNode }, out Aabb worldAabb)
            ? modelNode.ToLocal(worldAabb.GetCenter())
            : Vector3.Zero;

        // シーン読み込み前はメッシュが無く原点フォールバックとなるため、キャッシュせず次回再計算する。
        if (worldAabb.Size != Vector3.Zero)
        {
            _localAabbCenters[entityId] = localCenter;
        }

        return localCenter;
    }

    /// <summary>
    /// ModelEntityの状態（ロード完了など）が変化したとき、AABB中心キャッシュを破棄して位置を再計算する。
    /// </summary>
    /// <param name="entityId">状態が変化したModelEntity識別子。</param>
    private void OnEntityStatusNotified(string entityId)
    {
        if (Guid.TryParse(entityId, out Guid parsedEntityId))
        {
            _localAabbCenters.Remove(parsedEntityId);
        }

        UpdateTreePositionByEntityId(entityId);
    }

    /// <summary>
    /// ModelPropertyTreeをインスタンス化してOverlayの直接子に追加する。
    /// </summary>
    /// <param name="entityId">表示対象のModelEntity識別子。</param>
    /// <returns>生成したModelPropertyTree。シーンを読み込めない場合は null。</returns>
    private ModelPropertyTree CreatePropertyTree(Guid entityId)
    {
        if (_modelPropertyTreeScene == null)
        {
            Application.Log.Error("ViewportPropertyOverlay: ModelPropertyTree scene could not be loaded.");
            return null;
        }

        ModelPropertyTree propertyTree = _modelPropertyTreeScene.Instantiate<ModelPropertyTree>();
        propertyTree.Name = $"ModelPropertyTree-{entityId}";
        AddChild(propertyTree);
        return propertyTree;
    }

    #endregion
}
