using Godot;
using System;

/// <summary>
/// ModelEntity を操作対象とするネイティブコンテキストメニュー
/// </summary>
/// <remarks>
/// UIコンポーネント（Tree/Viewportなど）ごとにメニューを作る方針から、操作対象オブジェクト（ModelEntity）単位でメニューを作る方針へ変更したもの。
/// 呼び出し側は TreeItem や PickResult から ModelEntity の識別子(Guid)を解決してから ShowForEntity を呼び出す。
/// ネイティブメニュー Rid の生成・解放は基底クラスの <see cref="BaseMenu"/> が担う。
/// </remarks>
public partial class ModelEntityMenu : BaseMenu
{
	#region Fields

	// UIコンポーネントに依存せず ModelEntity の識別子のみを対象として保持する
	private Guid _targetEntityId = Guid.Empty;

	#endregion

	#region Public API

	/// <summary>
	/// 指定した ModelEntity を対象にネイティブコンテキストメニューを表示する
	/// </summary>
	/// <param name="entityId">操作対象とする ModelEntity の識別子</param>
	/// <param name="screenPosition">メニューを表示する OS 画面座標</param>
	public void ShowForEntity(Guid entityId, Vector2I screenPosition)
	{
		SetTargetEntity(entityId);
		PopupNativeMenu(screenPosition);
	}

	/// <summary>
	/// 表示はせず、操作対象とする ModelEntity のみを設定する
	/// </summary>
	/// <param name="entityId">操作対象とする ModelEntity の識別子</param>
	/// <remarks>PickResultMenu のサブメニューとして使われる場合、表示は親側が行うため対象設定のみを公開する。</remarks>
	public void SetTargetEntity(Guid entityId)
	{
		_targetEntityId = entityId;
	}

	#endregion

	#region Protected API

	/// <inheritdoc/>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		NativeMenu.Singleton.AddItem(nativeMenu, "Fit", Callable.From<Variant>(HandleFitMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Emphasize", Callable.From<Variant>(HandleEmphasizeMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Spin", Callable.From<Variant>(HandleSpinMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "TreeCentering", Callable.From<Variant>(HandleTreeCenteringMenuItemPressed));
	}

	#endregion

	#region Internal Helpers

	// NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る
	private void HandleFitMenuItemPressed(Variant tag)
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Node3D[] fitTargetNodes = new Node3D[] { modelNode };
		Application.Viewport.Event.Fit(fitTargetNodes, true);
	}

	private void HandleEmphasizeMenuItemPressed(Variant tag)
	{
		GetModelNode()?.Emphasize();
	}

	private void HandleSpinMenuItemPressed(Variant tag)
	{
		GetModelNode()?.SpinAppeal();
	}

	private void HandleTreeCenteringMenuItemPressed(Variant tag)
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
