using Godot;

/// <summary>
/// Viewportのレイヤー表示状態を保持し、表示状態の変更を通知する。
/// </summary>
public partial class ViewportDisplayHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>有効なビューポートレイヤーのマスクを取得する。</summary>
    public uint ActiveLayers { get; private set; } = (uint)ViewportLayer.Default | (uint)ViewportLayer.VisibleEntity | (uint)ViewportLayer.VisibleProperty;

    #endregion

    #region Signals

    /// <summary>レイヤー表示状態の変更通知。</summary>
    [Signal] public delegate void ActiveLayersNotifiedEventHandler();

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// レイヤー表示状態を更新して通知する。
    /// </summary>
    /// <param name="layer">状態を変更するレイヤー</param>
    /// <param name="isActive">有効にする場合は <see langword="true"/>、無効にする場合は <see langword="false"/></param>
    public void SetLayerActive(uint layer, bool isActive)
    {
        bool stateMatchesRequested = isActive
            ? (ActiveLayers & layer) == layer
            : (ActiveLayers & layer) == 0;
        if (stateMatchesRequested)
        {
            return;
        }

        if (isActive)
        {
            ActiveLayers |= layer;
        }
        else
        {
            ActiveLayers &= ~layer;
        }

        EmitSignal(SignalName.ActiveLayersNotified);
    }

    #endregion

    #region Helpers

    #endregion
}
