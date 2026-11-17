using System;
using Godot;

/// <summary>
/// ModelProperty 関連モジュールの構成を管理するハブ。
/// </summary>
public partial class ModelPropertyHub : BaseHub
{
    #region Fields

    #endregion

    #region Properties

    /// <summary>ModelProperty の識別子と階層を管理するレジストリを取得する。</summary>
    public ModelPropertyRegistryHub Registry { get; }
    /// <summary>ModelProperty の論理状態の変更通知を取得する。</summary>
    public ModelPropertyStateHub State { get; }
    /// <summary>ModelProperty の実効表示と描画反映を取得する。</summary>
    public ModelPropertyVisualHub Visual { get; }
    /// <summary>ModelProperty の選択機能を取得する。</summary>
    public ModelPropertySelectionHub Selection { get; }
    /// <summary>ModelProperty のロード機能を取得する。</summary>
    public ModelPropertyLoadHub Load { get; }

    #endregion

    #region Signals

    /// <summary>指定Propertyに対するManipulate操作の通知。</summary>
    /// <param name="propertyId">操作対象のProperty識別子</param>
    [Signal] public delegate void ManipulatedEventHandler(string propertyId);

    #endregion

    #region Lifecycle

    /// <summary>
    /// ModelProperty 機能を担当するモジュールを初期化する。
    /// </summary>
    public ModelPropertyHub()
    {
        Registry = AddModule<ModelPropertyRegistryHub>("Registry");
        State = AddModule<ModelPropertyStateHub>("State");
        Visual = AddModule<ModelPropertyVisualHub>("Visual");
        Selection = AddModule<ModelPropertySelectionHub>("Selection");
        Load = AddModule<ModelPropertyLoadHub>("Load");
    }

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 指定Propertyに対するManipulate操作を通知する。
    /// </summary>
    /// <param name="propertyId">操作対象のProperty識別子</param>
    public void Manipulate(Guid propertyId)
    {
        if (propertyId == Guid.Empty || Registry.Get(propertyId) == null)
        {
            Application.Log.Warn($"ModelProperty: manipulate target not found. propertyId='{propertyId}'");
            return;
        }

        EmitSignal(SignalName.Manipulated, propertyId.ToString());
    }

    #endregion

    #region Helpers

    #endregion
}