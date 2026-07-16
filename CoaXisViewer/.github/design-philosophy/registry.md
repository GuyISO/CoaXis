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
  - `.github/design-philosophy/registry.md`
- Notes: 境界変換は CoordinateSystemUtility を必須とする
