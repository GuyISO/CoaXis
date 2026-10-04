---
description: "Use when: 設計思想・実装方針・責務分割・命名規約に関する依頼を受け、再利用可能な規約として残す必要があるとき"
---

# Design Philosophy Instruction

## Objective

設計思想に関する依頼を、都度の回答で終わらせず、プロジェクト資産として蓄積する。

## Required Actions

1. 依頼内容を「方針1行」に要約する。
2. 方針の保存先を次で選ぶ。
   - 常時ルール: `.github/instructions/`
   - タスク手順: `.github/skills/`
   - 履歴: `.github/design-philosophy/registry.md`
3. `registry.md` に日付付きエントリを追記する。
4. 必要なら `index.md` のカテゴリ対応表を更新する。

## Quality Gate

- 曖昧な抽象論で終わらず、判定可能なルール文にする。
- ルールは「適用条件」と「禁止/必須」を明記する。
- 既存規約と衝突する場合は、優先順位または適用範囲を記載する。

## Current Rules

- Godot の Signal 引数に渡す自前の参照型は `RefCounted` を継承する。`GodotObject` の直継承は避け、signal payload として安全に受け渡せる型に限定する。
- 設定値参照の方針: `Constant` 由来の値はキャッシュしない。`SettingService` 由来の値は「高頻度参照（毎フレーム/入力処理/大量ループ）」の場合のみキャッシュし、低頻度参照は都度取得する。
- 即時反映の方針: `SettingsNotified` を購読するコンポーネントは、値更新だけで終わらせず、必要な再描画/再構築（例: `QueueRedraw`, `Rebuild`, `Draw*` 再実行）を同一ハンドラ内で必須実施する。
- 状態所有と初期同期の方針: 状態Hubの購読者は通知購読後にHubの現在状態を直接読み、以降は状態変更通知に追従する。Node/UIはHubが所有する正本状態を重複して保持せず、状態再要求Signalで通知を再送させない。
- モデル識別の方針: Model 系の Signal/Event/Pick/UI payload は ModelNode 参照を直接運ばず ModelId を運ぶ。Godot Signal 層では `string modelId` を使い、受信直後に `Guid.TryParse` でドメイン層の Guid へ変換する。
- コンテキストメニューの方針: PopupMenu は「表示元 UI（Tree/Viewport等）」単位ではなく「操作対象オブジェクト（例: ModelEntity）」単位で作成する。呼び出し側（Tree/Viewport等）は TreeItem や PickResult から対象の識別子（Guid）を解決してから `ShowForEntity(Guid, Vector2I)` 相当の対象単位APIを呼び出す。同じ対象を扱うUIコンポーネント間でメニュー実装を重複させない。
- メニューの保持場所の方針: PopupMenu は呼び出し元 Node（ModelEntityTree や ViewportInteractionHandler 等）の子としてシーンに直接配置しない。`Application.Ui.Menu` がメニュー実体を保持・生成し、呼び出し側は `Application.Ui.Menu.ShowXxxMenu(...)` を呼ぶだけにする。新規メニューを追加する場合も MenuService に `ShowXxxMenu` メソッドを追加し、.tscn に PopupMenu ノードを直接並べない。
- Model中心Domainの構成方針: `ModelHub` は `ModelEntityHub` と `ModelPropertyHub` を構成する。ModelEntityに関するRegistry/State/Visual/Selection/Tree/Pick/Measurement/Load/Sceneは `ModelEntityHub` 配下へ置き、PropertyのRegistry/Loadは `ModelPropertyHub` に置く。Pick/MeasurementはModelEntityを対象とするため、公開経路も `Application.Model.Entity.Pick` / `Application.Model.Entity.Measurement` とし、`Application` 直下には配置しない。Entity置換ロードは `Application.Model.Entity.Load`、Propertyロードは `Application.Model.Property.Load`、Entity Sceneロードは `Application.Model.Entity.Scene` から公開する。Entityに属するHub型名・ファイル名には `ModelEntity` を、Propertyに属するHub型名・ファイル名には `ModelProperty` を付け、所属対象を明示する。親Hubへの集約は構成上の所属を表し、子Hub間の責務境界や依存方向を曖昧にしない。ModelEntity/Propertyの識別子集合と論理階層はそれぞれのRegistryが個別に所有し、両方をまたぐ階層解決・集合クリアの順序はModelEntityLoadHubが調整する。状態変更要求はModelEntityStateHub、ModelNodeへの位置・回転・Visibility・選択強調・透明度反映はModelEntityVisualHub、TreeItem/PropertyTree投影はUIがそれぞれ唯一の所有者となる。Entity内の各操作Hub間は必要なイベント連携を許容するが、Model中核のRegistry/State/VisualからPick/Measurement/Load/Sceneへの不要な依存や循環依存を禁止する。RegistryからSceneロードを操作したり、VisualHubが状態変更・UI参照を保持したり、UIがModel Domainの保持状態になることを禁止する。
- IPCのレイヤ境界: Protocolはwire契約DTOとpayload/envelopeの組み立てを担当し、DTOからViewerドメインモデルへの変換（Mapper）はViewerに置く。単純なpayload生成はpayloadクラス内のstatic factoryに置き、複数payloadにまたがる複雑な組み立てが必要な場合を除き、payload専用Builderクラスを増やさない。Protocolの生成処理はViewer固有の型や振る舞いに依存させない。
- Viewport状態の所有方針: カメラの注視点位置・回転、距離、正投影サイズ、FOV、投影方式の正本・要求・通知は`ViewportCameraHub`、操作モード・アークボール・矩形選択状態は`ViewportInteractionHub`、Layer表示状態は`ViewportDisplayHub`がそれぞれ保持する。`ViewportHub`は各状態Hubの構成のみを担当する。`ViewportDisplayHub`はLayer切替を現在の責務とし、将来の表示・可視性制御（表示モード、表示対象の絞り込みなど）もこの責務の範囲で扱う。カメラ操作計算はイベント・状態を持たないstaticな`ViewportCameraUtility`へ分離し、`CameraRig`はCameraHubの状態通知をSceneへ反映する役に限定する。状態購読者は購読開始後に該当HubのPropertyを読む初期化処理を行い、状態再要求イベント（AskState）による初期同期を設けない。Node/UIがHubと重複する正本状態を持つこと、また初期状態をシーン上のCamera/NodeからHubへ逆流させることを禁止する。

- Hub通知の方針: Application層のHubは値を保持し、UI等は公開メソッドを直接呼んで要求する（要求用SignalやAskStateは設けない）。Hubは必要に応じてUtilityを併用して値を更新し、更新があったことだけをSignalで通知する（payloadに値を載せない）。個々のModelEntityなど対象を明示する必要がある通知は、対象のentityIdのみをpayloadとし（isSelected/position/visibility/status等の値は運ばない）、購読者はRegistry（GetEntity）やSelection.Containsで対象の値を参照する。購読者は通知受信時にHubのPropertyを直接読む。保持状態を持たない一過性イベント（ピック結果等）のみpayload付きを許容する。
