using System;

/// <summary>
/// ModelProperty の表示設定を変更する Undo/Redo 対応コマンド
/// </summary>
public sealed partial class SetModelPropertyVisibilityCommand : BaseCommand
{
    #region Fields

    private readonly Guid _propertyId;
    private readonly ModelVisibility _previousVisibility;
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
    {
        _propertyId = propertyId;
        _previousVisibility = Application.Model.Property.Registry.Get(propertyId)?.Visibility ?? ModelVisibility.Inherit;
        _nextVisibility = nextVisibility;
    }

    #endregion

    #region Methods

    /// <summary>
    /// コマンドを実行する
    /// </summary>
    public override void Do()
    {
        Apply("Do", _nextVisibility);
    }

    /// <summary>
    /// 実行したコマンドを元に戻す
    /// </summary>
    public override void Undo()
    {
        Apply("Undo", _previousVisibility);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// 対象Propertyへ表示設定を適用する
    /// </summary>
    private void Apply(string operation, ModelVisibility visibility)
    {
        ModelProperty property = Application.Model.Property.Registry.Get(_propertyId);
        if (property == null)
        {
            LogSkip(operation, $"property '{_propertyId}' is not registered.");
            return;
        }

        property.Visibility = visibility;
    }

    #endregion
}
