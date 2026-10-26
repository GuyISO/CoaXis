# IPCテスターガイド

## 目的

IPC Testerからモデル集合の読み込み・追加・削除・変更・読み出し要求を送り、Viewerの応答を確認する。要求へのResultを待つ間に先着した非同期通知は通信履歴に記録し、要求への応答は`correlationId`が一致する`Result`として識別する。Result受信後は接続を閉じるため、継続的な通知購読には対応しない。

## 起動

1. `dotnet build .\CoaXis.sln` を実行する。
2. CoaXisViewerを起動する。Viewerは既定で `CoaXisViewerPipe` を待ち受ける。
3. `dotnet run --project .\CoaXisIpcTester\CoaXis.IpcTester.csproj` を実行する。
4. リポジトリ内の `samples` フォルダーを検出できた場合、モデル実体に `modelentity.csv`、属性に `modelproperty.csv`、操作テストデータに `modelset-test-data.json` が初期表示される。別の入力を使う場合は各「選択...」から変更する。
5. 「ReplaceModelSetを送信」でCSVのモデル集合を読み込む。追加などの操作は要求選択欄から要求を選び、「選択した要求を送信」を押す。`ClearModelSet`はテストデータJSONなしで送信できる。
6. `AddModelSet`の追加対象にはCSV内のEntityを親としているテストEntityと、その子Entity・Propertyを含む。同じ追加を再送するとID衝突で拒否される。`RemoveModelSet`はこの追加データを削除するので、追加後に実行すれば試験データを片付けられる。
7. `ModifyModelSet`はCSV内のEntity位置・表示状態とProperty値・色を変更する。`ReadModelSet`はそのEntityとPropertyを指定IDで読み出す。サンプルIDは現行CSVに対応する。
8. 左側の通信履歴から要求を選ぶと、送信JSONと応答JSONを確認できる。Read結果の`modelSet`も応答JSON欄で確認できる。

## 入力形式

- JSON実体: `ModelEntityDto` の配列。
- 操作テストデータJSON: `add`、`remove`、`modify`、`read`の各payloadを持つ。`samples/modelset-test-data.json`をひな形として使う。各要求は現在のViewer Protocol payload形式に従う。
- Entity/PropertyのアイコンはProtocol定義の`IconId`を指定する。使用可能な値は`Annotation`、`AnnotationSet`、`Body`、`Component`、`HybridBody`、`Instance`、`Line`、`Point`、`Product`、`Surface`、`Target`。`IconId`の省略または空欄はアイコンなしを意味し、既定アイコンはない。画像パスでは指定しない。JSONで明示的に`null`を指定した場合は不正値となる。
- 実体CSV: ヘッダー付き15列、または末尾に`IsPropertyTreeVisible`（`true`/`false`、Viewport上のProperty OverlayTree初期表示）を加えた16列。`Id,ParentId,Type,Name,PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,RotationW,Visibility,IsCollapsed,IconId,ScenePath[,IsPropertyTreeVisible]`。15列の場合は`false`として扱う。JSONの`ModelEntityDto`も`IsPropertyTreeVisible`（省略時`false`）を持つ。
- 属性CSV: ヘッダー付き5列（`Id,ParentId,PropertyType,ValueType,Value`）、または表示設定とパーティクル指定を含む11列（`Id,ParentId,PropertyType,ValueType,Value,Visibility,IconId,ParticleEmitterType,ParticleBehaviorType,ParticleTextureId,ParticleColor`）。11列形式の空欄は既定値（`Inherit`・アイコンなし・発生源`None`・挙動`None`・`Star`・白）として扱う。発生源は`None`/`Point`/`Box`/`Sphere`/`Surface`、挙動は`None`/`Disperse`/`Sparkle`/`Emerge`。発生源と挙動は両方`None`か両方指定する。未指定の場合は空配列を送る。
- Protocol v2ではアイコンの画像パス指定を`IconId`に置き換えている。v1クライアントはProtocol契約に合わせて更新する。
- 数値はカルチャに依存しない小数点表記とする。CSVは引用符内のカンマと二重引用符を扱う。

## 切り分け

- 接続できない場合はViewerが起動していることとPipe名を確認する。
- Viewerの処理結果は応答JSON内の `payload.ok`、`payload.errorCode`、`payload.message` で確認する。
- ファイル読込、接続、応答待ちの失敗は履歴と応答JSON欄で確認する。ViewerからResultが返らない場合も接続・タイムアウトを切り分ける。
