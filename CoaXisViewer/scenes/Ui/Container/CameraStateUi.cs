using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// カメラ状態の保存と読み込みする UI
/// </summary>
public partial class CameraStateUi : PanelContainer
{
    #region Fields

    private readonly List<CameraState> _savedCameraStates = new();
    private bool _isInitialized = false;
    private bool _isUpdatingTree = false;

    // 直近のビューポート状態（Godot座標系）
    private Vector3 _currentPosition = Vector3.Zero;
    private Quaternion _currentRotation = Quaternion.Identity;
    private float _currentDistance = 5f;
    private float _currentSize = 5f;
    private float _currentFov = 35f;
    private Camera3D.ProjectionType _currentProjectionType = Camera3D.ProjectionType.Perspective;

    // 関連ノードのキャッシュ
    private Tree _tree = null!;
    private Button _buttonSave = null!;
    private Button _buttonRemove = null!;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        EnsureTreeColumns();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        UpdateRemoveButtonEnabled();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Viewport.Event.AskState();
        }
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
        _buttonSave = (Button)FindChild("ButtonSave");
        _buttonRemove = (Button)FindChild("ButtonRemove");
    }
    
    /// <summary>
    /// UIイベントの購読を開始する
    /// </summary>
    private void SubscribeUiEvents()
    {
        _buttonSave.Pressed += OnButtonSavePressed;
        _buttonRemove.Pressed += OnButtonRemovePressed;
        _tree.ItemSelected += OnTreeItemSelected;
        _tree.ItemActivated += OnTreeItemActivated;
    }

    /// <summary>
    /// UIイベントの購読を解除する
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _buttonSave.Pressed -= OnButtonSavePressed;
        _buttonRemove.Pressed -= OnButtonRemovePressed;
        _tree.ItemSelected -= OnTreeItemSelected;
        _tree.ItemActivated -= OnTreeItemActivated;
    }

    /// <summary>
    /// Applicationイベントの購読を開始する
    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Viewport.Event.PositionNotified += OnPositionNotified;
        Application.Viewport.Event.RotationNotified += OnRotationNotified;
        Application.Viewport.Event.DistanceNotified += OnDistanceNotified;
        Application.Viewport.Event.SizeNotified += OnSizeNotified;
        Application.Viewport.Event.FovNotified += OnFovNotified;
        Application.Viewport.Event.ProjectionTypeNotified += OnProjectionTypeNotified;
    }

    /// <summary>
    /// Applicationイベントの購読を解除する
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Viewport.Event.PositionNotified -= OnPositionNotified;
        Application.Viewport.Event.RotationNotified -= OnRotationNotified;
        Application.Viewport.Event.DistanceNotified -= OnDistanceNotified;
        Application.Viewport.Event.SizeNotified -= OnSizeNotified;
        Application.Viewport.Event.FovNotified -= OnFovNotified;
        Application.Viewport.Event.ProjectionTypeNotified -= OnProjectionTypeNotified;
    }

    /// <summary>
    /// Save ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonSavePressed()
    {
        if (!_isInitialized)
        {
            Application.Viewport.Event.AskState();
            Application.Log.Warn("CameraStateUi: save skipped because viewport state is not initialized yet.");
            return;
        }

        CameraState state = CameraState.Create(
            _currentPosition,
            _currentRotation,
            _currentDistance,
            _currentSize,
            _currentFov,
            _currentProjectionType);
        state.Normalize();

        _savedCameraStates.Add(state);
        RebuildTree();
        SelectTreeItem(_savedCameraStates.Count - 1);
    }

    /// <summary>
    /// Remove ボタン押下時のイベントハンドラ
    /// </summary>
    private void OnButtonRemovePressed()
    {
        int selectedIndex = GetSelectedStateIndex();
        if (selectedIndex < 0 || selectedIndex >= _savedCameraStates.Count)
        {
            return;
        }

        _savedCameraStates.RemoveAt(selectedIndex);
        RebuildTree();

        int nextIndex = Mathf.Clamp(selectedIndex, 0, _savedCameraStates.Count - 1);
        SelectTreeItem(nextIndex);
    }

    /// <summary>
    /// Tree の選択変更時に Remove ボタンの有効状態を更新する
    /// </summary>
    private void OnTreeItemSelected()
    {
        if (_isUpdatingTree)
        {
            return;
        }

        UpdateRemoveButtonEnabled();
    }

    /// <summary>
    /// Tree のアイテム確定時に保存済みカメラ状態を適用する
    /// </summary>
    private void OnTreeItemActivated()
    {
        int selectedIndex = GetSelectedStateIndex();
        if (selectedIndex < 0 || selectedIndex >= _savedCameraStates.Count)
        {
            return;
        }

        ApplyCameraState(_savedCameraStates[selectedIndex]);
    }

    /// <summary>
    /// カメラ位置通知のイベントハンドラ
    /// </summary>
    /// <param name="position">通知されたカメラ位置</param>
    private void OnPositionNotified(Vector3 position)
    {
        _currentPosition = position;
    }

    /// <summary>
    /// カメラ回転通知のイベントハンドラ
    /// </summary>
    /// <param name="rotation">通知されたカメラ回転</param>
    private void OnRotationNotified(Quaternion rotation)
    {
        _currentRotation = rotation;
    }

    /// <summary>
    /// カメラ距離通知のイベントハンドラ
    /// </summary>
    /// <param name="distance">通知されたカメラ距離</param>
    private void OnDistanceNotified(float distance)
    {
        _currentDistance = distance;
    }

    /// <summary>
    /// カメラサイズ通知のイベントハンドラ
    /// </summary>
    /// <param name="size">通知されたカメラサイズ</param>
    private void OnSizeNotified(float size)
    {
        _currentSize = size;
    }

    /// <summary>
    /// カメラFOV通知のイベントハンドラ
    /// </summary>
    /// <param name="fov">通知されたFOV</param>
    private void OnFovNotified(float fov)
    {
        _currentFov = fov;
    }

    /// <summary>
    /// カメラ投影タイプ通知のイベントハンドラ
    /// </summary>
    /// <param name="projectionType">通知された投影タイプ</param>
    private void OnProjectionTypeNotified(Camera3D.ProjectionType projectionType)
    {
        _currentProjectionType = projectionType;
        _isInitialized = true;
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
    /// 現在のカメラ状態一覧で Tree を再構築する
    /// </summary>
    private void RebuildTree()
    {
        if (_tree == null || !GodotObject.IsInstanceValid(_tree))
        {
            return;
        }

        _isUpdatingTree = true;

        try
        {
            _tree.Clear();
            TreeItem root = _tree.CreateItem();
            if (root == null)
            {
                return;
            }

            for (int i = 0; i < _savedCameraStates.Count; i++)
            {
                CameraState state = _savedCameraStates[i];
                TreeItem item = _tree.CreateItem(root);
                if (item == null)
                {
                    continue;
                }

                item.SetMetadata((int)SelectionTreeColumn.No, i);
                item.SetText((int)SelectionTreeColumn.No, i.ToString());
                item.SetText((int)SelectionTreeColumn.Name, BuildStateLabel(state));
            }
        }
        finally
        {
            _isUpdatingTree = false;
        }

        UpdateRemoveButtonEnabled();
    }

    /// <summary>
    /// 保存済みカメラ状態をビューポートへ適用する
    /// </summary>
    /// <param name="state">適用対象のカメラ状態</param>
    private void ApplyCameraState(CameraState state)
    {
        if (state == null)
        {
            return;
        }

        state.Normalize();

        Vector3 position = new Vector3(state.Position[0], state.Position[1], state.Position[2]);
        Quaternion rotation = new Quaternion(state.Rotation[0], state.Rotation[1], state.Rotation[2], state.Rotation[3]);
        Camera3D.ProjectionType projectionType = ParseProjectionType(state.ProjectionType);

        Application.Viewport.Event.SetProjectionType(projectionType);
        Application.Viewport.Event.MovePositionTo(position, true);
        Application.Viewport.Event.MoveRotationTo(rotation, true);
        Application.Viewport.Event.SetDistance(state.Distance, true);
        Application.Viewport.Event.SetSizeTo(state.Size, true);
        Application.Viewport.Event.SetFov(state.Fov, true);
    }

    /// <summary>
    /// Tree 選択から保存済みカメラ状態のインデックスを解決する
    /// </summary>
    /// <returns>選択インデックス、取得できない場合は -1</returns>
    private int GetSelectedStateIndex()
    {
        TreeItem selected = _tree.GetSelected();
        if (selected == null)
        {
            return -1;
        }

        Variant metadata = selected.GetMetadata((int)SelectionTreeColumn.No);
        if (metadata.VariantType != Variant.Type.Int)
        {
            return -1;
        }

        return (int)metadata;
    }

    /// <summary>
    /// 指定インデックスに対応する Tree アイテムを選択する
    /// </summary>
    /// <param name="index">選択対象のインデックス</param>
    private void SelectTreeItem(int index)
    {
        if (index < 0 || _savedCameraStates.Count == 0)
        {
            UpdateRemoveButtonEnabled();
            return;
        }

        TreeItem root = _tree.GetRoot();
        if (root == null)
        {
            return;
        }

        TreeItem item = root.GetFirstChild();
        while (item != null)
        {
            Variant metadata = item.GetMetadata((int)SelectionTreeColumn.No);
            if (metadata.VariantType == Variant.Type.Int && (int)metadata == index)
            {
                item.Select((int)SelectionTreeColumn.No);
                _tree.ScrollToItem(item);
                break;
            }

            item = item.GetNext();
        }

        UpdateRemoveButtonEnabled();
    }

    /// <summary>
    /// 保存済みカメラ状態の表示ラベルを作成する
    /// </summary>
    /// <param name="state">対象のカメラ状態</param>
    /// <returns>表示用ラベル文字列</returns>
    private static string BuildStateLabel(CameraState state)
    {
        if (state == null)
        {
            return "(null)";
        }

        state.Normalize();
        return $"{state.ProjectionType} | FOV {state.Fov:F1}";
    }

    /// <summary>
    /// 保存文字列から投影タイプを復元する
    /// </summary>
    /// <param name="projectionTypeText">保存された投影タイプ文字列</param>
    /// <returns>復元された投影タイプ</returns>
    private static Camera3D.ProjectionType ParseProjectionType(string projectionTypeText)
    {
        if (Enum.TryParse(projectionTypeText, true, out Camera3D.ProjectionType projectionType))
        {
            return projectionType;
        }

        return Camera3D.ProjectionType.Perspective;
    }

    /// <summary>
    /// Remove ボタンの有効状態を更新する
    /// </summary>
    private void UpdateRemoveButtonEnabled()
    {
        _buttonRemove.Disabled = GetSelectedStateIndex() < 0;
    }

    #endregion
}
