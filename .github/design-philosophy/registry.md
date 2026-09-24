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
- Trigger: コンテキストメニュー（PopupMenu）を呼び出し元Node（ModelEntityTree/ViewportInteractionHandler）の子として.tscnに直接配置する方針を廃止し、Application.Menu.Service経由で呼び出せるように変更
- Decision: `MenuFacade`/`MenuService` を新設し、MenuService が ModelEntityMenu の実体をコード上で保持・生成する（.tscnにPopupMenuノードを並べない）。呼び出し側は `Application.Menu.Service.ShowModelEntityMenu(Guid entityId, Vector2I screenPosition)` を呼ぶだけでよく、GetNodeでの参照保持や各UIでのメニューノード配置は不要になった
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
- Decision: モデル集合の置換・取消しはModelLoadService、論理Entity/Property階層はModelRegistry、非同期Sceneロードと状態遷移はModelSceneLoader、ModelNodeへの表示反映はModelPresentationService、TreeItem投影はUIが所有する。各責務は他者の内部状態を保持しない
- Scope: CoaXisViewerのModel Domain、モデルロード、Scene反映、モデルTree UI
- Artifacts Updated:
  - `CoaXisViewer/src/application/domain/model/ModelLoadService.cs`
  - `CoaXisViewer/src/application/domain/model/ModelEntityMapper.cs`
  - `CoaXisViewer/src/application/domain/model/ModelSceneLoader.cs`
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
