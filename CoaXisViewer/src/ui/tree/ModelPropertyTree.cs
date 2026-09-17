using Godot;

/// <summary>
/// ModelEntity に紐づく ModelProperty の階層を表示する UI コンポーネント
/// </summary>
public partial class ModelPropertyTree : Tree
{
	#region Public API

	/// <summary>
	/// 指定した ModelEntity をルートとして、紐づく ModelProperty をツリーに表示する
	/// </summary>
	/// <param name="entity">表示対象の ModelEntity</param>
	public void Show(ModelEntity entity)
	{
		Clear();
		if (entity == null)
		{
			return;
		}

		TreeItem rootItem = CreateItem();
		rootItem.SetText(0, entity.Name);

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
		propertyItem.SetText(0, $"{property.PropertyType}: {property.Value}");

		foreach (ModelProperty childProperty in property.Children)
		{
			AddPropertyToTree(childProperty, propertyItem);
		}
	}

	#endregion
}
