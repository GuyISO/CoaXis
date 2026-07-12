using Godot;

/// <summary>
/// Domain Facade で共通利用するモジュール生成ヘルパー
/// </summary>
public abstract partial class FacadeBase : Node
{
    /// <summary>
    /// 指定型の子ノードを生成して登録する
    /// </summary>
    /// <typeparam name="TModule">生成するモジュール型</typeparam>
    /// <param name="nodeName">生成ノード名</param>
    /// <returns>生成して登録した子ノード</returns>
    protected TModule AddModule<TModule>(string nodeName) where TModule : Node, new()
    {
        TModule module = new TModule
        {
            Name = nodeName
        };

        AddChild(module);
        return module;
    }
}