# CoaXis Viewer・Protocol仕様

最終更新: 2026-10-18

この文書は、本リポジトリで管理するCoaXisViewer・CoaXisProtocol・CoaXisIpcTesterの現行仕様を記す。その他のCoaXisアプリケーションとSceneBuilderは対象外。Viewerが使うモデルシーンは外部で生成する。

## Viewer

### 起動と構成

Godot AutoLoadは`Application`のみ。Infrastructure（Log、Setting、Asset、Ipc、Input）とDomain（Command、Model、Ui、Viewport）の各Hubを構成する。モデル機能は`Application.Model.Entity`と`Application.Model.Property`に分かれる。

### ModelEntity

| 型 | 責務 |
|---|---|
| `ModelSetHub` | Entity・Propertyをまたぐ全件置換、部分追加/置換、連鎖削除を調整する。DTO検証はFactory、各Registry更新はEntity/PropertyのLoadHubへ委譲する。 |
| `ModelEntityLoadHub` | 検証済みEntityの追加/同一ID置換/削除、Node接続とシーンロード開始を管理する。 |
| `ModelPropertyLoadHub` | 検証済みPropertyの追加/同一ID置換/連鎖削除を管理する。 |
| `ModelFactory` | Entity/Property DTOをMapperで内部モデルへ変換し、IDと階層を検証してModelSetまたは部分更新用モデルを生成する。 |
| `ModelEntityMapper` / `ModelPropertyMapper` | それぞれのDTOを対応する内部モデルへ単体変換する。集合全体のID・階層検証は`ModelFactory`が担う。 |
| `ModelEntityRegistryHub` / `ModelPropertyRegistryHub` | EntityとPropertyの識別子集合・論理階層をそれぞれ所有する。 |
| `ModelEntitySelectionHub` | 選択中Entity ID集合を所有し、選択操作で更新する。実際に集合が変わった場合に`SelectionChanged`を通知する。IPCやProtocolには依存しない。 |
| `ModelEntitySceneHub` | シーンロードキューを管理し、Entityのロード状態を更新する。 |
| `ModelEntity` / `ModelEntityStateHub` | Entityが状態値を所有し、位置・回転を対応Nodeへ同期する。StateHubは値を変更せず、対象IDのみ通知する。 |
| `ModelEntityVisualHub` | Visibilityと親階層から実効表示を解決してLayerへ反映する。選択強調などの表示効果も扱う。 |

EntityのStatusは専用遷移メソッドで変更する。通常遷移は`Unloaded→Initialized→Registered→Loading→Loaded|LoadFailed`、ScenePathが空なら`Registered→Loaded`、破棄は`Registered|Loading|Loaded|LoadFailed→Disposed`。`IsVisible`はVisibility設定と親階層から導出する。親IDをたどってRootへ到達しないEntityはRegistryに保持するが、描画Nodeを作成せずSceneもロードしない。親が後から登録されRootへ接続された時点で、該当Entityと子孫のNode生成・Sceneロードを開始する。

TreeItemはUIが構築・保持し、Model DomainはUI Nodeを保持しない。Pick・Measurement・Load・Scene機能はEntity配下、Property LoadはProperty配下に置く。

### Scene

Viewerは事前生成済みGodotシーン（`.scn` / `.tscn`）をロードする。SceneBuilderによる生成処理は本リポジトリで管理しない。

ロードしたシーンのルートは`ModelNode.SceneRoot`から扱う。`Emphasize`または`SpinAppeal`が必要な場合に限りPivotを生成し、SceneRootをその配下へ移してメッシュAABB中心をPivot原点に合わせる。見た目の位置を保つためPivot配下を逆方向へ補正し、`ModelNode`の位置は変更しない。固定NodePathには依存しない。

カメラFitはNodeの可視状態と、MeshInstance3DのLayer・`ViewportDisplayHub.ActiveLayers`の共通Layerを考慮する。対象メッシュがない場合はカメラ状態を変更しない。

## IPC

- Named PipeでUTF-8の1行1JSONメッセージを送受信する。
- `IpcHub`はNamed Pipeの接続・受信・送信、メインスレッドへの要求振り分け、応答エンベロープ生成を担う。ドメイン状態は所有せず、ModelEntity選択通知ではSelectionHubの変更通知を受けて現在のID集合をProtocol payloadへ変換する。
- Viewerは各要求のバージョンが空でないことと、メジャーバージョン一致を検査する。起動時のバージョン照会は行わない。
- Viewerが処理する要求は`ReplaceModelSet`、`AddOrReplaceModelEntities`、`AddOrReplaceModelProperties`、`DeleteModelEntities`、`DeleteModelProperties`。その他の未対応要求には`UNSUPPORTED_EVENT_TYPE`を返す。
- 接続中のIPCクライアントがある状態で選択ID集合が実際に変わると、Viewerは`ModelEntitySelected`を送信する。payloadの`entityIds`は変更後の全選択IDを昇順で含み、全解除時は空配列となる。同じ選択集合を再指定した場合は通知しない。接続開始時点の選択状態は別途通知しない。
- `ModelEntitySelected`は要求への応答ではないため`correlationId`はnull。Viewerは応答と通知を同一接続上の単一送信キューから直列に書き込む。IPCクライアントは`Result`の`correlationId`で要求応答を識別し、非同期通知と区別する。
- 要求への応答は`Result`イベントで返し、`correlationId`に要求の`eventId`を設定する。payloadは`ok`、`request`、`errorCode`、`message`を持つ。
- `ReplaceModelSet`のpayloadには`entities`と`properties`が必須。`entities`は1件以上、`properties`は空配列を許可する。両方を検証してから現在のモデル集合を置換する。
- Entity・Propertyの生成と階層検証は既存モデルの置換前に完了する。不正なpayloadには`INVALID_PAYLOAD`を返し、現在のモデル集合は変更しない。Entityの`position`は有限値3要素、`rotation`は有限値4要素かつゼロでないクォータニオンとする。DTOで省略した値は既定値を使うが、明示的な`null`や不正な配列は拒否する。
- Propertyの`iconPath`はツリー表示用アイコン（空ならなし）。`particleType`は`None`（既定）/`Point`/`Box`。`None`以外では所属Entityの描画Node配下に`ModelParticle`を生成し、`ModelProperty`が対応するパーティクルを所有する。`particleTexturePath`は粒子画像の`res://`パス（空なら`res://assets/icon/particle/star.png`、読み込めない場合も星画像で代替しWarnログを出す）。`particleColor`は`#RRGGBB`形式、空なら白（Point/Box共通。指定色には固定の発光倍率を掛ける）。`visibility`は`Inherit`（既定）/`Visible`/`Invisible`で、Inheritは親Property、所属Entityの順に継承し、実効表示でパーティクルの描画Layerを切り替える（Entityと同様にTreeのボタンで切替・Undo/Redo可能）。不正値は`INVALID_PAYLOAD`とする。パーティクルは所属Entityの描画NodeのAABB中心に置き、AABBをその対角線長の10%だけ全方向へ広げた範囲に収める（薄いモデルでも見えるようにするため）。シーンのロード完了時に再配置する。Entity置換でNodeが作り直された場合は、同期時に現在のNode配下へ再生成する。
- `AddOrReplaceModelEntities`は`entities` DTO配列、`AddOrReplaceModelProperties`は`properties` DTO配列を受け取る。同一IDが登録済みならDTOの値で置換する。各要求の配列全体を検証してから反映する。
- `DeleteModelEntities`と`DeleteModelProperties`は`ids` Guid配列を受け取る。Entity削除は子孫Entityと所属Propertyを再帰削除し、Property削除は子孫Propertyを再帰削除する。指定IDが未登録なら`TARGET_NOT_FOUND`を返し、その要求では変更しない。
- メインスレッド要求の待機上限は2秒。キュー待ち要求はタイムアウト時に取り消す。処理開始後は中断せず、結果不明として返すため、送信側は確認なしに再送しない。

## IPC Tester

`CoaXisIpcTester`は外部クライアントとして`ReplaceModelSet`を送信する。要求のResultを待つ間に受信した非同期通知は通信履歴に記録する。Result受信後は接続を閉じるため、継続的な通知購読には対応しない。入力形式と操作手順は[IPC Tester手順](../guides/viewer/IPC_TESTER_GUIDE.md)を参照。
