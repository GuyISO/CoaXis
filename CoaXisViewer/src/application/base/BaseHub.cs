using Godot;

/// <summary>
/// Application を含む AutoLoad モジュールの共通機能と階層的な生成処理を提供する基底ハブ。
/// </summary>
public abstract partial class BaseHub : Node
{
    #region Fields

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Signals

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