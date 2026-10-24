# IPCテスターガイド

## 目的

IPC Testerの現行 `ReplaceModelSet` IPC契約を使ってモデル集合を置換し、送信内容とViewer応答を確認する。Viewerは部分追加・削除のIPC要求も処理するが、このテスターのUIはそれらの送信に対応していない。要求へのResultを待つ間に先着した非同期通知は通信履歴に記録し、要求への応答は`correlationId`が一致する`Result`として識別する。Result受信後は接続を閉じるため、継続的な通知購読には対応しない。

## 起動

1. `dotnet build .\CoaXis.sln` を実行する。
2. CoaXisViewerを起動する。Viewerは既定で `CoaXisViewerPipe` を待ち受ける。
3. `dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj` を実行する。
4. リポジトリ内の `samples` フォルダーを検出できた場合、モデル実体に `modelentity.csv`、属性に `modelproperty.csv` が初期表示される。別の入力を使う場合は「選択...」から変更し、「ReplaceModelSetを送信」を押す。
5. 左側の通信履歴から要求を選ぶと、送信JSONと応答JSONを確認できる。

## 入力形式

- JSON: `ModelEntityDto` の配列。現在の `samples/modelentity.json` 形式。
- Entity/PropertyのアイコンはProtocol定義の`IconId`を指定する。使用可能な値は`Annotation`、`AnnotationSet`、`Body`、`Component`、`HybridBody`、`Instance`、`Line`、`Point`、`Product`、`Surface`、`Target`。`IconId`の省略または空欄はアイコンなしを意味し、既定アイコンはない。画像パスでは指定しない。JSONで明示的に`null`を指定した場合は不正値となる。
- 実体CSV: ヘッダー付き15列。`Id,ParentId,Type,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,Visibility,IsCollapsed,IconId,ScenePath`。
- 属性CSV: ヘッダー付き5列（`Id,ParentId,PropertyType,ValueType,Value`）、または表示設定とパーティクル指定を含む10列（`Id,ParentId,PropertyType,ValueType,Value,Visibility,IconId,ParticleType,ParticleTextureId,ParticleColor`）。10列の空欄は既定値（`Inherit`・アイコンなし・`None`・`Star`・白）として扱う。未指定の場合は空配列を送る。
- Protocol v2ではアイコンの画像パス指定を`IconId`に置き換えている。v1クライアントはProtocol契約に合わせて更新する。
- 数値はカルチャに依存しない小数点表記とする。CSVは引用符内のカンマと二重引用符を扱う。

## 切り分け

- 接続できない場合はViewerが起動していることとPipe名を確認する。
- Viewerの処理結果は応答JSON内の `payload.ok`、`payload.errorCode`、`payload.message` で確認する。
- ファイル読込、接続、応答待ちの失敗は履歴と応答JSON欄で確認する。ViewerからResultが返らない場合も接続・タイムアウトを切り分ける。
