# Design Philosophy Index

## Purpose

設計思想の依頼を、再利用しやすい形で保存先に振り分けるための目次。

## Category to Artifact Mapping

| Category | Store In | Notes |
|---|---|---|
| ライフサイクル規約 | `.github/skills/godot-node-lifecycle-rules/SKILL.md` | Godot Node の Ready/ExitTree など |
| 実装共通規約 | `.github/instructions/design-philosophy.instructions.md` | 全体的に効かせたい判断基準 |
| 意思決定履歴 | `.github/design-philosophy/registry.md` | 日付付きで履歴を蓄積 |
| 追加ワークフロー | `.github/skills/capture-design-philosophy/SKILL.md` | 依頼を資産化する手順 |

## Update Rule

- 新カテゴリを追加した場合は、この表へ1行追加する。
- 既存カテゴリの保存先が変わった場合は、同日に `registry.md` に理由を記録する。
