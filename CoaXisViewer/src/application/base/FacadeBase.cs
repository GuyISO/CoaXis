using Godot;

/// <summary>
/// Domain Facade で共通利用するモジュール生成ヘルパー
/// </summary>
public abstract partial class FacadeBase : Node
{
    #region Internal Helpers

    /// <summary>
    /// 指定された型のモジュールを追加する。すでに存在する場合は既存のモジュールを返す。
    /// </summary>
    /// <typeparam name="TModule">追加するモジュールの型。</typeparam>
    /// <param name="nodeName">モジュールのノード名。</param>
    /// <returns>追加された、もしくは既存のモジュール。</returns>
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