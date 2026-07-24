using Godot;
using System.Collections.Generic;

/// <summary>
/// 繧ｳ繝槭Φ繝牙ｱ･豁ｴ陦ｨ遉ｺ縺ｨ謫堺ｽ懃畑縺ｮ繝代ロ繝ｫ
/// </summary>
public partial class CommandUi : PanelContainer
{
    #region Fields

    private bool _isInitialized = false; // 蛻晏屓迥ｶ諷矩夂衍繧貞女縺代◆縺九□縺代ｒ菫晄戟縺吶ｋ
    private bool _isUpdatingTree = false;
    private bool _isRequestingCursorMove = false;
    private bool _isRebuildQueued = false;
    private int _cursor = 0;
    private readonly List<CommandBase> _history = new();

    // 髢｢騾｣繝弱・繝峨・繧ｭ繝｣繝・す繝･
    private Tree _tree = null!;

    private Color _doColor;
    private Color _undoColor;

    #endregion

    #region Lifecycle

    public override void _Ready()
    {
        EnsureChildNodes();
        SubscribeUiEvents();
        SubscribeApplicationEvents();
        ApplySettings();
    }

    public override void _ExitTree()
    {
        UnsubscribeUiEvents();
        UnsubscribeApplicationEvents();

        base._ExitTree();
    }

    public override void _Process(double delta)
    {
        if (!_isInitialized)
        {
            Application.Command.Event.AskState();
        }
    }

    #endregion

    #region Events

    /// <summary>
    /// 蟄舌ヮ繝ｼ繝峨ｒ隗｣豎ｺ縺励√ヵ繧｣繝ｼ繝ｫ繝峨↓菫晄戟縺吶ｋ
    /// </summary>
    private void EnsureChildNodes()
    {
        _tree = (Tree)FindChild("Tree");
        _tree.Columns = 4;
        _tree.SetColumnTitle(0, "No");
        _tree.SetColumnTitle(1, "Name");
        _tree.SetColumnTitle(2, "Description");
        _tree.SetColumnTitle(3, "State");
        _tree.SetColumnExpand(0, false);
        _tree.SetColumnExpand(1, true);
        _tree.SetColumnExpand(2, true);
        _tree.SetColumnExpand(3, false);
        _tree.SetColumnCustomMinimumWidth(0, 64);
        _tree.SetColumnCustomMinimumWidth(3, 64);
    }
    
    /// <summary>
    /// UI繧､繝吶Φ繝医・雉ｼ隱ｭ繧帝幕蟋九☆繧・    /// </summary>
    private void SubscribeUiEvents()
    {
        _tree.ItemSelected += OnTreeItemSelected;
        _tree.ItemActivated += OnTreeItemActivated;
    }

    /// <summary>
    /// UI繧､繝吶Φ繝医・雉ｼ隱ｭ繧定ｧ｣髯､縺吶ｋ
    /// </summary>
    private void UnsubscribeUiEvents()
    {
        _tree.ItemSelected -= OnTreeItemSelected;
        _tree.ItemActivated -= OnTreeItemActivated;
    }

    /// <summary>
    /// Application繧､繝吶Φ繝医・雉ｼ隱ｭ繧帝幕蟋九☆繧・    /// </summary>
    private void SubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified += ApplySettings;
        Application.Command.Event.StateNotified += OnStateNotified;
    }

    /// <summary>
    /// Application繧､繝吶Φ繝医・雉ｼ隱ｭ繧定ｧ｣髯､縺吶ｋ
    /// </summary>
    private void UnsubscribeApplicationEvents()
    {
        Application.Setting.Event.SettingsNotified -= ApplySettings;
        Application.Command.Event.StateNotified -= OnStateNotified;
    }

    /// <summary>
    /// 繧ｳ繝槭Φ繝牙ｱ･豁ｴ迥ｶ諷九・騾夂衍繧貞女縺大叙縺｣縺溘→縺阪↓蜻ｼ縺ｳ蜃ｺ縺輔ｌ繧九う繝吶Φ繝医ワ繝ｳ繝峨Λ
    /// </summary>
    /// <param name="history">騾夂衍縺輔ｌ縺溷ｱ･豁ｴ驟榊・</param>
    /// <param name="cursor">騾夂衍縺輔ｌ縺溘き繝ｼ繧ｽ繝ｫ菴咲ｽｮ</param>
    private void OnStateNotified(CommandBase[] history, int cursor)
    {
        _history.Clear();
        if (history != null)
        {
            _history.AddRange(history);
        }

        _cursor = cursor;
        _isInitialized = true;
        _isRequestingCursorMove = false;
        QueueRebuildTimelineTree();
    }

    /// <summary>
    /// 螻･豁ｴ繝・Μ繝ｼ縺ｮ驕ｸ謚槫､画峩譎ゅ↓蜻ｼ縺ｳ蜃ｺ縺輔ｌ繧九う繝吶Φ繝医ワ繝ｳ繝峨Λ
    /// </summary>
    private void OnTreeItemSelected()
    {
        RequestCursorMoveFromSelection();
    }

    /// <summary>
    /// 螻･豁ｴ繝・Μ繝ｼ縺ｮ繧｢繧､繝・Β縺檎｢ｺ螳壹＆繧後◆縺ｨ縺阪↓蜻ｼ縺ｳ蜃ｺ縺輔ｌ繧九う繝吶Φ繝医ワ繝ｳ繝峨Λ
    /// </summary>
    private void OnTreeItemActivated()
    {
        RequestCursorMoveFromSelection();
    }

    /// <summary>
    /// 迴ｾ蝨ｨ驕ｸ謚槭＆繧後※縺・ｋ螻･豁ｴ陦後∈縺ｮ繧ｫ繝ｼ繧ｽ繝ｫ遘ｻ蜍輔ｒ繝ｪ繧ｯ繧ｨ繧ｹ繝医☆繧・    /// </summary>
    private void RequestCursorMoveFromSelection()
    {
        if (_isUpdatingTree || _isRequestingCursorMove)
        {
            return;
        }

        TreeItem selected = _tree.GetSelected();
        if (selected == null)
        {
            return;
        }

        Variant metadata = selected.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.Int)
        {
            return;
        }

        int nextCursor = (int)metadata + 1;
        if (nextCursor == _cursor)
        {
            return;
        }

        _isRequestingCursorMove = true;
        Application.Command.Event.SetCursor(nextCursor);
    }

    #endregion

    #region Internal Helpers

    /// <summary>
    /// 險ｭ螳壼､繧貞渚譏縺吶ｋ
    /// </summary>
    private void ApplySettings()
    {
        ColorSettings c = Application.Setting.Service.Current.Color;
        _doColor = Color.FromHtml(c.CommandDoColor);
        _undoColor = Color.FromHtml(c.CommandUndoColor);
    }

    /// <summary>
    /// 繧ｿ繧､繝繝ｩ繧､繝ｳ繝・Μ繝ｼ蜀肴ｧ狗ｯ峨ｒ驕・ｻｶ繧ｭ繝･繝ｼ縺ｸ遨阪・
    /// </summary>
    private void QueueRebuildTimelineTree()
    {
        if (_isRebuildQueued)
        {
            return;
        }

        _isRebuildQueued = true;
        CallDeferred(MethodName.RebuildTimelineTreeDeferred);
    }

    /// <summary>
    /// 驕・ｻｶ蜻ｼ縺ｳ蜃ｺ縺励〒繧ｿ繧､繝繝ｩ繧､繝ｳ繝・Μ繝ｼ繧貞・讒狗ｯ峨☆繧・    /// </summary>
    private void RebuildTimelineTreeDeferred()
    {
        _isRebuildQueued = false;
        if (_tree == null || !GodotObject.IsInstanceValid(_tree))
        {
            return;
        }

        RebuildTimelineTree();
    }

    /// <summary>
    /// 繧ｿ繧､繝繝ｩ繧､繝ｳ繝・Μ繝ｼ繧堤樟蝨ｨ縺ｮ螻･豁ｴ迥ｶ諷九〒蜀肴ｧ狗ｯ峨☆繧・    /// </summary>
    private void RebuildTimelineTree()
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

            for (int i = 0; i < _history.Count; i++)
            {
                CommandBase command = _history[i];
                TreeItem item = _tree.CreateItem(root);
                if (item == null)
                {
                    continue;
                }

                item.SetMetadata(0, i);
                item.SetText(0, i.ToString());
                item.SetText(1, command?.GetType().Name ?? "(null)");
                item.SetText(2, command?.Description ?? string.Empty);

                string state = ResolveState(i, _cursor);
                Color color = ResolveStateColor(i, _cursor);
                item.SetText(3, state);

                for (int column = 0; column < 4; column++)
                {
                    item.SetCustomColor(column, color);
                }

                if (i == _cursor - 1)
                {
                    item.Select(0);
                }
            }
        }
        finally
        {
            _isUpdatingTree = false;
        }
    }

    /// <summary>
    /// 螻･豁ｴ繧､繝ｳ繝・ャ繧ｯ繧ｹ縺ｫ蟇ｾ蠢懊☆繧狗憾諷区枚蟄怜・繧定ｿ斐☆
    /// </summary>
    private static string ResolveState(int index, int cursor)
    {
        if (index < cursor)
        {
            return "Do";
        }

        return "Undo";
    }

    /// <summary>
    /// 螻･豁ｴ繧､繝ｳ繝・ャ繧ｯ繧ｹ縺ｫ蟇ｾ蠢懊☆繧玖｡ｨ遉ｺ濶ｲ繧定ｿ斐☆
    /// </summary>
    private Color ResolveStateColor(int index, int cursor)
    {
        if (index < cursor)
        {
            return _doColor;
        }

        return _undoColor;
    }

    #endregion
}
