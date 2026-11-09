# CoaXis

CoaXis は、3D モデルの可視化と事前アセット生成を分離して運用するプロジェクトです。このリポジトリでは主に Viewer・Protocol・IPC Tester を開発します。要件の変更時は、実装・仕様・運用ルールを同時に揃えます。

## プロジェクト

- CoaXisViewer
  - 3D 表示・操作・IPC 連携を担当する Godot プロジェクト
- CoaXisProtocol
  - 共通契約と IPC で使う DTO / 定義を管理するプロジェクト
- CoaXisIpcTester
  - JSON/CSVや操作テストデータを使い、Viewerへモデル集合の読込・追加・削除・変更・読み出し要求を送り、要求と応答を確認するWindowsデスクトップツール
- CoaXisUi
  - WinFormsアプリケーションで共通利用するテーマ、フォーム、ボタン、ナビゲーションレール、ロゴとSVGアイコンを提供するUIライブラリ。ブランドカラーはブルー `#4E95D9` とグレー `#7F7F7F`、フォントはSegoe UIに統一し、フラットデザインを基本とする。`CoaXisForm` を継承すると共通テーマが適用され、`CoaXisButton` や `CoaXisNavigationRail` を配置して利用できる。共有アセットは `CoaXisUiAssets` から参照できる
  - モニター間のDPI変更に対応するには、利用側のWinFormsプロジェクトで `ApplicationHighDpiMode` に `PerMonitorV2` を指定し、起動時に `ApplicationConfiguration.Initialize()` を呼ぶ
  - IpcTesterのフォームデザイナーではタブで各ページを切り替えて編集する。タブとレールのアイコンはデザイン時のプレビューで、実行時は通常のナビゲーションレールを使う。コンボボックスと表のサンプルはデザイン時専用のプレビューで、実行時のデータには混ざらない
  - `CoaXisComboBox`、`CoaXisLabel`、入力用の `CoaXisTextBox`、表示用の `CoaXisDisplayTextBox`、`CoaXisDataGridView`、`CoaXisTreeView`、`CoaXisCheckBox` を共通コントロールとして利用できる。IpcTesterの「開発中1」で表示を確認できる
  - 履歴表示などに `CoaXisListBox`、ステータス表示に `CoaXisStatusStrip` を利用できる。レイアウト用に `CoaXisPanel`、`CoaXisTableLayoutPanel`、`CoaXisFlowLayoutPanel`、`CoaXisSplitContainer`、`CoaXisTabControl`、`CoaXisTabPage` も用意している。ステータスバーの項目は追加時とテーマ適用時に配色される
  - `CoaXisForm` は表示時とテーマ変更時に、`ICoaXisThemedControl` を実装した子孫へテーマを適用する。テーマ適用が必要なコントロールはCoaXis.Uiの共通コントロールを使用する。フォーム外のコントロールは `ThemeManager.Apply` で再適用する
  - `CoaXisButton` は `Tag = "Primary"` で強調し、選択状態の表示には `IsSelected` を使う。テーマ固有のスクロールバーの外観は `Theme.ScrollBarThemeName` でWindowsのテーマ名を指定し、`null` はOSの既定テーマを使う
  - `CoaXisMessageBox.Show(owner, text, caption, buttons, icon)` はオーナー付きのモーダルダイアログを表示し、`DialogResult` を返す。ボタンとアイコンにはWinFormsの `MessageBoxButtons` と `MessageBoxIcon` を指定する

## 開発

```powershell
dotnet build .\CoaXis.sln
# Viewerを起動してから実行
dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj
```

## ドキュメント

- [Viewer・Protocol仕様](docs/specification/specification_integrated.md)
- [C#実装規約](docs/rules/implementation-conventions.md)
- [IPC Tester手順](docs/guides/viewer/IPC_TESTER_GUIDE.md)
