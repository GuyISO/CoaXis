using Godot;
using System;

/// <summary>
/// 各種コンテキストメニューの表示を一元管理するサービス
/// </summary>
/// <remarks>
/// 従来は ModelEntityTree や ViewportInteractionHandler など、呼び出し元 Node の子として
/// PopupMenu を配置していたが、対象オブジェクト単位でメニューを作る方針に伴い、
/// メニュー実体は本サービスが一括保持し、呼び出し元は Application.Menu.Service 経由で表示を依頼する
/// </remarks>
public partial class MenuService : Node
{
    #region Fields

    private ModelEntityMenu _modelEntityMenu;
    private PickResultMenu _pickResultMenu;
    private AxisNavigatorMenu _axisNavigatorMenu;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        _modelEntityMenu = EnsureMenu(_modelEntityMenu, "ModelEntityMenu");
        _pickResultMenu = EnsureMenu(_pickResultMenu, "PickResultMenu");
        _axisNavigatorMenu = EnsureMenu(_axisNavigatorMenu, "AxisNavigatorMenu");
    }

    #endregion

    #region Public API

    /// <summary>
    /// 指定した ModelEntity を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="entityId">操作対象とする ModelEntity の識別子</param>
    /// <param name="screenPosition">メニューを表示する OS 画面座標</param>
    internal void ShowModelEntityMenu(Guid entityId, Vector2I screenPosition)
    {
        _modelEntityMenu = EnsureMenu(_modelEntityMenu, "ModelEntityMenu");
        _modelEntityMenu.ShowForEntity(entityId, screenPosition);
    }

    /// <summary>
    /// 指定した PickResult を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="pickResult">操作対象とする PickResult</param>
    /// <param name="screenPosition">メニューを表示する OS 画面座標</param>
    internal void ShowPickResultMenu(PickResult pickResult, Vector2I screenPosition)
    {
        _pickResultMenu = EnsureMenu(_pickResultMenu, "PickResultMenu");
        _pickResultMenu.ShowForPickResult(pickResult, screenPosition);
    }

    /// <summary>
    /// AxisNavigator を対象にコンテキストメニューを表示する
    /// </summary>
    /// <param name="screenPosition">メニューを表示する OS 画面座標</param>
    internal void ShowAxisNavigatorMenu(Vector2I screenPosition)
    {
        _axisNavigatorMenu = EnsureMenu(_axisNavigatorMenu, "AxisNavigatorMenu");
        _axisNavigatorMenu.ShowAtPosition(screenPosition);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 指定したメニューのインスタンスを確保する、シーンに依存せずコードから直接生成する
    /// </summary>
    /// <typeparam name="TMenu">確保する PopupMenu の型</typeparam>
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
