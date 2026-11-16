namespace CoaXis.Ui;

/// <summary>CoaXisボタンの基本的な表示バリエーション。</summary>
public enum CoaXisButtonVariant
{
    /// <summary>通常操作に使う控えめなボタン。</summary>
    Secondary,

    /// <summary>主操作に使うアクセントカラーのボタン。</summary>
    Primary
}

/// <summary>CoaXisコンテナーに適用する背景の役割。</summary>
public enum CoaXisSurfaceRole
{
    /// <summary>カードや区切られた領域の背景。</summary>
    Surface,

    /// <summary>アプリケーションの基底背景。</summary>
    Background
}

/// <summary>CoaXisラベルに適用する文字の役割。</summary>
public enum CoaXisLabelRole
{
    /// <summary>通常の本文。</summary>
    Standard,

    /// <summary>ブランドを示す見出し。</summary>
    Brand,

    /// <summary>補助情報。</summary>
    Muted
}
