using Godot;
using System;

/// <summary>
/// ModelEntity を操作対象とするコンテキストメニュー
/// </summary>
/// <remarks>
/// UIコンポーネント（Tree/Viewportなど）ごとにメニューを作る方針から、操作対象オブジェクト（ModelEntity）単位でメニューを作る方針へ変更したもの
/// 呼び出し側は TreeItem や PickResult から ModelEntity の識別子(Guid)を解決してから ShowForEntity を呼び出す
/// </remarks>
public partial class ModelEntityMenu : PopupMenu
{
	/// <summary>
	/// コンテキストメニューの項目を識別する ID
	/// </summary>
	private enum MenuItemId
	{
		Fit,
		Emphasize,
		Spin,
		TreeCentering,
	}

	#region Fields

	// UIコンポーネントに依存せず ModelEntity の識別子のみを対象として保持する
	private Guid _targetEntityId = Guid.Empty;

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		InitializeItems();
		IdPressed += OnIdPressed;
	}

	public override void _ExitTree()
	{
		IdPressed -= OnIdPressed;

		base._ExitTree();
	}

	#endregion

	#region Public Methods

	/// <summary>
	/// 指定した ModelEntity を対象にコンテキストメニューを表示する
	/// </summary>
	/// <param name="entityId">操作対象とする ModelEntity の識別子</param>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowForEntity(Guid entityId, Vector2I screenPosition)
	{
		SetTargetEntity(entityId);
		Position = screenPosition;

		Popup();
	}

	/// <summary>
	/// 表示はせず、操作対象とする ModelEntity のみを設定する
	/// </summary>
	/// <param name="entityId">操作対象とする ModelEntity の識別子</param>
	/// <remarks>PickResultMenu 等の親メニューからサブメニューとして使われる場合、表示は親側が行うため対象設定のみを公開する</remarks>
	public void SetTargetEntity(Guid entityId)
	{
		_targetEntityId = entityId;
	}

	#endregion

	#region Internal Helpers

	private void InitializeItems()
	{
		foreach (var value in Enum.GetValues(typeof(MenuItemId)))
		{
			AddItem(value.ToString(), (int)value);
		}
	}

	private void OnIdPressed(long id)
	{
		switch ((MenuItemId)id)
		{
			case MenuItemId.Fit:
				HandleFitMenuItemPressed();
				break;
			case MenuItemId.Emphasize:
				HandleEmphasizeMenuItemPressed();
				break;
			case MenuItemId.Spin:
				HandleSpinMenuItemPressed();
				break;
			case MenuItemId.TreeCentering:
				HandleTreeCenteringMenuItemPressed();
				break;
		}
	}

	private void HandleFitMenuItemPressed()
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Node3D[] fitTargetNodes = new Node3D[] { modelNode };
		Application.Viewport.Event.Fit(fitTargetNodes, true);
	}

	private void HandleEmphasizeMenuItemPressed()
	{
		GetModelNode()?.Emphasize();
	}

	private void HandleSpinMenuItemPressed()
	{
		GetModelNode()?.SpinAppeal();
	}
	
	private void HandleTreeCenteringMenuItemPressed()
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Application.Model.Event.TreeCentering(modelNode.EntityId);
	}

	private ModelNode GetModelNode()
	{
		if (_targetEntityId == Guid.Empty)
		{
			return null;
		}

		return Application.Model.Registry.GetEntity(_targetEntityId)?.Node;
	}

	#endregion
}
