using Godot;

/// <summary>
/// コマンドの基底クラス、Undo/Redo 操作の単位となる
/// </summary>
public abstract partial class CommandBase : RefCounted
{
    #region Properties

    /// <summary>
    /// コマンドの説明、ログ出力時に使用される
    /// </summary>
    public abstract string Description { get; }

    #endregion

    #region Public Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public virtual void Do() { }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public virtual void Undo() { }

    #endregion

    #region Internal Helpers

    protected void LogDo(string detail = "")
    {
        LogWithDescription("Do", detail);
    }

    /// <summary>
    /// Undo 操作時にログ出力する
    /// </summary>
    /// <param name="detail">詳細情報</param>
    protected void LogUndo(string detail = "")
    {
        LogWithDescription("Undo", detail);
    }

    /// <summary>
    /// 指定された操作がスキップされたことをログ出力する
    /// </summary>
    /// <param name="operation">スキップされた操作の名前</param>
    /// <param name="reason">スキップの理由</param>
    protected void LogSkip(string operation, string reason)
    {
        string message = $"{GetType().Name}.{operation} skipped: {reason}";
        Application.Log.Warn(message);
    }

    /// <summary>
    /// 指定された操作の説明をログ出力する
    /// </summary>
    /// <param name="operation">操作の名前</param>
    /// <param name="detail">詳細情報</param>
    private void LogWithDescription(string operation, string detail)
    {
        string message = string.IsNullOrWhiteSpace(detail)
            ? $"{GetType().Name}.{operation}: {Description}"
            : $"{GetType().Name}.{operation}: {Description}, {detail}";
        Application.Log.Debug(message);
    }

    #endregion
}
