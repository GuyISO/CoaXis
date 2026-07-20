using Godot;
using System.Collections.Generic;

/// <summary>
/// モデル選択状態表示と操作用のパネル
/// </summary>
public partial class SelectionUi : PanelContainer
{
    #region Fields

    private readonly List<AnyModel> _selectedModels = new();
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
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Selection.Event.ModeNotified += OnModeNotified;
        Application.Selection.Event.ModelStateNotified += OnModelStateNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Selection.Event.ModeNotified -= OnModeNotified;
        Application.Selection.Event.ModelStateNotified -= OnModelStateNotified;
    }

    /// <summary>
    /// Set ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonSetPressed()
    {
        Application.Selection.Event.SetMode(SelectionMode.Set);
    }

    /// <summary>
    /// Add ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonAddPressed()
    {
        Application.Selection.Event.SetMode(SelectionMode.Add);
    }

    /// <summary>
    /// Remove ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonRemovePressed()
    {
        Application.Selection.Event.SetMode(SelectionMode.Remove);
    }

    /// <summary>
    /// Toggle ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonTogglePressed()
    {
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
    /// <param name="model">選択状態が変化したモデル</param>
    /// <param name="isSelected">選択状態</param>
    private void OnModelStateNotified(AnyModel model, bool isSelected)
    {
        if (model == null)
        {
            return;
        }

        int index = _selectedModels.IndexOf(model);
        if (isSelected)
        {
            if (index < 0)
            {
                _selectedModels.Add(model);
            }
        }
        else if (index >= 0)
        {
            _selectedModels.RemoveAt(index);
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
        _tree.Columns = 2;
        _tree.SetColumnTitle(0, "No");
        _tree.SetColumnTitle(1, "Name");
        _tree.SetColumnExpand(0, false);
        _tree.SetColumnExpand(1, true);
        _tree.SetColumnCustomMinimumWidth(0, 64);
    }

    /// <summary>
    /// 初期状態を SelectionService から同期する
    /// </summary>
    private void SyncInitialState()
    {
        UpdateModeButtons(Application.Selection.Service.Mode);

        _selectedModels.Clear();
        AnyModel[] selectedModels = Application.Selection.Service.GetModelArray();
        if (selectedModels != null && selectedModels.Length > 0)
        {
            _selectedModels.AddRange(selectedModels);
        }

        RebuildTree();
    }

    /// <summary>
    /// モード切替ボタンの押下状態を更新する
    /// </summary>
    /// <param name="mode">選択モード</param>
    private void UpdateModeButtons(SelectionMode mode)
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

        _selectedModels.RemoveAll(model => model == null || !GodotObject.IsInstanceValid(model) || !model.IsInsideTree());

        _tree.Clear();
        TreeItem root = _tree.CreateItem();
        if (root == null)
        {
            _isUpdatingTree = false;
            return;
        }

        for (int i = 0; i < _selectedModels.Count; i++)
        {
            AnyModel model = _selectedModels[i];
            TreeItem item = _tree.CreateItem(root);
            item.SetText(0, i.ToString());
            item.SetText(1, model.Name);
        }

        _isUpdatingTree = false;
    }

    #endregion
}
