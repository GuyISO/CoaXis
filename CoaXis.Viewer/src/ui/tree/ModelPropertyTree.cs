using System;
using System.Collections.Generic;
using CoaXis.Protocol.Viewer;
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

	private bool _isNotifyingRootPick;

	/// <summary>
	/// 表示切替ボタンのID
	/// </summary>
	private const int VisibilityButtonId = ModelEntityRowBehavior.VisibilityButtonId;

	/// <summary>
	/// Property OverlayTree表示切替ボタンのID（Root行のみ）
	/// </summary>
	private const int PropertyButtonId = ModelEntityRowBehavior.PropertyButtonId;

	/// <summary>
	/// Particleテクスチャ表示ボタンのID（Particle指定のProperty行のみ）
	/// </summary>
	private const int ParticleButtonId = 3;

	/// <summary>
	/// ModelProperty識別子と、対応する TreeItem の対応表
	/// </summary>
	private readonly System.Collections.Generic.Dictionary<Guid, TreeItem> _propertyItems = new();

	private TreeItem _lastSelectedPropertyItem;
	private readonly HashSet<TreeItem> _highlightedItems = new();

	#endregion

	#region Lifecycle

	public override void _Ready()
	{
		base._Ready();
		SubscribeEvents();
	}

	public override void _ExitTree()
	{
		UnsubscribeEvents();
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

				ModelEntityRowBehavior.ShowContextMenu(_entityId);
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
		// 自身のPickを受けた埋め込みツリーが、操作中の行を再生成しないようにする。
		if (_isNotifyingRootPick && entityId == _entityId)
		{
			return;
		}

		_entityId = entityId;
		Clear();
		_rootItem = null;
		_propertyItems.Clear();
		_highlightedItems.Clear();
		_lastSelectedPropertyItem = null;

		ModelEntity entity = Application.Model.Entity.Registry.Get(_entityId);
		if (entity == null)
		{
			return;
		}

		TreeItem rootItem = CreateItem();
		rootItem.SetText(0, entity.Name);
		Texture2D entityIcon = ModelEntityRowBehavior.GetModelIcon(entity.IconId);
		rootItem.SetIcon(0, entityIcon);
		rootItem.AddButton(0, GetEntityPropertyIcon(entity), id: PropertyButtonId, disabled: entity.Properties.Count == 0);
		rootItem.AddButton(0, GetEntityVisibilityIcon(entity), id: VisibilityButtonId);
		_rootItem = rootItem;

		// Entity直下のプロパティを起点に、プロパティ階層を再帰表示する。
		foreach (ModelProperty property in entity.Properties)
		{
			AddPropertyToTree(property, rootItem);
		}

		RefreshSelectionHighlights();
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

	private void SubscribeEvents()
	{
		ButtonClicked += OnButtonClicked;
		CellSelected += OnItemSelected;
		ItemCollapsed += OnItemCollapsed;
		ItemActivated += OnItemActivated;
		Application.Model.Property.Visual.VisibilityResolved += OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified += OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved += OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified += OnEntityPropertyTreeVisibleNotified;
		Application.Model.Entity.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged += OnSelectionChanged;
		Application.Model.Property.Selection.Cleared += OnPropertySelectionCleared;
		Application.Ui.Theme.ThemeChanged += OnThemeChanged;
	}

	private void UnsubscribeEvents()
	{
		ButtonClicked -= OnButtonClicked;
		CellSelected -= OnItemSelected;
		ItemCollapsed -= OnItemCollapsed;
		ItemActivated -= OnItemActivated;
		Application.Model.Property.Visual.VisibilityResolved -= OnPropertyVisibilityResolved;
		Application.Model.Property.State.ValueNotified -= OnPropertyValueNotified;
		Application.Model.Entity.Visual.VisibilityResolved -= OnEntityVisibilityResolved;
		Application.Model.Entity.State.PropertyTreeVisibleNotified -= OnEntityPropertyTreeVisibleNotified;
		Application.Model.Entity.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.SelectionChanged -= OnSelectionChanged;
		Application.Model.Property.Selection.Cleared -= OnPropertySelectionCleared;
		Application.Ui.Theme.ThemeChanged -= OnThemeChanged;
	}

	/// <summary>
	/// 行が選択されたとき、EntityのPickまたはPropertyの選択を処理する。
	/// </summary>
	private void OnItemSelected()
	{
		TreeItem item = GetSelected();
		if (_rootItem == null || _entityId == Guid.Empty || item == null)
		{
			return;
		}

		if (item == _rootItem)
		{
			_lastSelectedPropertyItem = null;
			_isNotifyingRootPick = true;
			try
			{
				Application.Model.Entity.Pick.NotifyResult(PickUtility.PickByEntityId(_entityId));
			}
			finally
			{
				_isNotifyingRootPick = false;
			}
			return;
		}

		if (!Guid.TryParse(item.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid propertyId))
		{
			Application.Log.Warn("ModelPropertyTree: selected item has no associated property.");
			return;
		}

		ModelSelectionMode mode = Application.Model.Selection.CurrentMode;
		Guid[] ids = (mode == ModelSelectionMode.Add || mode == ModelSelectionMode.Remove) && _lastSelectedPropertyItem != null
			? GetPropertyIdsInRange(_lastSelectedPropertyItem, item)
			: new[] { propertyId };

		switch (mode)
		{
			case ModelSelectionMode.Set:
				Application.Model.Property.Selection.Set(ids);
				break;
			case ModelSelectionMode.Add:
				Application.Model.Property.Selection.Add(ids);
				break;
			case ModelSelectionMode.Remove:
				Application.Model.Property.Selection.Remove(ids);
				break;
			case ModelSelectionMode.Toggle:
				Application.Model.Property.Selection.Toggle(ids);
				break;
			default:
				Application.Log.Warn($"ModelPropertyTree: Unknown selection mode {mode}.");
				break;
		}

		_lastSelectedPropertyItem = item;
	}

	/// <summary>
	/// TreeItem のボタンがクリックされたとき、表示切替ボタンならPropertyの表示設定を切り替える
	/// </summary>
	private void OnButtonClicked(TreeItem item, long column, long id, long mouseButtonIndex)
	{
		if (item == null)
		{
			return;
		}

		if (item == _rootItem)
		{
			ModelEntityRowBehavior.HandleButtonClick(_entityId, id);
			return;
		}

		if (id != VisibilityButtonId)
		{
			return;
		}

		if (!Guid.TryParse(item.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid propertyId))
		{
			return;
		}

		Application.Model.Property.Registry.Get(propertyId)?.ToggleVisibility();
	}

	/// <summary>
	/// EntityまたはPropertyの選択状態が変更されたとき、背景色を更新する。
	/// </summary>
	private void OnSelectionChanged()
	{
		RefreshSelectionHighlights();
	}

	/// <summary>Property選択の解除後、範囲選択の起点を破棄する。</summary>
	private void OnPropertySelectionCleared()
	{
		_lastSelectedPropertyItem = null;
	}

	/// <summary>折り畳みに応じて選択背景を更新する。</summary>
	private void OnItemCollapsed(TreeItem item)
	{
		RefreshSelectionHighlights();
	}

	/// <summary>
	/// テーマ切替後、背景色とParticle以外のアイコンを更新する。
	/// </summary>
	private void OnThemeChanged()
	{
		RefreshSelectionHighlights();
		ModelEntity entity = Application.Model.Entity.Registry.Get(_entityId);
		if (_rootItem != null && entity != null)
		{
			ModelEntityRowBehavior.UpdateThemeIcons(_rootItem, entity, entity.PropertyCountRecursive, false);
		}

		foreach (System.Collections.Generic.KeyValuePair<Guid, TreeItem> pair in _propertyItems)
		{
			ModelProperty property = Application.Model.Property.Registry.Get(pair.Key);
			if (property == null)
			{
				continue;
			}

			pair.Value.SetIcon(0, ModelEntityRowBehavior.GetModelIcon(property.IconId));
			int index = pair.Value.GetButtonById(0, VisibilityButtonId);
			if (index >= 0)
			{
				pair.Value.SetButton(0, index, GetVisibilityIcon(property));
			}
		}
	}

	/// <summary>
	/// 現在の選択と折り畳み状態から各行の背景色を更新する。
	/// </summary>
	private void RefreshSelectionHighlights()
	{
		if (_rootItem == null)
		{
			return;
		}

		var next = new HashSet<TreeItem>();
		if (Application.Model.Entity.Selection.Contains(_entityId))
		{
			next.Add(_rootItem);
		}

		foreach (Guid propertyId in Application.Model.Property.Selection.SelectedIds)
		{
			if (!_propertyItems.TryGetValue(propertyId, out TreeItem item))
			{
				continue;
			}

			TreeItem visible = item;
			for (TreeItem ancestor = item.GetParent(); ancestor != null; ancestor = ancestor.GetParent())
			{
				if (ancestor.Collapsed)
				{
					visible = ancestor;
				}
			}
			next.Add(visible);
		}

		foreach (TreeItem item in _highlightedItems)
		{
			if (!next.Contains(item))
			{
				item.ClearCustomBgColor(0);
			}
		}

		Color color = ModelEntityRowBehavior.GetSelectionColor();
		foreach (TreeItem item in next)
		{
			item.SetCustomBgColor(0, color);
		}
		_highlightedItems.Clear();
		_highlightedItems.UnionWith(next);
	}

	/// <summary>
	/// ルートの ModelEntity 行がアクティブ化されたとき、Manipulate 操作を通知する。
	/// </summary>
	private void OnItemActivated()
	{
		TreeItem selectedItem = GetSelected();
		if (_rootItem != null && _entityId != Guid.Empty && selectedItem == _rootItem)
		{
			ModelEntityRowBehavior.Manipulate(_entityId);
			return;
		}

		if (selectedItem != null &&
			Guid.TryParse(selectedItem.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid propertyId))
		{
			Application.Model.Property.Manipulate(propertyId);
		}
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
			int index = item.GetButtonById(0, VisibilityButtonId);
			if (index >= 0)
			{
				item.SetButton(0, index, GetVisibilityIcon(property));
			}
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

	/// <summary>表示順で2行の間にあるProperty識別子を取得する。</summary>
	private Guid[] GetPropertyIdsInRange(TreeItem first, TreeItem last)
	{
		var visible = new List<TreeItem>();
		CollectVisiblePropertyItems(_rootItem, visible);
		int firstIndex = visible.IndexOf(first);
		int lastIndex = visible.IndexOf(last);
		if (firstIndex < 0 || lastIndex < 0)
		{
			return Guid.TryParse(last.GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid id)
				? new[] { id }
				: Array.Empty<Guid>();
		}

		var ids = new List<Guid>();
		for (int i = Math.Min(firstIndex, lastIndex); i <= Math.Max(firstIndex, lastIndex); i++)
		{
			if (Guid.TryParse(visible[i].GetMeta("PropertyId", Variant.CreateFrom(string.Empty)).AsString(), out Guid id))
			{
				ids.Add(id);
			}
		}
		return ids.ToArray();
	}

	/// <summary>表示中のProperty行を階層順に収集する。</summary>
	private void CollectVisiblePropertyItems(TreeItem item, List<TreeItem> items)
	{
		if (item != _rootItem)
		{
			items.Add(item);
		}
		if (!item.Collapsed)
		{
			for (TreeItem child = item.GetFirstChild(); child != null; child = child.GetNext())
			{
				CollectVisiblePropertyItems(child, items);
			}
		}
	}

	/// <summary>
	/// Entityの表示設定と実効表示に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityVisibilityIcon(ModelEntity entity)
	{
		return ModelEntityRowBehavior.GetVisibilityIcon(entity.Visibility, entity.IsVisible);
	}

	/// <summary>
	/// EntityのProperty有無とOverlayTree表示状態に対応するボタンアイコンを取得する
	/// </summary>
	private static Texture2D GetEntityPropertyIcon(ModelEntity entity)
	{
		return ModelEntityRowBehavior.GetPropertyIcon(entity.PropertyCountRecursive, entity.IsPropertyTreeVisible);
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
		return ModelEntityRowBehavior.GetVisibilityIcon(property.Visibility, property.IsVisible);
	}

	/// <summary>
	/// Particleテクスチャ指定があるPropertyのボタンアイコンを、ParticleColorで着色して取得する
	/// </summary>
	/// <returns>着色済みアイコン、テクスチャ未指定または取得失敗時は null</returns>
	private static Texture2D GetParticleIcon(ModelProperty property)
	{
		if (!property.IsParticleEnabled)
		{
			return null;
		}

		Texture2D source = Application.Asset.Texture.GetParticle(property.ParticleTextureId);
		Image image = source?.GetImage();
		if (image == null)
		{
			return null;
		}

		image = (Image)image.Duplicate();
		if (image.IsCompressed())
		{
			image.Decompress();
		}

		image.Convert(Image.Format.Rgba8);
		int size = Constant.Ui.Tree.HierarchyVisibleIconSize;
		image.Resize(size, size, Image.Interpolation.Lanczos);

		Color tint = property.ParticleColor ?? Colors.White;
		for (int y = 0; y < image.GetHeight(); y++)
		{
			for (int x = 0; x < image.GetWidth(); x++)
			{
				image.SetPixel(x, y, image.GetPixel(x, y) * tint);
			}
		}

		return ImageTexture.CreateFromImage(image);
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
			Texture2D icon = ModelEntityRowBehavior.GetModelIcon(property.IconId);
			if (icon != null)
			{
				propertyItem.SetIcon(0, icon);
			}
		}
		
		// 右クリックメニューでの値コピー用に、表示列とは別に値そのものをメタデータへ保持する
		propertyItem.SetMeta("PropertyValue", Variant.CreateFrom(property.Value ?? string.Empty));
		propertyItem.SetMeta("PropertyId", Variant.CreateFrom(property.Id.ToString()));

		Texture2D particleIcon = GetParticleIcon(property);
		if (particleIcon != null)
		{
			propertyItem.AddButton(0, particleIcon, id: ParticleButtonId);
		}
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
