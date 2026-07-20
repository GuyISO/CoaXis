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
