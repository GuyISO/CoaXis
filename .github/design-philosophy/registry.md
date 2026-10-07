# Design Philosophy Registry

設計思想に関する依頼の履歴。新しい方針は末尾へ追記する。

## Entry Template

- Date: YYYY-MM-DD
- Trigger: 依頼の要約
- Decision: 採用した方針
- Scope: 適用範囲
- Artifacts Updated:
  - path/to/file
- Notes: 補足

---

- Date: 2026-07-15
- Trigger: Node継承クラスのExitTree末尾に base._ExitTree(); を統一
- Decision: `_ExitTree()` の末尾で `base._ExitTree();` を必須化し、購読解除を先に行う
- Scope: Godot C# の Node 継承クラス
- Artifacts Updated:
  - `.github/skills/godot-node-lifecycle-rules/SKILL.md`
- Notes: region順序は `Lifecycle -> Events -> Public API` を基準として運用

- Date: 2026-07-15
- Trigger: 設計思想依頼を継続蓄積できる体制を構築
- Decision: AGENTS + Instructions + Capture Skill + Registry/Index の4層運用にする
- Scope: プロジェクト全体
- Artifacts Updated:
  - `.github/AGENTS.md`
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/skills/capture-design-philosophy/SKILL.md`
  - `.github/design-philosophy/index.md`
- Notes: 以後、設計思想依頼ごとに registry への追記を必須化

- Date: 2026-07-16
- Trigger: Subscribe/Unsubscribe メソッドを Events region の最上部に置く運用へ変更
- Decision: `SubscribeEvents` / `UnsubscribeEvents`（同等メソッド含む）は `Events` region 先頭に配置する
- Scope: Godot C# の Node 継承クラス
- Artifacts Updated:
  - `.github/skills/godot-node-lifecycle-rules/SKILL.md`
  - `.github/design-philosophy/registry.md`
- Notes: `Lifecycle -> Events -> Public API` の region 順序は維持する

- Date: 2026-07-16
- Trigger: UiEvents系購読と EnsureChildNodes 系の参照確立メソッドも整列対象へ拡張
- Decision: `SubscribeUiEvents` / `UnsubscribeUiEvents` などの類似購読と、`EnsureChildNodes` などの参照確立メソッドも `Events` region 先頭に配置する
- Scope: Godot C# の Node 継承クラス
- Artifacts Updated:
  - `.github/skills/godot-node-lifecycle-rules/SKILL.md`
  - `.github/design-philosophy/registry.md`
- Notes: 従来の `SubscribeEvents` / `UnsubscribeEvents` 配置ルールを拡張した

- Date: 2026-07-16
- Trigger: Measurement の座標運用を「内部計算=Godot、外部境界=CATIA」へ統一
- Decision: 内部計算では Godot 座標系を維持し、UI/DB/IPC/ファイル出力の境界でのみ CATIA 変換と単位換算を行う
- Scope: 座標系を扱う C# 実装全般
- Artifacts Updated:
  - `.github/instructions/coordinate-system.instructions.md`
  - `.github/design-philosophy/index.md`

- Date: 2026-09-19
- Trigger: ModelEntityTree用・Viewport用に個別のPopupMenu(ModelEntityMenu/ViewportMenu)を作る方針から、操作対象オブジェクト単位でMenuを作る方針へ変更
- Decision: PopupMenuは表示元UI単位ではなく操作対象オブジェクト（ModelEntity等）単位で作成する。ModelEntityMenuをTreeItem専用から `ShowForEntity(Guid entityId, Vector2I screenPosition)` によるGuid(ModelEntity)ベースのAPIへ変更し、ModelEntityTreeとViewportInteractionHandlerの両方から共用する形にした。重複実装だったViewportMenuは廃止。あわせて、Measurementのコピーで作られ未使用のまま壊れていた `application/domain/menu`（MenuService/MenuEvent/MenuFacade）配下も削除した
- Scope: Godot C# の PopupMenu 系 UI コンポーネント全般
- Artifacts Updated:
  - `CoaXisViewer/src/ui/menu/ModelEntityMenu.cs`
  - `CoaXisViewer/src/ui/tree/ModelEntityTree.cs`
  - `CoaXisViewer/src/application/domain/viewport/ViewportInteractionHandler.cs`
  - `CoaXisViewer/scenes/viewport/viewport_container.tscn`
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/index.md`
- Notes: 今後、別対象（例: Viewport全体の背景クリックなど対象なしのケース）向けメニューを追加する場合も同じ「対象オブジェクト単位」の方針を踏襲すること

- Date: 2026-09-19
- Trigger: コンテキストメニュー（PopupMenu）を呼び出し元Node（ModelEntityTree/ViewportInteractionHandler）の子として.tscnに直接配置する方針を廃止し、Application.Ui.Menu経由で呼び出せるように変更
- Decision: `MenuFacade`/`MenuService` を新設し、MenuService が ModelEntityMenu の実体をコード上で保持・生成する（.tscnにPopupMenuノードを並べない）。呼び出し側は `Application.Ui.Menu.ShowModelEntityMenu(Guid entityId, Vector2I screenPosition)` を呼ぶだけでよく、GetNodeでの参照保持や各UIでのメニューノード配置は不要になった
- Scope: Godot C# の PopupMenu 系 UI コンポーネント全般
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/menu/MenuFacade.cs`
  - `CoaXisViewer/src/application/domain/menu/MenuService.cs`
  - `CoaXisViewer/src/application/Application.cs`
  - `CoaXisViewer/src/ui/tree/ModelEntityTree.cs`
  - `CoaXisViewer/src/application/domain/viewport/ViewportInteractionHandler.cs`
  - `CoaXisViewer/scenes/viewer/canvas.tscn`
  - `CoaXisViewer/scenes/viewport/viewport_container.tscn`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: 以前のMeasurementコピー由来で壊れていた旧 `application/domain/menu` 配下は今回作り直して再利用
  - `.github/design-philosophy/registry.md`
- Notes: 境界変換は CoordinateSystemUtility を必須とする

- Date: 2026-07-20
- Trigger: Godot の signal で運ぶ payload 型をどう扱うかを統一
- Decision: Signal 引数に渡す自前の参照型は `RefCounted` を継承し、`GodotObject` の直継承は避ける
- Scope: signal payload として設計する C# クラス全般
- Artifacts Updated:
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/registry.md`
- Notes: `MeasurementResult` と `PickResult` を対象に適用

- Date: 2026-07-25
- Trigger: 設定値を定数化した後の参照方針と即時反映時の描画更新ルールを統一
- Decision: `Constant` 由来値はキャッシュしない。`SettingService` 由来値は高頻度参照時のみキャッシュする。`SettingsNotified` 購読時は値更新に加えて再描画/再構築を同時に実施する
- Scope: 設定値を参照する Godot C# コンポーネント全般
- Artifacts Updated:
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/registry.md`
- Notes: 判定基準は「参照頻度」と「再描画が必要な見た目更新の有無」で決定する

- Date: 2026-08-02
- Trigger: ModelNode 直結ロジックから Guid 中心ロジックへの移行運用を恒久ルール化
- Decision: モデル関連の Signal/Event/Pick/UI payload は ModelId を運び、ModelNode は軽量ビューとして扱う。Godot Signal 層は string modelId、受信直後に Guid.TryParse を必須化する
- Scope: Model, Selection, Pick, UI ツリー連携を含む C# 実装全般
- Artifacts Updated:
  - `docs/guides/viewer/MODEL_ID_OPERATION_GUIDE.md`
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/index.md`
  - `.github/design-philosophy/registry.md`
- Notes: Guid 変換失敗 payload は処理せず警告ログを残す

- Date: 2026-09-24
- Trigger: Model関連Domainのロード・表示更新・ツリー管理の責務を段階的に分割
- Decision: モデル集合の置換・取消しはModelLoadService、論理Entity/Property階層はModelRegistry、非同期Sceneロードと状態遷移はModelSceneService、ModelNodeへの表示反映はModelPresentationService、TreeItem投影はUIが所有する。各責務は他者の内部状態を保持しない
- Scope: CoaXisViewerのModel Domain、モデルロード、Scene反映、モデルTree UI
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/ModelLoadService.cs`
  - `CoaXisViewer/src/application/domain/model/ModelEntityMapper.cs`
  - `CoaXisViewer/src/application/domain/model/ModelSceneService.cs`
  - `CoaXisViewer/src/application/domain/model/ModelRegistry.cs`
  - `CoaXisViewer/src/model/factory/ModelEntityFactory.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: ModelEntityとModelNodeの結合は互換性のため維持し、完全なGodot非依存化は別フェーズとする

- Date: 2026-09-24
- Trigger: ModelPresentationServiceを表示反映専任へ分離
- Decision: 表示状態切替要求とツリー折畳み状態の更新をModelStateServiceへ移し、ModelPresentationServiceはModelNodeへの位置・回転・Visibility・選択強調・透明度反映だけを担う。ModelPresentationServiceからUI Tree参照を禁止し、ModelPropertyTreeはPickEventを直接購読して更新する
- Scope: CoaXisViewerのModel Domainとモデル/プロパティTree UI
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/ModelStateService.cs`
  - `CoaXisViewer/src/application/domain/model/ModelPresentationService.cs`
  - `CoaXisViewer/src/ui/tree/EmbededModelPropertyTree.cs`
  - `CoaXisViewer/src/ui/tree/ModelEntityTree.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: Treeの選択は既存のPickEvent経路でPropertyTreeへ到達するため、Tree間の直接参照を追加しない

- Date: 2026-09-26
- Trigger: Model中心Domain内の責務増加を受け、Modelと操作能力のFacade境界を整理
- Decision: `application/domain`をModel中心領域とし、Model中核・ModelLoad・ModelState・ModelPresentation・Selection・Measurementを兄弟能力として構成する。ModelFacadeはModelEvent/ModelRegistryを所有し、ロード・状態・表示は能力別Facadeから公開する。Selection/MeasurementはModelを利用する操作能力としてModel配下へ移さず、Model中核からの逆依存を禁止する
- Scope: CoaXisViewerのApplication Domain Facade構成とモデル操作API
- Artifacts Updated:
  - `CoaXisViewer/src/application/Application.cs`
  - `CoaXisViewer/src/application/domain/model/ModelFacade.cs`
  - `CoaXisViewer/src/application/domain/model-load/ModelLoadFacade.cs`
  - `CoaXisViewer/src/application/domain/model-state/ModelStateFacade.cs`
  - `CoaXisViewer/src/application/domain/model-presentation/ModelPresentationFacade.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `docs/specification/specification_integrated.md`
- Notes: ModelLoadServiceとModelSceneServiceはロード世代・キャンセル処理が連動するため同一Facadeにまとめる。ModelEventの通知契約と各Serviceの状態所有者・実行順は維持する

- Date: 2026-09-29
- Trigger: ProtocolとViewer間のMapperおよびpayload組み立て責務を整理
- Decision: Protocolはwire契約DTOとpayload/envelopeの組み立てを担当し、DTOからViewerドメインモデルへの変換はViewerに置く。ProtocolのBuilderは契約データのみを扱い、Viewer固有の型や振る舞いに依存させない
- Scope: CoaXisProtocol.ViewerとCoaXisViewer間のIPC境界
- Artifacts Updated:
  - `CoaXisProtocol/Viewer/src/payload/ModelSetPayload.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: PayloadBuilderはDTO配列をコピーしてModelSetPayloadを生成し、ModelEntityMapper等の変換処理は持たない

- Date: 2026-09-29
- Trigger: Payload専用Builderを増やさず、生成責務をpayloadへ集約する方針に変更
- Decision: 単一payloadの単純な生成はpayloadクラス内のstatic factoryに置く。複数payloadにまたがる複雑な組み立てがない限り、payload専用Builderクラスを作らない。DTOからViewerドメインモデルへのMapperはViewerに置く
- Scope: CoaXisProtocol.Viewerのpayload定義と生成処理
- Artifacts Updated:
  - `CoaXisProtocol/Viewer/src/payload/ModelSetPayload.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: `ModelSetPayloadBuilder` を廃止し、`ModelSetPayload.Create` に生成処理を統合

- Date: 2026-10-03
- Trigger: ViewportHubをイベント仲介だけでなくカメラ状態の唯一の所有者にする
- Decision: カメラの注視点位置・回転、距離、正投影サイズ、FOV、投影方式はViewportCameraHubが保持する。CameraRigとUIはHub状態を初期値として受け取り、変更要求をHubへ送り、Hubの変更通知に追従する。シーン上のNode/Camera値を正本としてHubへ取り込む運用は禁止する
- Scope: CoaXisViewerのViewportHub、CameraRig、Viewport関連Node/UI
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/ViewportCameraHub.cs`
  - `CoaXisViewer/src/component/scene/CameraRig.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `docs/rules/technical-knowledge-base.md`
- Notes: CameraRigはGodotシーンへの状態反映に限定し、CameraHubが正本値を保持してから購読者へ通知する

- Date: 2026-10-03
- Trigger: CameraRigからカメラ操作の計算責務を分離
- Decision: カメラ状態・操作要求・通知・TweenはViewportHub配下のViewportCameraHubが担当し、数値計算はNode継承やイベント購読を行わないstaticなViewportCameraUtilityへ置く。CameraRigはCameraHubの状態通知をGodot Sceneへ反映する
- Scope: CoaXisViewerのViewportHub、ViewportCameraHub、ViewportCameraUtility、CameraRig
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/ViewportHub.cs`
  - `CoaXisViewer/src/application/domain/ViewportCameraHub.cs`
  - `CoaXisViewer/src/application/domain/ViewportCameraUtility.cs`
  - `CoaXisViewer/src/component/scene/CameraRig.cs`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: Fit計算はフィット対象Nodeの所属Viewportを使い、SubViewportの表示比率を保つ。ViewportHubはCameraHubとLayer/操作モード等のViewport共通状態をまとめる

- Date: 2026-10-03
- Trigger: Viewport状態同期のAskState要求を廃止
- Decision: Viewportの状態購読者はイベント購読後の初期化処理で各状態HubのPropertyを読み、以降は状態変更通知に追従する。Hubの状態を再通知させるAskState要求を設けない。操作モード・アークボール・矩形選択状態はViewportInteractionHubに、Layer表示と将来の表示制御はViewportDisplayHubに所有させる
- Scope: CoaXisViewerのViewport関連Node/UIとViewportHub
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/ViewportHub.cs`
  - `CoaXisViewer/src/application/domain/ViewportCameraHub.cs`
  - `CoaXisViewer/src/application/domain/ViewportInteractionHub.cs`
  - `CoaXisViewer/src/component/ViewportInteractionHandler.cs`
  - `CoaXisViewer/src/component/ViewportOverlay.cs`
  - `CoaXisViewer/src/component/scene/CameraRig.cs`
  - `CoaXisViewer/src/component/scene/AxisNavigator.cs`
  - `CoaXisViewer/src/ui/panel/ViewportUi.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `docs/rules/technical-knowledge-base.md`
- Notes: 初期表示に必要なLayer状態はViewportDisplayHub、操作モード・アークボール・矩形選択状態はViewportInteractionHub、カメラ状態はViewportCameraHubが保持する。DisplayHubは表示モードや表示対象の絞り込みなど、表示/可視性制御の拡張先とする

- Date: 2026-10-05
- Trigger: 副作用のないカメラ計算もViewportCameraHubへ統合
- Decision: カメラ状態・操作要求・通知・Tweenと、それらに必要なカメラ計算はViewportCameraHubに置く。計算処理はprivate helperとし、独立したViewportCameraUtilityを設けない。CameraRigはHub状態をSceneへ反映する
- Scope: CoaXisViewerのViewportCameraHubとCameraRig
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/viewport/ViewportCameraHub.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `docs/rules/technical-knowledge-base.md`
- Notes: カメラ計算をHubから独立した公開面として扱う必要がないため、状態操作と同じ責務境界に集約する。計算式と既存の操作結果は維持する

- Date: 2026-10-04
- Trigger: ModelEntityHubとModelPropertyHubの分離に合わせてRegistry責務を分離
- Decision: Entity集合・Entity階層は`ModelEntityRegistryHub`、Property集合・Property階層は`ModelPropertyRegistryHub`がそれぞれ所有する。両Registryにまたがる階層解決とクリアの順序はModelLoadが調整し、統合Registryを再導入しない
- Scope: CoaXisViewerのModel DomainとModelの利用側
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityRegistryHub.cs`
  - `CoaXisViewer/src/application/domain/model/property/ModelPropertyRegistryHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityLoadHub.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: Propertyの所属Entity解決にはEntity Registryを参照し、集合の横断操作はModelLoad側に限定する

- Date: 2026-10-04
- Trigger: Model.VisualとModel.StateをModel.Entity配下へ移動
- Decision: ModelEntityに関する状態管理・表示反映は `ModelEntityStateHub` と `ModelEntityVisualHub` が担当し、両Hubを `ModelEntityHub` の子として構成する。Hubの親子関係は機能の所属を示し、State/Visualの責務分離と依存境界は維持する
- Scope: CoaXisViewerのModel Hub構成とその利用側
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/ModelHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityStateHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityVisualHub.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: 呼び出し側は `Application.Model.Entity.State` と `Application.Model.Entity.Visual` を使用する

- Date: 2026-10-04
- Trigger: PickとMeasurementの対象がModelEntityであることを踏まえたApplication Domain配置の整理
- Decision: PickHubとMeasurementHubをModelEntityHub配下へ集約し、型名を `ModelEntityPickHub` / `ModelEntityMeasurementHub` に統一する。公開経路は `Application.Model.Entity.Pick` / `Application.Model.Entity.Measurement` とする
- Scope: CoaXisViewerのModelEntity関連Domain機能
- Artifacts Updated:
  - `CoaXisViewer/src/application/Application.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityPickHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityMeasurementHub.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `docs/specification/specification_integrated.md`
- Notes: 既存の操作イベント契約・実行責務は維持し、Hubの構成上の所属と公開経路のみ変更する

- Date: 2026-10-04
- Trigger: ModelのEntity/Propertyロード機能とEntity Sceneロード機能の所属・命名を明確化
- Decision: Entity置換ロードは `ModelEntityHub.Load`、Propertyロードは `ModelPropertyHub.Load` として各対象Hub配下に配置する。SceneロードHubはEntity機能の一部として `ModelEntityHub.Scene` に置き、型名を `ModelEntitySceneHub` とする。ロード責務の親Hub `ModelLoadHub` は廃止する
- Scope: CoaXisViewerのModel Domainロード機能
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/ModelHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityLoadHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntitySceneHub.cs`
  - `CoaXisViewer/src/application/domain/model/property/ModelPropertyHub.cs`
  - `CoaXisViewer/src/application/domain/model/property/ModelPropertyLoadHub.cs`
  - `CoaXisViewer/src/core/ipc/IpcCommandDispatcher.cs`
  - `docs/specification/specification_integrated.md`
- Notes: Entity/Property Registryをまたぐ置換時の階層解決・クリア順序は引き続きModelEntityLoadHubが調整し、SceneロードはModelEntitySceneHubへ委譲する

- Date: 2026-10-04
- Trigger: CommandHubの初期同期にAskState再要求を使わず、要求側が保持状態を読む
- Decision: 状態Hubの購読者は通知購読後にHubの現在状態を直接読み、以降は状態変更通知に追従する。状態再要求Signalによる通知再送を設けない
- Scope: 状態Hubとその状態を表示・利用するNode/UI
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/CommandHub.cs`
  - `CoaXisViewer/src/ui/panel/CommandUi.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/index.md`
- Notes: CommandUiは購読直後にHistory/Cursorを取得し、以後はHistoryNotifiedで更新する

- Date: 2026-10-04
- Trigger: CommandUiがCommandHub所有の履歴とカーソルを重複保持しないようにする
- Decision: 状態Hubを表示するUIは、履歴やカーソルなどHubが所有する正本状態のコピーをフィールドに保持しない。UI更新時にHubのPropertyを読み、変更通知後に再描画する
- Scope: CoaXisViewerのCommandUiおよびHub状態を表示するNode/UI
- Artifacts Updated:
  - `CoaXisViewer/src/ui/panel/CommandUi.cs`
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/index.md`
  - `.github/design-philosophy/registry.md`
- Notes: CommandUiはExecuted通知で再構築をキューし、再構築時にCommandHub.History/Cursorを参照する

- Date: 2026-10-04
- Trigger: Application層とHubの基本アーキテクチャを「Hubが値を保持・公開メソッドで要求を受ける・更新通知のみ発行・購読者がHubのPropertyを直接読む」に統一する
- Decision: Hubの変更通知Signalは値を運ばず「更新があったこと」だけを通知する（対象識別子entityIdのみ許容）。UI等からの要求はHubの公開メソッドを直接呼び、要求用Signal（XxxRequested/AskXxx）を設けない。購読者は通知受信時にHubのPropertyを読む。一過性のイベント（ピック結果など保持状態を持たないもの）は例外としてpayloadを運んでよい
- Scope: CoaXisViewerのApplication層Hub（Viewport系・Pick・Selection・Measurement を対応済み。ModelEntityState/Visual/Registry/Tree/Load/Scene、Property系、Ipc/Log/Setting は Model リファクタ時に対応）
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/viewport/*Hub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityPickHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntitySelectionHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityMeasurementHub.cs`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: Display の LayerActivated(layer,isActive) は ActiveLayersNotified() に、Selection の Mode は引数なし通知にした。個々の ModelEntity を対象とする通知（Selected/Visibility/Status/Collapsed/Position/Rotation/Added）は entityId のみを payload とし、値は Registry.Get や Selection.Contains 経由で参照する。折り畳みは State.SetCollapsed(Guid,bool) の直接呼び出しに統一し、ToggleModelVisibilityRequested 要求Signalは廃止した

- Date: 2026-10-07
- Trigger: ModelEntityの状態Field更新、対応するModelNodeへの反映、および更新通知の責務を明確にする
- Decision: ModelEntityのField更新は呼び出し側から直接代入せず、専用メソッドまたはアクセサー経由で値を渡す。ModelEntity自身がFieldを更新し、対応するModelNodeの状態（Position等）を同期した後、ModelEntityStateHubを通じて対象entityIdのみの変更通知を発行する。ModelEntityStateHubは値を変更せず通知のみを担い、ModelEntityVisualHubは同じField更新・Node同期を重複して実装しない
- Scope: CoaXisViewerのModelEntity状態Fieldと対応するModelNode
- Artifacts Updated:
  - `.github/instructions/design-philosophy.instructions.md`
  - `.github/design-philosophy/index.md`
  - `.github/design-philosophy/registry.md`
- Notes: 選択強調・透明度など、ModelEntityの状態Field更新を伴わない表示専用効果のNode反映は引き続きModelEntityVisualHubが担当する

- Date: 2026-10-07
- Trigger: ModelEntity.Statusを許可されたライフサイクル順序に制限し、Visibility設定と実際の表示結果を区別する
- Decision: Statusは読み取り専用にし、Unloaded→Initialized→Registered→Loading→LoadedまたはLoadFailed→Disposedの遷移を専用メソッドで制御する。ScenePathが空の場合はRegistered→Loaded、登録済み状態からの破棄遷移も許可し、それ以外は例外で拒否する。VisibilityはInheritを含む設定値、IsVisibleは設定値と親階層から導出する読み取り専用値とし、解決値をキャッシュしない。Visibility設定変更時はVisualHubが対象と子孫を再解決し、Layer反映後に解決完了通知を発行する
- Scope: CoaXisViewerのModelEntity状態管理、Visibility解決、Tree表示
- Artifacts Updated:
  - `CoaXisViewer/src/model/entity/ModelEntity.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityVisualHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntityRegistryHub.cs`
  - `CoaXisViewer/src/application/domain/model/entity/ModelEntitySceneHub.cs`
  - `CoaXisViewer/src/ui/tree/ModelEntityTree.cs`
  - `docs/specification/specification_integrated.md`
  - `.github/instructions/design-philosophy.instructions.md`
- Notes: Sceneロードの失敗・破棄中を含め、無効なStatus遷移を成功扱いで無視しない。VisualHubのVisibilityResolved(entityId)を購読するUIは、設定値と導出値をModelEntityから別々に参照する。コマンドは設定値だけを変更し、子孫の影響範囲解決を重複して実装しない

## 2026-10-04 Hubのregion構成統一
- Policy: Application配下の全Hubを Fields/Properties/Signals/Lifecycle/Events/Methods/Helpers の共通7regionに統一（Signalsを上位に配置、Lifecycle以外の全メンバーにXMLコメント必須）
- Notes: Notifications/Actions regionを廃止。Signal宣言と NotifyXxx は Signals、On*/Subscribe*/Unsubscribe* は Events へ移動（旧Helpers/Lifecycle内のものも含む）
