# CoaXis Viewer・Protocol仕様

最終更新: 2026-10-18

この文書は、本リポジトリで管理するCoaXisViewer・CoaXisProtocol・CoaXisIpcTesterの現行仕様を記す。その他のCoaXisアプリケーションとSceneBuilderは対象外。Viewerが使うモデルシーンは外部で生成する。

## Viewer

### 起動と構成

Godot AutoLoadは`Application`のみ。Infrastructure（Log、Setting、Asset、Ipc、Input）とDomain（Command、Model、Ui、Viewport）の各Hubを構成する。モデル機能は`Application.Model.Entity`と`Application.Model.Property`に分かれる。

### ModelEntity

ModelEntityとModelPropertyのアイコンは`IconId`で指定し、アイコン画像ファイルのパスはProtocolに含めない。`IconId`の省略または空文字はアイコンなしを表す。指定可能なIDは`ModelIconId`で定義し、現在は`Annotation`、`AnnotationSet`、`Body`、`Component`、`HybridBody`、`Instance`、`Line`、`Point`、`Product`、`Surface`、`Target`をサポートする。既定アイコンは設けず、ViewerがIDからアセットパスへ変換する。明示的な`null`、空白のみの文字列、未定義IDは`INVALID_PAYLOAD`とする。

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

EntityのStatusは専用遷移メソッドで変更する。通常遷移は`Unloaded→Initialized→Registered→Loading→Loaded|LoadFailed`、ScenePathが空なら`Registered→Loaded`、破棄は`Registered|Loading|Loaded|LoadFailed→Disposed`。`IsVisible`はVisibility設定と親階層から導出する。Replace/Addでは、各Entityの親が同時に存在するEntityまたはRootであり、親IDをたどってRootへ到達することを検証する。各Propertyの親IDは空でなく、同時に存在するEntityまたはPropertyを指し、その親階層がEntityへ到達することを検証する。Entity/Property間のID重複を含め、階層を解決できない入力は要求全体を拒否し、部分反映やRegistryへの孤児保持を行わない。

TreeItemはUIが構築・保持し、Model DomainはUI Nodeを保持しない。Pick・Measurement・Load・Scene機能はEntity配下、Property LoadはProperty配下に置く。

### Scene

Viewerは事前生成済みGodotシーン（`.scn` / `.tscn`）をロードする。SceneBuilderによる生成処理は本リポジトリで管理しない。

ロードしたシーンのルートは`ModelNode.SceneRoot`から扱う。`Emphasize`または`SpinAppeal`が必要な場合に限りPivotを生成し、SceneRootをその配下へ移してメッシュAABB中心をPivot原点に合わせる。見た目の位置を保つためPivot配下を逆方向へ補正し、`ModelNode`の位置は変更しない。固定NodePathには依存しない。

カメラFitはNodeの可視状態と、MeshInstance3DのLayer・`ViewportDisplayHub.ActiveLayers`の共通Layerを考慮する。対象メッシュがない場合はカメラ状態を変更しない。

## IPC

- Named PipeでUTF-8の1行1JSONメッセージを送受信する。
- `IpcHub`はNamed Pipeの接続・受信・送信、メインスレッドへの要求振り分け、応答エンベロープ生成を担う。ドメイン状態は所有せず、ModelEntity選択通知ではSelectionHubの変更通知を受けて現在のID集合をProtocol payloadへ変換する。
- Viewerは各要求のバージョンが空でないことと、メジャーバージョン一致を検査する。起動時のバージョン照会は行わない。
- Viewerが処理する要求は`ReplaceModelSet`、`ClearModelSet`、`AddModelSet`、`RemoveModelSet`、`ModifyModelSet`、`ReadModelSet`。その他の未対応要求には`UNSUPPORTED_EVENT_TYPE`を返す。
- 接続中のIPCクライアントがある状態で選択ID集合が実際に変わると、Viewerは`ModelEntitySelected`を送信する。payloadの`entityIds`は変更後の全選択IDを昇順で含み、全解除時は空配列となる。同じ選択集合を再指定した場合は通知しない。接続開始時点の選択状態は別途通知しない。
- `ModelEntitySelected`は要求への応答ではないため`correlationId`はnull。Viewerは応答と通知を同一接続上の単一送信キューから直列に書き込む。IPCクライアントは`Result`の`correlationId`で要求応答を識別し、非同期通知と区別する。
- 要求への応答は`Result`イベントで返し、`correlationId`に要求の`eventId`を設定する。payloadは共通して`ok`、`request`、`errorCode`、`message`を持つ。
- `ReplaceModelSet`のpayloadには`entities`と`properties`が必須。`entities`は1件以上、`properties`は空配列を許可する。全Entity/Propertyの親参照とRootへの到達を含め、両方を検証してから現在のモデル集合を置換する。
- Entity・Propertyの生成と階層検証は既存モデルの置換前に完了する。不正なpayloadには`INVALID_PAYLOAD`を返し、現在のモデル集合は変更しない。Entityの`position`は有限値3要素、`rotation`は有限値4要素かつゼロでないクォータニオンとする。DTOで省略した値は既定値を使うが、明示的な`null`や不正な配列は拒否する。
- Propertyの`iconId`はツリー表示用アイコンID（省略または空文字ならなし）。`particleEmitterType`は`None`（既定）/`Point`/`Box`/`Sphere`/`Surface`、`particleBehaviorType`は`None`（既定）/`Disperse`/`Sparkle`/`Emerge`。パーティクルを無効にする場合は両方を`None`にし、有効にする場合はいずれも`None`以外にする。既定の組み合わせは`Point + Disperse`、`Box + Sparkle`、`Surface + Sparkle`。SphereはBoxと同様にAABB中心へ配置し、AABBを包含する球から放出する。Disperseは粒子を発生源から外向きに広げながら縮小させる。EmergeはDisperseと同じく外向きに広がりながら縮小するが、初速と寿命内の最大移動距離をDisperseの25%に抑える。Surfaceは所属Entityの可視メッシュ（三角形のみ）の表面を面積比でサンプリングし、全Modelで固定512点を放出点にする。発生源が`None`以外の場合、所属Entityの描画Node配下に`ModelParticle`を生成し、`ModelProperty`が対応するパーティクルを所有する。`particleTextureId`は粒子画像のテクスチャID（`ModelParticleTextureId`で定義し、現在は`Star`、`Kira`をサポート。省略または空文字なら`Star`）で、画像パスはProtocolに含めずViewerの`AssetTextureHub`がIDからアセットへ変換する。明示的な`null`と未定義IDは`INVALID_PAYLOAD`、画像を読み込めない場合はテクスチャなしで描画しWarnログを出す。`particleColor`は`#RRGGBB`形式、空なら白（Point/Disperse・Emerge、Box/Sparkle、Sphere/Sparkle、Surface/Sparkle共通。指定色には固定の発光倍率を掛ける）。`visibility`は`Inherit`（既定）/`Visible`/`Invisible`で、Inheritは親Property、所属Entityの順に継承し、実効表示でパーティクルの描画Layerを切り替える（Entityと同様にTreeのボタンで切替・Undo/Redo可能）。不正値は`INVALID_PAYLOAD`とする。Point/Box/Sphereのパーティクルは所属Entityの描画NodeのAABB中心を基準に配置する。AABBをその対角線長の10%だけ全方向へ広げ、Sphereの放出半径は拡張AABBの外接球半径とする（薄いモデルでも見えるようにするため）。SurfaceはModelNodeローカル座標系の原点から点を放出し、各可視メッシュ頂点をModelNodeローカル座標へ変換して三角形を構成する。面積累積に基づいて三角形を選び、各三角形内を一様にサンプリングする。シーンのロード完了時に再配置する。Entity置換でNodeが作り直された場合は、同期時に現在のNode配下へ再生成する。
- `Disperse`の最大移動距離は余白を含むAABB半サイズの長さ（外接球半径）とし、寿命から初速を求める。`Emerge`の最大移動距離と初速はその25%とする。発生範囲がPoint以外の場合は、その移動距離を表示AABBにも加算する。
- `ClearModelSet`のpayloadは空オブジェクト（省略可）。ルートEntity以外のEntityと全Propertyを削除する。
- `AddModelSet`のpayloadは`ReplaceModelSet`と同じ`entities`・`properties`（ともに必須、空配列可、両方空は`INVALID_PAYLOAD`）。既存を残したまま追加する。EntityまたはPropertyのIDがそれぞれの既存集合と衝突する場合、Entity/Property間でIDが重複する場合、または追加後の全階層がRoot/Entityへ接続しない場合は`INVALID_PAYLOAD`を返し、要求全体を反映しない。両方を検証してから反映する。既存値の変更には`ModifyModelSet`を使用し、IDや階層など変更対象外の構造を変える場合は`ReplaceModelSet`を使用する。
- `RemoveModelSet`と`ReadModelSet`のpayloadは`entityIds`・`propertyIds`のGuid配列（ともに必須、空配列可、両方空は`INVALID_PAYLOAD`）。`RemoveModelSet`はEntityの子孫と所属Propertyも削除し、指定Propertyを先に削除する。未登録IDは`TARGET_NOT_FOUND`で、全IDを検証してから反映するため部分削除しない。ルートEntityは削除できない。
- `ModifyModelSet`のpayloadは`entities`・`properties`（ともに必須、空配列可、両方空は`INVALID_PAYLOAD`）。各要素は`id`が必須で、指定した項目だけを更新し省略した項目は変更しない。Entityは`position`・`rotation`（CATIA座標系、検証は`ReplaceModelSet`と同じ）・`visibility`・`isCollapsed`・`isPropertyTreeVisible`、Propertyは`value`・`visibility`・`particleColor`（空文字で既定色）を変更できる。`parentId`・`type`・`name`・`iconId`・`scenePath`などの構造項目は変更できない。ルートEntityは変更できない。全要素を検証してから反映する。
- `ReadModelSet`の成功`Result`は、共通Result項目に加えてpayload直下の`modelSet`に`entities`・`properties`のDTO（`ReplaceModelSet`と同形式、座標はCATIA座標系）を含む専用Result型で返す。ワイヤ上のJSON形式と共通Result処理は他の要求と同じで、`modelSet`だけが追加される。指定IDのみを返し、子孫や所属Propertyは含めない。他の要求の`Result`には`modelSet`を含めない。
- メインスレッド要求の待機上限は2秒。キュー待ち要求はタイムアウト時に取り消す。処理開始後は中断せず、結果不明として返すため、送信側は確認なしに再送しない。

## IPC Tester

`CoaXisIpcTester`は外部クライアントとして`ReplaceModelSet`を送信する。要求のResultを待つ間に受信した非同期通知は通信履歴に記録する。Result受信後は接続を閉じるため、継続的な通知購読には対応しない。入力形式と操作手順は[IPC Tester手順](../guides/viewer/IPC_TESTER_GUIDE.md)を参照。
