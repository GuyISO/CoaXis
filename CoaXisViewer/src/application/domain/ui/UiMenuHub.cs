using Godot;
using System;

/// <summary>
/// コンテキストメニューの生成と表示を管理するハブ。
/// </summary>
public partial class UiMenuHub : BaseHub
{
    #region Fields

    private ModelEntityMenu _modelEntityMenu;
    private ModelPropertyMenu _modelPropertyMenu;
    private PickResultMenu _pickResultMenu;
    private AxisNavigatorMenu _axisNavigatorMenu;

    #endregion

    #region Properties

    #endregion

    #region Lifecycle

    #endregion

    #region Signals

    #endregion

    #region Events

    #endregion

    #region Methods

    /// <summary>
    /// 指定した ModelEntity を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="entityId">操作対象とする ModelEntity の識別子</param>
    internal void ShowModelEntityMenu(Guid entityId)
    {
        _modelEntityMenu = EnsureMenu(_modelEntityMenu, "ModelEntityMenu");
        _modelEntityMenu.ShowForEntity(entityId);
    }

    /// <summary>
    /// 指定した ModelProperty の値を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="value">操作対象とするプロパティ値</param>
    internal void ShowModelPropertyMenu(string value)
    {
        _modelPropertyMenu = EnsureMenu(_modelPropertyMenu, "ModelPropertyMenu");
        _modelPropertyMenu.ShowForValue(value);
    }

    /// <summary>
    /// 指定した PickResult を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="pickResult">操作対象とする PickResult</param>
    internal void ShowPickResultMenu(PickResult pickResult)
    {
        _pickResultMenu = EnsureMenu(_pickResultMenu, "PickResultMenu");
        _pickResultMenu.ShowForPickResult(pickResult);
    }

    /// <summary>
    /// AxisNavigator を対象にコンテキストメニューを表示する
    /// </summary>
    internal void ShowAxisNavigatorMenu()
    {
        _axisNavigatorMenu = EnsureMenu(_axisNavigatorMenu, "AxisNavigatorMenu");
        _axisNavigatorMenu.ShowAtPosition();
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 指定したメニューノードのインスタンスを確保する、シーンに依存せずコードから直接生成する
    /// </summary>
    /// <typeparam name="TMenu">確保するメニューノードの型</typeparam>
    /// <param name="menu">既存のインスタンス、未生成またはツリーから外れている場合は再生成する</param>
    /// <param name="nodeName">生成時に設定するノード名</param>
    /// <returns>有効な状態が保証されたメニューのインスタンス</returns>
    private TMenu EnsureMenu<TMenu>(TMenu menu, string nodeName) where TMenu : Node, new()
    {
        if (menu != null && GodotObject.IsInstanceValid(menu))
        {
            return menu;
        }

        menu = new TMenu { Name = nodeName };
        AddChild(menu);
        return menu;
    }

    #endregion
}
