using System;
using Godot;

/// <summary>
/// ModelEntity に紐づく ModelProperty の階層を表示する UI コンポーネント
/// </summary>
public partial class ModelPropertyTree : Tree
{
	#region Fields

	/// <summary>
	/// このツリーが表示対象としている ModelEntity の識別子
	/// </summary>
	private Guid _entityId = Guid.Empty;

	/// <summary>
	/// ModelEntity を表すルートの TreeItem、右クリック対象の判定に使う
	/// </summary>
	private TreeItem _rootItem;

	#endregion

	#region Lifecycle

	public override void _GuiInput(InputEvent @event)
	{
		// 右クリックによるコンテキストメニュー表示の処理
		if (@event is InputEventMouseButton mb && mb.ButtonIndex == MouseButton.Right && mb.Pressed)
		{
			TreeItem targetItem = GetItemAtPosition(GetLocalMousePosition());
			// 空白領域での右クリックは対象不明のメニュー表示になり紛らわしいため、TreeItem上のみメニューを出す
			if (targetItem == null)
			{
				return;
			}

			// Popup(Window)のPositionはOS画面座標系のため、ビューポート内座標のGetGlobalMousePositionではなくDisplayServerの実マウス座標を使う
			Vector2I mousePosition = DisplayServer.MouseGetPosition();

			// Root(ModelEntity)行はModelEntityMenu、下階層(ModelProperty)行はModelPropertyMenuを表示する
			if (targetItem == _rootItem)
			{
				if (_entityId == Guid.Empty)
				{
					return;
				}

				Application.Menu.Service.ShowModelEntityMenu(_entityId, mousePosition);
			}
			else
			{
				Application.Menu.Service.ShowModelPropertyMenu(TryGetPropertyValue(targetItem), mousePosition);
			}
		}
	}

	#endregion

	#region Public API

	/// <summary>
	/// 表示対象の ModelEntity 識別子を設定し、紐づく ModelProperty をツリーに表示する
	/// </summary>
	/// <param name="entityId">表示対象の ModelEntity の識別子</param>
	public void Show(Guid entityId)
	{
		_entityId = entityId;
		Clear();
		_rootItem = null;

		ModelEntity entity = Application.Model.Registry.GetEntity(_entityId);
		if (entity == null)
		{
			return;
		}

		TreeItem rootItem = CreateItem();
		rootItem.SetText(0, entity.Name);
		_rootItem = rootItem;

		// Entity直下のプロパティを起点に、プロパティ階層を再帰表示する。
		foreach (ModelProperty property in entity.Properties)
		{
			AddPropertyToTree(property, rootItem);
		}
	}

	#endregion

	#region Internal Helpers

	/// <summary>
	/// ModelProperty とその子プロパティをツリーへ追加する
	/// </summary>
	/// <param name="property">追加対象の ModelProperty</param>
	/// <param name="parentItem">追加先の TreeItem</param>
	private static void AddPropertyToTree(ModelProperty property, TreeItem parentItem)
	{
		if (property == null || parentItem == null)
		{
			return;
		}

		TreeItem propertyItem = parentItem.CreateChild();
		propertyItem.SetText(0, property.PropertyType);
		propertyItem.SetText(1, property.Value);
		// 右クリックメニューでの値コピー用に、表示列とは別に値そのものをメタデータへ保持する
		propertyItem.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));

		foreach (ModelProperty childProperty in property.Children)
		{
			AddPropertyToTree(childProperty, propertyItem);
		}
	}

	/// <summary>
	/// TreeItem に保持されたプロパティ値を取得する
	/// </summary>
	/// <param name="item">値を取得する対象の TreeItem</param>
	/// <returns>保持されている値、存在しない場合は空文字</returns>
	private static string TryGetPropertyValue(TreeItem item)
	{
		if (item == null)
		{
			return string.Empty;
		}

		return item.GetMeta("PropertyValue", Variant.CreateFrom(string.Empty)).AsString();
	}

	#endregion
}
