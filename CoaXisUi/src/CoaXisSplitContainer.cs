using System.Windows.Forms;

namespace CoaXis.Ui;

/// <summary>CoaXisテーマの分割コンテナー。</summary>
public sealed class CoaXisSplitContainer : SplitContainer
{
    /// <summary>分割コンテナーを作成する。</summary>
    public CoaXisSplitContainer() => ThemeManager.Apply(this);
}
