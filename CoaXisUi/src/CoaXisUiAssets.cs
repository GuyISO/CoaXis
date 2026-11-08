using System;
using System.IO;

namespace CoaXis.Ui;

/// <summary>CoaXis UIライブラリに含まれる共有SVGアセット名を提供する。</summary>
public static class CoaXisUiAssets
{
    /// <summary>CoaXisロゴのマニフェストリソース名。</summary>
    public const string LogoResourceName = "CoaXis.Ui.Assets.Logo.svg";

    /// <summary>IPCアイコンのマニフェストリソース名。</summary>
    public const string IpcIconResourceName = "CoaXis.Ui.Assets.Icons.ipc.svg";

    /// <summary>開発中ページ1のアイコンのマニフェストリソース名。</summary>
    public const string DevelopmentOneIconResourceName = "CoaXis.Ui.Assets.Icons.development-one.svg";

    /// <summary>開発中ページ2のアイコンのマニフェストリソース名。</summary>
    public const string DevelopmentTwoIconResourceName = "CoaXis.Ui.Assets.Icons.development-two.svg";

    /// <summary>共有アセットを読み取り用ストリームとして開く。</summary>
    /// <param name="resourceName">このライブラリに埋め込まれたリソース名。</param>
    /// <returns>呼び出し側が破棄するリソースストリーム。</returns>
    /// <exception cref="ArgumentException"><paramref name="resourceName"/>が空の場合。</exception>
    /// <exception cref="InvalidOperationException">指定されたリソースが見つからない場合。</exception>
    public static Stream OpenStream(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return typeof(CoaXisUiAssets).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"CoaXis UIアセットが見つかりません: {resourceName}");
    }
}
