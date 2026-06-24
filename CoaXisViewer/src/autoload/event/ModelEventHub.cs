using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル関連のイベント集約ハブです。AutoLoadノードとしてシーンツリーに配置し、モデルの状態変更や操作のリクエストを通知するためのシグナルを提供します。これにより、モデル操作のロジックを分散させずに一元管理できます。
/// Autoloadに登録してシングルトン参照することを前提としています。
/// </summary>
public partial class ModelEventHub : Node
{
	/// <summary>
	/// シングルトン参照です。
	/// </summary>
	public static ModelEventHub I { get; private set; }

	/// <summary>
	/// シーンツリー参加時にシングルトン参照を確立します。
	/// </summary>
	public override void _EnterTree()
	{
		// AutoLoad をデフォルト参照として維持するため、未設定時のみ I を確立する。
		if (I == null)
		{
			I = this;
		}
	}

	/// <summary>
	/// シーンツリー離脱時に、現在インスタンスがシングルトン参照なら解放します。
	/// </summary>
	public override void _ExitTree()
	{
		// 複数インスタンスが存在し得るため、自身が I の場合のみ解放する。
		if (ReferenceEquals(I, this))
		{
			I = null;
		}
	}

	#region --------------------------------------- Request ---------------------------------------

	[Signal] public delegate void SetMultiSelectModeRequestedEventHandler(bool enable);
	/// <summary>
	/// 複数選択モードの設定をリクエストします。
	/// </summary>
	/// <param name="enable">複数選択モードを有効にする場合はtrue、無効にする場合はfalseです。</param>
	public static void RequestSetMultiSelectMode(bool enable)
	{
		I.EmitSignal(SignalName.SetMultiSelectModeRequested, enable);
	}

	[Signal] public delegate void AddModelRequestedEventHandler(Node3D node, Node3D parent);
	/// <summary>
	/// モデルの追加をリクエストします。
	/// </summary>
	/// <param name="node">追加するノードです。</param>
	/// <param name="parent">追加先の親ノードです。nullの場合はルートに追加されます。</param>
	public static void RequestAddModel(Node3D node, Node3D parent = null)
	{
		I.EmitSignal(SignalName.AddModelRequested, node, parent);
	}

	[Signal] public delegate void SelectModelRequestedEventHandler(Node3D node);
	/// <summary>
	/// モデルの選択をリクエストします。
	/// </summary>
	/// <param name="node">選択するノードです。</param>
	public static void RequestSelectModel(Node3D node)
	{
		I.EmitSignal(SignalName.SelectModelRequested, node);
	}

	[Signal] public delegate void DeselectModelRequestedEventHandler(Node3D node);
	/// <summary>
	/// モデルの選択解除をリクエストします。
	/// </summary>
	/// <param name="node">選択解除するノードです。</param>
	public static void RequestDeselectModel(Node3D node)
	{
		I.EmitSignal(SignalName.DeselectModelRequested, node);
	}

	#endregion

	#region --------------------------------------- Notification ---------------------------------------



	#endregion
}