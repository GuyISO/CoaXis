# CoaXis.Ui

WinFormsアプリケーションで共通利用するテーマ、フォーム、コントロール、ナビゲーションレール、ロゴとSVGアイコンを提供するUIライブラリです。ブランドカラーはブルー `#4E95D9` とグレー `#7F7F7F`、フォントはSegoe UIに統一し、フラットデザインを基本とします。共有アセットは `CoaXisUiAssets` から参照できます。

## 利用方法

- `CoaXisForm` を継承すると共通テーマが適用されます。`CoaXisButton` や `CoaXisNavigationRail` などの共通コントロールを配置して利用できます。
- モニター間のDPI変更に対応するには、利用側のWinFormsプロジェクトで `ApplicationHighDpiMode` に `PerMonitorV2` を指定し、起動時に `ApplicationConfiguration.Initialize()` を呼びます。
- 共通コントロールは `CoaXisComboBox`、`CoaXisLabel`、入力用の `CoaXisTextBox`、表示用の `CoaXisDisplayTextBox`、`CoaXisDataGridView`、`CoaXisTreeView`、`CoaXisCheckBox`、`CoaXisListBox`、`CoaXisStatusStrip` のほか、`CoaXisPanel`、`CoaXisTableLayoutPanel`、`CoaXisFlowLayoutPanel`、`CoaXisSplitContainer`、`CoaXisTabControl`、`CoaXisTabPage` を用意しています。
- 表示役割は `CoaXisButton.Variant`、`CoaXisLabel.Role`、コンテナーの `SurfaceRole` で指定します。`Tag` はアプリケーション固有データ用に残し、テーマ設定には使いません。

## テーマ

- テーマは `Theme` を継承して色とフォントを定義し、`ThemeManager.SetTheme` でプロセス全体へ設定します。UIスレッドから切り替えると、ハンドル作成済みの `CoaXisForm` が更新されます。
- 独自テーマでは `Background`、`Surface`、`Border`、`Text`、`Primary`、`MutedText`、`Selection`、`GridHeader`、`Header`、`HeaderHover` を実装します。`InputBackground`、`DisplayBackground`、ボタン状態色、フォント、スクロールバー名は必要な場合だけ上書きします。
- 文字色と背景色の組み合わせで十分なコントラストを確保し、テーマのフォントは適用中に破棄しません（ライブラリも破棄しません）。
- 標準ヘッダーの切替ボタンは組み込みのライト／ダークテーマ専用で、独自テーマでは表示されません。
- `CoaXisForm` は表示時とテーマ変更時に、`ICoaXisThemedControl` を実装した子孫へテーマを適用します。表示後にコントロールを追加した場合や、フォーム外のコントロールツリーでは `ThemeManager.Apply(root)` を呼び出します。`CoaXisNavigationRail.AddPage` は追加ページへ自動適用します。
- テーマ固有のスクロールバーは `Theme.ScrollBarThemeName` でWindowsのテーマ名を指定し、`null` はOSの既定テーマを使います。
- ステータスバーの項目は追加時とテーマ適用時に配色されます。

## IpcTesterでの確認

- フォームデザイナーではタブで各ページを切り替えて編集します。タブとレールのアイコンはデザイン時のプレビューで、実行時は通常のナビゲーションレールを使います。
- コンボボックスと表のサンプルはデザイン時専用のプレビューで、実行時のデータには混ざりません。
- 共通コントロールの表示はIpcTesterの「開発中1」で確認できます。
- `CoaXisMessageBox.Show(owner, text, caption, buttons, icon)` はオーナー付きのモーダルダイアログを表示し、`DialogResult` を返します。ボタンとアイコンにはWinFormsの `MessageBoxButtons` と `MessageBoxIcon` を指定します。
