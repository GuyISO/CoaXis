using Godot;

/// <summary>モデルの共通選択モード。</summary>
public enum ModelSelectionMode
{
    /// <summary>対象のみを選択する。</summary>
    Set,
    /// <summary>対象を追加する。</summary>
    Add,
    /// <summary>対象を解除する。</summary>
    Remove,
    /// <summary>対象の選択状態を切り替える。</summary>
    Toggle,
}

/// <summary>モデルの共通選択モードと種別間の選択切り替えを管理する。</summary>
public partial class ModelSelectionHub : BaseHub
{
    #region Fields
    #endregion

    #region Properties
    /// <summary>現在の選択モード。</summary>
    public ModelSelectionMode CurrentMode { get; private set; } = ModelSelectionMode.Set;
    #endregion

    #region Signals
    /// <summary>選択モードの変更通知。</summary>
    [Signal] public delegate void ModeNotifiedEventHandler();
    #endregion

    #region Lifecycle
    #endregion

    #region Events
    #endregion

    #region Methods
    /// <summary>選択モードを設定する。</summary>
    /// <param name="mode">設定する選択モード</param>
    public void SetMode(ModelSelectionMode mode)
    {
        if (CurrentMode != mode)
        {
            CurrentMode = mode;
            Application.Log.Debug($"SelectionHub: Selection mode changed to {CurrentMode}.");
        }

        EmitSignal(SignalName.ModeNotified);
    }

    /// <summary>EntityとPropertyの選択をすべて解除する。</summary>
    public void Clear()
    {
        Application.Model.Entity.Selection.Clear();
        Application.Model.Property.Selection.Clear();
    }

    /// <summary>Entityを追加選択する前にPropertyの選択を解除する。</summary>
    internal void PrepareEntitySelection()
    {
        Application.Model.Property.Selection.Clear();
    }

    /// <summary>Propertyを追加選択する前にEntityの選択を解除する。</summary>
    internal void PreparePropertySelection()
    {
        Application.Model.Entity.Selection.Clear();
    }
    #endregion

    #region Helpers
    #endregion
}
