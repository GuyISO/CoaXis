using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// モデル選択状態表示と操作用のパネル
/// </summary>
public partial class SelectionUi : PanelContainer
{
    #region Fields

    private readonly List<Guid> _selectedEntityIds = new();
    private bool _isUpdatingTree = false;

    // 関連ノードのキャッシュ
    private Tree _tree = null!;
    private Button _buttonSet = null!;
    private Button _buttonAdd = null!;
    private Button _buttonRemove = null!;
    private Button _buttonToggle = null!;
    private Button _buttonClear = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        EnsureTreeColumns();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        SyncInitialState();
        Application.Pick.Event.AskHandlingMode();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    #endregion

    #region Events

    /// <summary>
    /// 子ノードを解決し、フィールドに保持する
    /// </summary>
    private void EnsureChildNodes()
    {
        // シーン構造が変更される可能性があるため、名前探索で関連ノードを解決する
        _tree = (Tree)FindChild("Tree");
        _buttonSet = (Button)FindChild("ButtonSet");
        _buttonAdd = (Button)FindChild("ButtonAdd");
        _buttonRemove = (Button)FindChild("ButtonRemove");
        _buttonToggle = (Button)FindChild("ButtonToggle");
        _buttonClear = (Button)FindChild("ButtonClear");
    }
    
    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _buttonSet.Pressed += OnButtonSetPressed;
        _buttonAdd.Pressed += OnButtonAddPressed;
        _buttonRemove.Pressed += OnButtonRemovePressed;
        _buttonToggle.Pressed += OnButtonTogglePressed;
        _buttonClear.Pressed += OnButtonClearPressed;
        _tree.GuiInput += OnTreeGuiInput;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _buttonSet.Pressed -= OnButtonSetPressed;
        _buttonAdd.Pressed -= OnButtonAddPressed;
        _buttonRemove.Pressed -= OnButtonRemovePressed;
        _buttonToggle.Pressed -= OnButtonTogglePressed;
        _buttonClear.Pressed -= OnButtonClearPressed;
        _tree.GuiInput -= OnTreeGuiInput;
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Pick.Event.HandlingModeNotified += OnPickHandlingModeNotified;
        Application.Selection.Event.ModeNotified += OnModeNotified;
        Application.Selection.Event.ModelStateNotified += OnModelStateNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Pick.Event.HandlingModeNotified -= OnPickHandlingModeNotified;
        Application.Selection.Event.ModeNotified -= OnModeNotified;
        Application.Selection.Event.ModelStateNotified -= OnModelStateNotified;
    }

    /// <summary>
    /// ピック操作モードの通知を受け取り、Selection以外ではモードボタンを解除する
    /// </summary>
    /// <param name="mode">通知されたピック操作モード</param>
    private void OnPickHandlingModeNotified(PickHandlingMode mode)
    {
        if (mode == PickHandlingMode.Selection)
        {
            UpdateModeButtons(Application.Selection.Service.Mode);
            return;
        }

        UpdateModeButtons(null);
    }

    /// <summary>
    /// Set ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonSetPressed()
    {
        Application.Pick.Event.SetHandlingMode(PickHandlingMode.Selection);
        Application.Selection.Event.SetMode(SelectionMode.Set);
    }

    /// <summary>
    /// Add ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonAddPressed()
    {
        Application.Pick.Event.SetHandlingMode(PickHandlingMode.Selection);
        Application.Selection.Event.SetMode(SelectionMode.Add);
    }

    /// <summary>
    /// Remove ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonRemovePressed()
    {
        Application.Pick.Event.SetHandlingMode(PickHandlingMode.Selection);
        Application.Selection.Event.SetMode(SelectionMode.Remove);
    }

    /// <summary>
    /// Toggle ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonTogglePressed()
    {
        Application.Pick.Event.SetHandlingMode(PickHandlingMode.Selection);
        Application.Selection.Event.SetMode(SelectionMode.Toggle);
    }

    /// <summary>
    /// Clear ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonClearPressed()
    {
        Application.Selection.Event.Clear();
    }

    /// <summary>
    /// Tree の入力イベントを受け取り、右クリック時にModelEntityメニューを表示する
    /// </summary>
    /// <param name="event">入力イベント</param>
    private void OnTreeGuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mb || mb.ButtonIndex != MouseButton.Right || !mb.Pressed)
        {
            return;
        }
        
        TreeItem targetItem = _tree.GetItemAtPosition(mb.Position);
        if (targetItem == null)
        {
            return;
        }
        
        Variant entityIdVariant = targetItem.GetMeta("EntityId", Variant.CreateFrom(string.Empty));
        if (!Guid.TryParse(entityIdVariant.AsString(), out Guid entityId) || entityId == Guid.Empty)
        {
            return;
        }

        // Popup(Window)のPositionはOS画面座標系のため、ビューポート内座標ではなくDisplayServerの実マウス座標を使う
        Vector2I mousePosition = DisplayServer.MouseGetPosition();
        Application.Menu.Service.ShowModelEntityMenu(entityId, mousePosition);
    }

    /// <summary>
    /// 選択モード通知を受け取ったときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="mode">通知された選択モード</param>
    private void OnModeNotified(SelectionMode mode)
    {
        UpdateModeButtons(mode);
    }

    /// <summary>
    /// モデル選択状態通知を受け取ったときに呼び出されるイベントハンドラ
    /// </summary>
    /// <param name="entityId">選択状態が変化した ModelEntity の識別子</param>
    /// <param name="isSelected">選択状態</param>
    private void OnModelStateNotified(string entityId, bool isSelected)
    {
        if (!Guid.TryParse(entityId, out Guid parsedEntityId) || parsedEntityId == Guid.Empty)
        {
            return;
        }

        int index = _selectedEntityIds.IndexOf(parsedEntityId);
        if (isSelected)
        {
            if (index < 0)
            {
                _selectedEntityIds.Add(parsedEntityId);
            }
        }
        else if (index >= 0)
        {
            _selectedEntityIds.RemoveAt(index);
        }

        RebuildTree();
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// Tree列の初期設定を行う
    /// </summary>
    private void EnsureTreeColumns()
    {
        SelectionTreeColumn[] columns = System.Enum.GetValues<SelectionTreeColumn>();
        _tree.Columns = columns.Length;

        foreach (SelectionTreeColumn column in columns)
        {
            int columnIndex = (int)column;
            _tree.SetColumnTitle(columnIndex, column.ToString());
            _tree.SetColumnExpand(columnIndex, column != SelectionTreeColumn.No);
        }

        // 固定幅にして運用する列の幅を指定する
        _tree.SetColumnCustomMinimumWidth((int)SelectionTreeColumn.No, Constant.Ui.Tree.SelectionNoColumnMinWidth);
    }

    /// <summary>
    /// 初期状態を SelectionService から同期する
    /// </summary>
    private void SyncInitialState()
    {
        UpdateModeButtons(Application.Selection.Service.Mode);

        _selectedEntityIds.Clear();
        IReadOnlyCollection<Guid> selectedEntityIds = Application.Selection.Service.EntityIds;
        if (selectedEntityIds != null && selectedEntityIds.Count > 0)
        {
            _selectedEntityIds.AddRange(selectedEntityIds);
        }

        RebuildTree();
    }

    /// <summary>
    /// モード切替ボタンの押下状態を更新する
    /// </summary>
    /// <param name="mode">選択モード</param>
    private void UpdateModeButtons(SelectionMode? mode)
    {
        _buttonSet.ButtonPressed = mode == SelectionMode.Set;
        _buttonAdd.ButtonPressed = mode == SelectionMode.Add;
        _buttonRemove.ButtonPressed = mode == SelectionMode.Remove;
        _buttonToggle.ButtonPressed = mode == SelectionMode.Toggle;
    }

    /// <summary>
    /// 現在の選択モデル一覧で Tree を再構築する
    /// </summary>
    private void RebuildTree()
    {
        if (_tree == null || !GodotObject.IsInstanceValid(_tree))
        {
            return;
        }

        _isUpdatingTree = true;

        _selectedEntityIds.RemoveAll(entityId => entityId == Guid.Empty || Application.Model.Registry.GetEntity(entityId) == null);

        _tree.Clear();
        TreeItem root = _tree.CreateItem();
        if (root == null)
        {
            _isUpdatingTree = false;
            return;
        }

        for (int i = 0; i < _selectedEntityIds.Count; i++)
        {
            Guid entityId = _selectedEntityIds[i];
            ModelEntity modelEntity = Application.Model.Registry.GetEntity(entityId);
            TreeItem item = _tree.CreateItem(root);
            item.SetText((int)SelectionTreeColumn.No, i.ToString());
            item.SetText((int)SelectionTreeColumn.Name, modelEntity?.Name ?? entityId.ToString());
            item.SetMeta("EntityId", entityId.ToString());
        }

        _isUpdatingTree = false;
    }

    #endregion
}
