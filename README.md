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
  - WinFormsアプリケーションで共通利用するテーマ、フォーム、ボタン、ナビゲーションレール、ロゴとSVGアイコンを提供するUIライブラリ。`CoaXisForm` を継承すると共通テーマが適用され、`CoaXisButton` や `CoaXisNavigationRail` を配置して利用できる。共有アセットは `CoaXisUiAssets` から参照できる
  - モニター間のDPI変更に対応するには、利用側のWinFormsプロジェクトで `ApplicationHighDpiMode` に `PerMonitorV2` を指定し、起動時に `ApplicationConfiguration.Initialize()` を呼ぶ
  - IpcTesterのフォームデザイナーではタブで各ページを切り替えて編集する。タブとレールのアイコンはデザイン時のプレビューで、実行時は通常のナビゲーションレールを使う。コンボボックスと表のサンプルはデザイン時専用のプレビューで、実行時のデータには混ざらない
  - `CoaXisComboBox`、`CoaXisLabel`、`CoaXisTextBox`、`CoaXisDataGridView`、`CoaXisTreeView`、`CoaXisCheckBox` を共通コントロールとして利用できる。IpcTesterの「開発中1」で表示を確認できる
  - 履歴表示などに `CoaXisListBox`、ステータス表示に `CoaXisStatusStrip` を利用できる。レイアウト用に `CoaXisPanel`、`CoaXisTableLayoutPanel`、`CoaXisFlowLayoutPanel`、`CoaXisSplitContainer`、`CoaXisTabControl`、`CoaXisTabPage` も用意している。ステータスバーの項目は追加時とテーマ適用時に配色される
  - `CoaXisForm` は表示時とヘッダーのテーマ切り替え時に子コントロールへテーマを適用する。既存のWinFormsコントロールもその対象となる。`ThemeManager.SetTheme` を直接呼んだ場合は、表示中の対象に `ThemeManager.Apply` を呼んで再適用する
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
