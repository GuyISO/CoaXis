using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// AnyModel と TreeItem の対応を管理するシングルトンクラス、Autoload としてシーンに追加して使用する、モデルのヒエラルキーを TreeItem に反映するための登録・登録解除、対応の取得処理を提供する
/// </summary>
/// <remarks>
/// 実際のモデル動的ロード時の登録の流れはModelManagerでメインシーンに追加、その後にModelEventHub経由で通知を受けたHierarchyTreeが更新され、そこからModelBinderに登録される
/// </remarks>
public partial class ModelBinder : Node
{
    public static ModelBinder I { get; private set; }

    // AnyModel ↔ TreeItem の対応辞書
    private readonly Dictionary<AnyModel, TreeItem> _modelToItem = new();
    private readonly Dictionary<TreeItem, AnyModel> _itemToModel = new();

    public override void _Ready()
    {
        I = this;
    }

	/// <summary>
	/// TreeItem から対応する AnyModel を取得する
	/// </summary>
	/// <param name="item">対応を取得する TreeItem</param>
	/// <returns>対応する AnyModel</returns>
    public static AnyModel GetModel(TreeItem item)
		=> I._itemToModel.TryGetValue(item, out var model) ? model : null;

	/// <summary>
	/// AnyModel から対応する TreeItem を取得する
	/// </summary>
	/// <param name="model">対応を取得する AnyModel</param>
	/// <returns>対応する TreeItem</returns>
    public static TreeItem GetItem(AnyModel model)
        => I._modelToItem.TryGetValue(model, out var item) ? item : null;

	/// <summary>
	/// AnyModel を登録する
	/// </summary>
	/// <param name="model">登録する AnyModel</param>
	/// <param name="item">対応する TreeItem</param>
	/// <returns>登録に成功した場合は true、すでに登録されている場合は false</returns>
    public static bool Bind(AnyModel model, TreeItem item)
    {
        if (I._modelToItem.ContainsKey(model))
            return false; // すでに登録されている
		
		if (I._itemToModel.ContainsKey(item))
            return false; // すでに登録されている
		
        I._modelToItem[model] = item;
        I._itemToModel[item] = model;
        
		return true;
    }

	/// <summary>
	/// AnyModel を登録解除する
	/// </summary>
	/// <param name="model">登録解除する AnyModel</param>
    public static void Unbind(AnyModel model)
    {
        if (!I._modelToItem.TryGetValue(model, out var item))
            return;

        I._itemToModel.Remove(item);
        I._modelToItem.Remove(model);

        item.Free();
    }

	/// <summary>
	/// TreeItem を登録解除する
	/// </summary>
	/// <param name="item">登録解除する TreeItem</param>
	public static void Unbind(TreeItem item)
	{
		if (!I._itemToModel.TryGetValue(item, out var model))
			return;

		I._modelToItem.Remove(model);
		I._itemToModel.Remove(item);

		item.Free();
	}
}