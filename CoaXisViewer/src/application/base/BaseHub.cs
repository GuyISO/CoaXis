using Godot;

/// <summary>
/// Application を含む AutoLoad モジュールで共通利用する基底クラス。
/// 階層的なモジュール生成を補助する。
/// </summary>
public abstract partial class BaseHub : Node
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Actions

    #endregion

    #region Notifications

    #endregion

    #region Events

    #endregion

    #region Methods

    #endregion

    #region Helpers

    /// <summary>
    /// 指定された型のモジュールを追加する。すでに存在する場合は既存のモジュールを返す。
    /// </summary>
    /// <typeparam name="TModule">追加するモジュールの型。</typeparam>
    /// <param name="nodeName">モジュールのノード名。</param>
    /// <returns>追加したモジュール、または既存のモジュール。</returns>
    protected TModule AddModule<TModule>(string nodeName) where TModule : Node, new()
    {
        TModule existingModule = GetNodeOrNull<TModule>(nodeName);
        if (existingModule != null)
        {
            return existingModule;
        }

        TModule module = new TModule
        {
            Name = nodeName
        };

        AddChild(module);
        return module;
    }

    #endregion
}