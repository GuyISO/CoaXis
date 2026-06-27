using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// AnyModel と TreeItem の対応を管理するシングルトンクラスで、Autoload としてシーンに追加して使用する
/// モデルのヒエラルキーを TreeItem に反映するための登録処理と登録解除処理、対応取得処理を提供する
/// </summary>
/// <remarks>
/// 実際の動的ロード時は ModelManager でメインシーンへ追加し、その後 ModelEventHub の通知で HierarchyTree が更新されて ModelBinder に登録される
/// </remarks>
public partial class ModelBinder : Node
{
    public static ModelBinder Instance { get; private set; }

    // AnyModel ↔ TreeItem の対応辞書
    private readonly Dictionary<AnyModel, TreeItem> _modelToItem = new();
    private readonly Dictionary<TreeItem, AnyModel> _itemToModel = new();

    public override void _Ready()
    {
        Instance = this;
    }

	/// <summary>
	/// TreeItem から対応する AnyModel を取得する
	/// </summary>
	/// <param name="item">対応を取得する TreeItem</param>
	/// <returns>対応する AnyModel</returns>
    public static AnyModel GetModel(TreeItem item)
		=> Instance._itemToModel.TryGetValue(item, out var model) ? model : null;

	/// <summary>
	/// AnyModel から対応する TreeItem を取得する
	/// </summary>
	/// <param name="model">対応を取得する AnyModel</param>
	/// <returns>対応する TreeItem</returns>
    public static TreeItem GetItem(AnyModel model)
        => Instance._modelToItem.TryGetValue(model, out var item) ? item : null;

	/// <summary>
	/// AnyModel を登録する
	/// </summary>
	/// <param name="model">登録する AnyModel</param>
	/// <param name="item">対応する TreeItem</param>
	/// <returns>登録に成功した場合は true、すでに登録されている場合は false</returns>
    public static bool Bind(AnyModel model, TreeItem item)
    {
        if (Instance._modelToItem.ContainsKey(model))
            return false; // すでに登録されている
		
		if (Instance._itemToModel.ContainsKey(item))
            return false; // すでに登録されている
		
        Instance._modelToItem[model] = item;
        Instance._itemToModel[item] = model;
        
		return true;
    }

	/// <summary>
	/// AnyModel を登録解除する
	/// </summary>
	/// <param name="model">登録解除する AnyModel</param>
    public static void Unbind(AnyModel model)
    {
        if (!Instance._modelToItem.TryGetValue(model, out var item))
            return;

        Instance._itemToModel.Remove(item);
        Instance._modelToItem.Remove(model);

        item.Free();
    }

	/// <summary>
	/// TreeItem を登録解除する
	/// </summary>
	/// <param name="item">登録解除する TreeItem</param>
	public static void Unbind(TreeItem item)
	{
		if (!Instance._itemToModel.TryGetValue(item, out var model))
			return;

		Instance._modelToItem.Remove(model);
		Instance._itemToModel.Remove(item);

		item.Free();
	}
}
