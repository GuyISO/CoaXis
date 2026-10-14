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
	public void Show(Guid entityId)
	{
		SetTargetEntity(entityId);
		PopupNativeMenu();
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

	#region Helpers

	/// <summary>
	/// ModelEntityMenu のネイティブメニュー項目を構築する
	/// </summary>
	/// <param name="nativeMenu">ネイティブメニューの Rid</param>
	protected override void BuildMenuItems(Rid nativeMenu)
	{
		NativeMenu.Singleton.AddItem(nativeMenu, "Fit", Callable.From<Variant>(HandleFitMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Emphasize", Callable.From<Variant>(HandleEmphasizeMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Spin", Callable.From<Variant>(HandleSpinMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "TreeCentering", Callable.From<Variant>(HandleTreeCenteringMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Show Property", Callable.From<Variant>(HandleShowPropertyMenuItemPressed));
		NativeMenu.Singleton.AddItem(nativeMenu, "Hide Property", Callable.From<Variant>(HandleHidePropertyMenuItemPressed));
	}

	/// <summary>
	/// カメラを対象の ModelEntity にフィットさせるメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleFitMenuItemPressed(Variant tag)
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Node3D[] fitTargetNodes = new Node3D[] { modelNode };
		Application.Viewport.Camera.Fit(fitTargetNodes, true);
	}

	/// <summary>
	/// 対象の ModelEntity を強調表示するメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleEmphasizeMenuItemPressed(Variant tag)
	{
		GetModelNode()?.Emphasize();
	}

	/// <summary>
	/// 対象の ModelEntity を回転させるメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleSpinMenuItemPressed(Variant tag)
	{
		GetModelNode()?.SpinAppeal();
	}

	/// <summary>
	/// 対象の ModelEntity をツリー上で中央に配置するメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleTreeCenteringMenuItemPressed(Variant tag)
	{
		ModelNode modelNode = GetModelNode();
		if (modelNode == null)
		{
			return;
		}

		Application.Model.Entity.Tree.Centering(modelNode.EntityId);
	}

	/// <summary>
	/// 対象の ModelEntity のプロパティを表示するメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleShowPropertyMenuItemPressed(Variant tag)
	{
		if (_targetEntityId == Guid.Empty)
		{
			Application.Log.Warn("ModelEntityMenu: show-property requested without a target entity.");
			return;
		}

		Application.Ui.Property.ShowModelEntityProperties(_targetEntityId);
	}

	/// <summary>
	/// 対象の ModelEntity のプロパティを非表示にするメニュー項目が選択された際の処理
	/// </summary>
	/// <param name="tag">NativeMenuのcallbackはtagに渡した値をVariantとして1引数で受け取る契約のため、未使用でも引数を受け取る</param>
	private void HandleHidePropertyMenuItemPressed(Variant tag)
	{
		if (_targetEntityId == Guid.Empty)
		{
			Application.Log.Warn("ModelEntityMenu: hide-property requested without a target entity.");
			return;
		}

		Application.Ui.Property.HideModelEntityProperties(_targetEntityId);
	}

	/// <summary>
	/// 対象の ModelEntity の ModelNode を取得するヘルパーメソッド
	/// </summary>
	/// <returns>対象の ModelEntity に対応する ModelNode。存在しない場合は null。</returns>
	private ModelNode GetModelNode()
	{
		if (_targetEntityId == Guid.Empty)
		{
			return null;
		}

		return Application.Model.Entity.Registry.Get(_targetEntityId)?.Node;
	}

	#endregion
}
