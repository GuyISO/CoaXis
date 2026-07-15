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
