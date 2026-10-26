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

	/// <summary>
	/// 表示切替ボタンのID
	/// </summary>
	private const int VisibilityButtonId = 1;

	/// <summary>
	/// Property OverlayTree表示切替ボタンのID（Root行のみ）
	/// </summary>
	private const int PropertyButtonId = 2;

	/// <summary>
	/// ModelProperty識別子と、対応する TreeItem の対応表
	/// </summary>
	private readonly System.Collections.Generic.Dictionary<Guid, TreeItem> _propertyItems = new();

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		base._Ready();
		ButtonClicked += OnButtonClicked;
		Application.Model.Property.Visual.VisibilityResolved += OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified += OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityPropertyTreeVisibleNotified;
	}

	public override void _ExitTree()
	{
		ButtonClicked -= OnButtonClicked;
		Application.Model.Property.Visual.VisibilityResolved -= OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified -= OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityPropertyTreeVisibleNotified;
		base._ExitTree();
	}

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

			// Root(ModelEntity)行はModelEntityMenu、下階層(ModelProperty)行はModelPropertyMenuを表示する
			if (targetItem == _rootItem)
			{
				if (_entityId == Guid.Empty)
				{
					return;
				}

				Application.Ui.Menu.ShowModelEntityMenu(_entityId);
			}
			else
			{
				Application.Ui.Menu.ShowModelPropertyMenu(TryGetPropertyValue(targetItem));
			}
		}
	}

	#endregion

	#region Methods

	/// <summary>
	/// 表示対象の ModelEntity 識別子を設定し、紐づく ModelProperty をツリーに表示する
	/// </summary>
	/// <param name="entityId">表示対象の ModelEntity の識別子</param>
	public void Show(Guid entityId)
	{
		_entityId = entityId;
		Clear();
		_rootItem = null;
		_propertyItems.Clear();

		ModelEntity entity = Application.Model.Entity.Registry.Get(_entityId);
		if (entity == null)
		{
			return;
		}

		TreeItem rootItem = CreateItem();
		rootItem.SetText(0, entity.Name);
		Texture2D entityIcon = Application.Asset.Icon.GetModelIcon(entity.IconId, Constant.Ui.Tree.HierarchyVisibleIconSize);
		rootItem.SetIcon(0, entityIcon);
		rootItem.AddButton(0, GetEntityPropertyIcon(entity), id: PropertyButtonId, disabled: entity.Properties.Count == 0);
		rootItem.AddButton(0, GetEntityVisibilityIcon(entity), id: VisibilityButtonId);
		_rootItem = rootItem;

		// Entity直下のプロパティを起点に、プロパティ階層を再帰表示する。
		foreach (ModelProperty property in entity.Properties)
		{
			AddPropertyToTree(property, rootItem);
		}
	}

	/// <summary>
	/// 現在表示中のEntityに紐づくPropertyを再表示する。
	/// </summary>
	public void Refresh()
	{
		Show(_entityId);
	}

	#endregion

	#region Events

	/// <summary>
	/// TreeItem のボタンがクリックされたとき、表示切替ボタンならPropertyの表示設定を切り替える
	/// </summary>
	private void OnButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		if (item == null)
		{
			return;
		}

		if (id == PropertyButtonId && item == _rootItem)
		{
			if (_entityId != Guid.Empty)
			{
				Application.Ui.Property.ToggleModelEntityProperties(_entityId);
			}

			return;
		}

		if (id != VisibilityButtonId)
		{
			return;
		}

		if (item == _rootItem)
		{
			Application.Model.Entity.Registry.Get(_entityId)?.ToggleVisibility();
			return;
		}

		if (!Guid.TryParse(item.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid propertyId))
		{
			return;
		}

		Application.Model.Property.Registry.Get(propertyId)?.ToggleVisibility();
	}

	/// <summary>
	/// Propertyの実効表示が解決されたとき、対応する行の表示ボタンを更新する
	/// </summary>
	/// <param name="propertyId">解決された ModelProperty の識別子</param>
	private void OnPropertyVisibilityResolved(string propertyId)
	{
		if (!Guid.TryParse(propertyId, out Guid parsedId) || !_propertyItems.TryGetValue(parsedId, out TreeItem item))
		{
			return;
		}

		ModelProperty property = Application.Model.Property.Registry.Get(parsedId);
		if (property != null)
		{
			item.SetButton(0, 0, GetVisibilityIcon(property));
		}
	}

	/// <summary>
	/// Propertyの値が変更されたとき、対応する行の表示文字列と値メタデータを更新する
	/// </summary>
	/// <param name="propertyId">値が変更された ModelProperty の識別子</param>
	private void OnPropertyValueNotified(string propertyId)
	{
		if (!Guid.TryParse(propertyId, out Guid parsedId) || !_propertyItems.TryGetValue(parsedId, out TreeItem item))
		{
			return;
		}

		ModelProperty property = Application.Model.Property.Registry.Get(parsedId);
		if (property != null)
		{
			item.SetText(0, GetPropertyText(property));
			item.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
		}
	}

	/// <summary>
	/// Entityの実効表示が解決されたとき、表示中Entityのルート行の表示ボタンを更新する
	/// </summary>
	/// <param name="entityId">解決された ModelEntity の識別子</param>
	private void OnEntityVisibilityResolved(string entityId)
	{
		if (_rootItem == null || !Guid.TryParse(entityId, out Guid parsedId) || parsedId != _entityId)
		{
			return;
		}

		ModelEntity entity = Application.Model.Entity.Registry.Get(parsedId);
		if (entity != null)
		{
			int index = _rootItem.GetButtonById(0, VisibilityButtonId);
			if (index >= 0)
			{
				_rootItem.SetButton(0, index, GetEntityVisibilityIcon(entity));
			}
		}
	}

	/// <summary>
	/// Property OverlayTreeの表示状態が通知されたとき、Root行のPropertyボタンを更新する
	/// </summary>
	/// <param name="entityId">表示状態が変更された ModelEntity の識別子</param>
	private void OnEntityPropertyTreeVisibleNotified(string entityId)
	{
		if (_rootItem == null || !Guid.TryParse(entityId, out Guid parsedId) || parsedId != _entityId)
		{
			return;
		}

		ModelEntity entity = Application.Model.Entity.Registry.Get(parsedId);
		int index = _rootItem.GetButtonById(0, PropertyButtonId);
		if (entity != null && index >= 0)
		{
			_rootItem.SetButton(0, index, GetEntityPropertyIcon(entity));
		}
	}

	#endregion

	#region Helpers

	/// <summary>
	/// Entityの表示設定と実効表示に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityVisibilityIcon(ModelEntity entity)
	{
		return Application.Asset.Icon.GetVisibility(
			entity.Visibility,
			entity.IsVisible,
			Constant.Ui.Tree.HierarchyVisibleIconSize);
	}

	/// <summary>
	/// EntityのProperty有無とOverlayTree表示状態に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityPropertyIcon(ModelEntity entity)
	{
		return Application.Asset.Icon.GetProperty(
			entity.PropertyCountRecursive,
			entity.IsPropertyTreeVisible,
			Constant.Ui.Tree.HierarchyVisibleIconSize);
	}

	/// <summary>
	/// Propertyの行に表示する文字列を取得する
	/// </summary>
	private static string GetPropertyText(ModelProperty property)
	{
		return string.IsNullOrEmpty(property.Value)
			? property.PropertyType
			: $"{property.PropertyType} : {property.Value}";
	}

	/// <summary>
	/// Propertyの表示設定と実効表示に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetVisibilityIcon(ModelProperty property)
	{
		return Application.Asset.Icon.GetVisibility(
			property.Visibility,
			property.IsVisible,
			Constant.Ui.Tree.HierarchyVisibleIconSize);
	}

	/// <summary>
	/// ModelProperty とその子プロパティをツリーへ追加する
	/// </summary>
	/// <param name="property">追加対象の ModelProperty</param>
	/// <param name="parentItem">追加先の TreeItem</param>
	private void AddPropertyToTree(ModelProperty property, TreeItem parentItem)
	{
		if (property == null || parentItem == null)
		{
			return;
		}

		TreeItem propertyItem = parentItem.CreateChild();
		propertyItem.SetText(0, GetPropertyText(property));
		if (!string.IsNullOrEmpty(property.IconId))
		{
			Texture2D icon = Application.Asset.Icon.GetModelIcon(property.IconId, Constant.Ui.Tree.HierarchyVisibleIconSize);
			if (icon != null)
			{
				propertyItem.SetIcon(0, icon);
			}
		}
		// 右クリックメニューでの値コピー用に、表示列とは別に値そのものをメタデータへ保持する
		propertyItem.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
		propertyItem.SetMeta("PropertyId", Variant.CreateFrom(property.Id.ToString()));

		propertyItem.AddButton(0, GetVisibilityIcon(property), id: VisibilityButtonId);
		_propertyItems[property.Id] = propertyItem;

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
