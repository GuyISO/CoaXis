# IPCテスターガイド

## 目的

Viewerの現行 `LoadModel` IPC契約を使ってモデルを読み込み、送信内容とViewer応答を確認する。プロトコル上の他イベントは、Viewer側に処理が実装されるまでテスト対象ではない。

## 起動

1. `dotnet build .\CoaXis.sln` を実行する。
2. CoaXisViewerを起動する。Viewerは既定で `CoaXisViewerPipe` を待ち受ける。
3. `dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj` を実行する。
4. リポジトリ内の `samples` フォルダーを検出できた場合、モデル実体に `modelentity.csv`、属性に `modelproperty.csv` が初期表示される。別の入力を使う場合は「選択...」から変更し、「LoadModelを送信」を押す。
5. 左側の通信履歴から要求を選ぶと、送信JSONと応答JSONを確認できる。

## 入力形式

- JSON: `ModelEntityDto` の配列。現在の `samples/modelentity.json` 形式。
- 実体CSV: ヘッダー付き16列。`Id,ParentId,Type,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,Visibility,IsCollapsed,IconPath,ScenePath,AlignToAabbCenter`。
- 属性CSV: ヘッダー付き5列。`Id,ParentId,PropertyType,ValueType,Value`。未指定の場合は空配列を送る。
- 数値はカルチャに依存しない小数点表記とする。CSVは引用符内のカンマと二重引用符を扱う。

## 切り分け

- 接続できない場合はViewerが起動していることとPipe名を確認する。
- Viewerの処理結果は応答JSON内の `payload.ok`、`payload.errorCode`、`payload.message` で確認する。
- ファイル読込、接続、応答待ちの失敗は履歴と応答JSON欄で確認する。ViewerからResultが返らない場合も接続・タイムアウトを切り分ける。
