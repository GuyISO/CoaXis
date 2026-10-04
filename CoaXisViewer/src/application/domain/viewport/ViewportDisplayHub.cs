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
    internal uint ActiveLayers { get; private set; } = (uint)ViewportLayer.Default | (uint)ViewportLayer.Visible;

    #endregion

    #region Lifecycle

    #endregion

    #region Events

    [Signal] public delegate void LayerActivatedEventHandler(uint layer, bool isActive);
    /// <summary>
    /// レイヤー表示状態の変更通知。
    /// </summary>
    /// <param name="layer">状態を変更したレイヤー</param>
    /// <param name="isActive">レイヤーが有効な場合は <see langword="true"/></param>
    private void NotifyLayerActivated(uint layer, bool isActive)
    {
        EmitSignal(SignalName.LayerActivated, layer, isActive);
    }

    #endregion

    #region Methods

    /// <summary>
    /// レイヤー表示状態を更新して通知する。
    /// </summary>
    /// <param name="layer">状態を変更するレイヤー</param>
    /// <param name="isActive">有効にする場合は <see langword="true"/>、無効にする場合は <see langword="false"/></param>
    internal void SetLayerActive(uint layer, bool isActive)
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

        NotifyLayerActivated(layer, isActive);
    }

    #endregion

    #region Helpers

    #endregion
}
