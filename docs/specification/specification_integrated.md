# CoaXis Viewer・Protocol仕様

最終更新: 2026-10-18

この文書は、本リポジトリで管理するCoaXisViewer・CoaXisProtocol・CoaXisIpcTesterの現行仕様を記す。その他のCoaXisアプリケーションとSceneBuilderは対象外。Viewerが使うモデルシーンは外部で生成する。

## Viewer

### 起動と構成

Godot AutoLoadは`Application`のみ。Infrastructure（Log、Setting、Asset、Ipc、Input）とDomain（Command、Model、Ui、Viewport）の各Hubを構成する。モデル機能は`Application.Model.Entity`と`Application.Model.Property`に分かれる。

### ModelEntity

| 型 | 責務 |
|---|---|
| `ModelEntityLoadHub` | Entity集合の置換・クリア、Registry登録と階層解決、Node生成、シーンロード開始を調整する。 |
| `ModelEntityMapper` / `ModelEntityFactory` | MapperはDTOを内部モデルへ変換する。FactoryはDTOからEntityを生成し、IDの空・重複を検証する。 |
| `ModelEntityRegistryHub` / `ModelPropertyRegistryHub` | EntityとPropertyの識別子集合・論理階層をそれぞれ所有する。 |
| `ModelEntitySceneHub` | シーンロードキューを管理し、Entityのロード状態を更新する。 |
| `ModelEntity` / `ModelEntityStateHub` | Entityが状態値を所有し、位置・回転を対応Nodeへ同期する。StateHubは値を変更せず、対象IDのみ通知する。 |
| `ModelEntityVisualHub` | Visibilityと親階層から実効表示を解決してLayerへ反映する。選択強調などの表示効果も扱う。 |

EntityのStatusは専用遷移メソッドで変更する。通常遷移は`Unloaded→Initialized→Registered→Loading→Loaded|LoadFailed`、ScenePathが空なら`Registered→Loaded`、破棄は`Registered|Loading|Loaded|LoadFailed→Disposed`。`IsVisible`はVisibility設定と親階層から導出する。

TreeItemはUIが構築・保持し、Model DomainはUI Nodeを保持しない。Pick・Measurement・Load・Scene機能はEntity配下、Property LoadはProperty配下に置く。

### Scene

Viewerは事前生成済みGodotシーン（`.scn` / `.tscn`）をロードする。SceneBuilderによる生成処理は本リポジトリで管理しない。

ロードしたシーンのルートは`ModelNode.SceneRoot`から扱う。`Emphasize`または`SpinAppeal`が必要な場合に限りPivotを生成し、SceneRootをその配下へ移す。固定NodePathには依存しない。

カメラFitはNodeの可視状態と、MeshInstance3DのLayer・`ViewportDisplayHub.ActiveLayers`の共通Layerを考慮する。対象メッシュがない場合はカメラ状態を変更しない。

## IPC

- Named PipeでUTF-8の1行1JSONメッセージを送受信する。
- Protocolの現行バージョンは`1.0.0`。Viewerは各要求のバージョンが空でないことと、メジャーバージョン一致を検査する。起動時のバージョン照会は行わない。
- Protocolは複数のイベント名を定義するが、Viewerが現在処理する要求は`LoadModel`のみ。その他の未対応要求には`UNSUPPORTED_EVENT_TYPE`を返す。Viewer発イベントの送信処理は未実装。
- 要求への応答は`Result`イベントで返し、`correlationId`に要求の`eventId`を設定する。payloadは`ok`、`request`、`errorCode`、`message`を持つ。
- `LoadModel`のpayloadには`entities`と`properties`が必須。`entities`は1件以上、`properties`は空配列を許可する。
- メインスレッド要求の待機上限は2秒。キュー待ち要求はタイムアウト時に取り消す。処理開始後は中断せず、結果不明として返すため、送信側は確認なしに再送しない。

## IPC Tester

`CoaXisIpcTester`は外部クライアントとして`LoadModel`を送信する。入力形式と操作手順は[IPC Tester手順](../guides/viewer/IPC_TESTER_GUIDE.md)を参照。
