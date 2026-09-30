# IPCテスターガイド

## 目的

Viewer内のテストコードによるJSON/CSV直接読込をなくし、実運用と同じNamed Pipe IPC経由でモデルを読み込んで動作確認する。送信内容とViewerの応答を画面上で追えるため、モデル表示とIPC処理結果を合わせて検証できる。

## 起動

1. `dotnet build .\CoaXis.sln` を実行する。
2. CoaXisViewerを起動する。Viewerは既定で `CoaXisViewerPipe` を待ち受ける。
3. `dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj` を実行する。
4. モデル実体JSONまたはCSVを選ぶ。属性が必要な場合は属性CSVも選び、「LoadModelを送信」を押す。
5. 左側の通信履歴から要求を選ぶと、送信JSONと応答JSONを確認できる。

## 入力形式

- JSON: `ModelEntityDto` の配列。現在の `samples/modelentity.json` 形式。
- 実体CSV: ヘッダー付き16列。`Id,ParentId,Type,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,Visibility,IsCollapsed,IconPath,ScenePath,AlignToAabbCenter`。
- 属性CSV: ヘッダー付き5列。`Id,ParentId,PropertyType,ValueType,Value`。未指定の場合は空配列を送る。
- 数値はカルチャに依存しない小数点表記とする。CSVは引用符内のカンマと二重引用符を扱う。

## 切り分け

- 接続できない場合はViewerが起動していることとPipe名を確認する。
- Viewerの処理結果は応答JSON内の `payload.ok`、`payload.errorCode`、`payload.message` で確認する。
- ファイル読込や接続で例外になった場合も履歴に失敗として残り、応答JSON欄にエラー内容を表示する。