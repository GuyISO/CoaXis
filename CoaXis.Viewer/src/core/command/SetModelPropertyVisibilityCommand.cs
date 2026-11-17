using System;

/// <summary>
/// ModelProperty の表示設定を変更する Undo/Redo 対応コマンド、バッチで複数Propertyを変更することも可能
/// </summary>
public sealed partial class SetModelPropertyVisibilityCommand : BaseCommand
{
    #region Fields

    private readonly Guid[] _propertyIds;
    private readonly ModelVisibility[] _previousVisibilities;
    private readonly ModelVisibility _nextVisibility;

    #endregion

    #region Properties

    /// <summary>
    /// コマンドの説明、ログ出力時に使用される
    /// </summary>
    public override string Description => "Set model property visibility";

    #endregion

    #region Constructors

    /// <summary>
    /// コンストラクタ、指定されたPropertyの表示設定を変更するコマンド
    /// </summary>
    /// <param name="propertyId">表示設定を変更するPropertyの識別子</param>
    /// <param name="nextVisibility">変更後の表示設定</param>
    public SetModelPropertyVisibilityCommand(Guid propertyId, ModelVisibility nextVisibility)
        : this(new[] { propertyId }, nextVisibility)
    {
    }

    /// <summary>
    /// コンストラクタ、指定された複数Propertyの表示設定を変更するコマンド
    /// </summary>
    /// <param name="propertyIds">表示設定を変更するPropertyの識別子の配列</param>
    /// <param name="nextVisibility">変更後の表示設定</param>
    public SetModelPropertyVisibilityCommand(Guid[] propertyIds, ModelVisibility nextVisibility)
    {
        _propertyIds = propertyIds ?? throw new ArgumentNullException(nameof(propertyIds));
        _previousVisibilities = new ModelVisibility[_propertyIds.Length];
        for (int i = 0; i < _propertyIds.Length; i++)
        {
            _previousVisibilities[i] = Application.Model.Property.Registry.Get(_propertyIds[i])?.Visibility ?? ModelVisibility.Inherit;
        }
        _nextVisibility = nextVisibility;
    }

    #endregion

    #region Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public override void Do()
    {
        for (int i = 0; i < _propertyIds.Length; i++)
        {
            Apply("Do", _propertyIds[i], _nextVisibility);
        }
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        for (int i = 0; i < _propertyIds.Length; i++)
        {
            Apply("Undo", _propertyIds[i], _previousVisibilities[i]);
        }
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 対象Propertyへ表示設定を適用する
    /// </summary>
    private void Apply(string operation, Guid propertyId, ModelVisibility visibility)
    {
        ModelProperty property = Application.Model.Property.Registry.Get(propertyId);
        if (property == null)
        {
            LogSkip(operation, $"property '{propertyId}' is not registered.");
            return;
        }

        property.Visibility = visibility;
    }

    #endregion
}