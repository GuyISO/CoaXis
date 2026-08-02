using System;
using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// モデルのインスタンスとUUIDを管理する Autoload ノード
/// </summary>
public partial class ModelRegistry : Node
{
    #region Fields

    // Guid をキーにして ModelData を管理する辞書
    private readonly Dictionary<Guid, ModelData> _dataSet = new();

    #endregion

    #region Properties

    // シングルトンインスタンス
    public static ModelRegistry Instance { get; } = new ModelRegistry();

    // 登録されている ModelData の集合を取得する
    public IReadOnlyDictionary<Guid, ModelData> DataSet => _dataSet;

    #endregion

    #region Public Methods

    public ModelData GetModelData(Guid id)
    {
        return _dataSet.TryGetValue(id, out ModelData modelData) ? modelData : null;
    }

    public bool IsRegistered(Guid id)
    {
        return _dataSet.ContainsKey(id);
    }

    /// <summary>
    /// ModelData を登録する。親が未登録の場合は、親が登録されるまで待機する。
    /// すでに同じ Id の ModelData が登録されている場合は、既存の ModelData を置き換える。
    /// </summary>
    /// <param name="modelData"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    public void Register(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (modelData.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelData id must not be empty.", nameof(modelData));
        }

        if (modelData.Status != ModelStatus.Initialized)
        {
            throw new ArgumentException("ModelData must be in Initialized status.", nameof(modelData));
        }

        if (IsRegistered(modelData.Id))
        {
            // すでに登録されている場合は、既存の ModelData を置き換える
            Dispose(modelData.Id);
        }

        // レジストリに登録する
        _dataSet.Add(modelData.Id, modelData);
        modelData.Status = ModelStatus.Registered;
    }

    public bool Dispose(Guid id)
    {
        if (!_dataSet.TryGetValue(id, out ModelData modelData))
        {
            return false;
        }

        // 子孫の ModelData から先に削除する
        foreach (ModelData child in modelData.Children.ToList())
        {
            modelData.DetachChild(child);
        }

        // 親からの参照に登録済みの場合は解除する
        ModelData parent = GetModelData(modelData.ParentId);
        parent?.DetachChild(modelData);

        // レジストリから削除する
        _dataSet.Remove(id);
        modelData.Status = ModelStatus.Disposed;

        return true;
    }

    /// <summary>
    /// ModelData の状態を解決する。
    /// </summary>
    public void ResolveHierarchy()
    {
        foreach (var modelData in _dataSet.Values)
        {
            if (modelData.Status == ModelStatus.Registered)
            {
                LinkToRegisteredParent(modelData);
            }
        }
    }

    public void Clear()
    {
        _dataSet.Clear();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// 登録済みの親 ModelData にリンクする。親が未登録の場合は、親が登録されるまで待機する。
    /// </summary>
    /// <param name="modelData">リンクする ModelData</param>
    private void LinkToRegisteredParent(ModelData modelData)
    {
        if (modelData.ParentId == Guid.Empty)
        {
            return;
        }

        if (_dataSet.TryGetValue(modelData.ParentId, out ModelData parent))
        {
            parent.AttachChild(modelData);
            return;
        }
    }

    #endregion
}
