---
name: capture-design-philosophy
description: "Use when: ユーザーの設計思想に関する依頼を、instructions/skills/registry に分解して継続運用できる形へ落とし込むとき"
---

# Capture Design Philosophy

設計思想の依頼を、再利用可能な規約資産へ変換するための手順。

## Inputs

- ユーザー依頼（例: 責務分離、命名規約、レイヤ境界、ライフサイクル実装方針）

## Procedure

1. 依頼を1行で要約し、判定可能なルールに変換する。
2. 保存先を決める。
   - 横断ルール: `.github/instructions/*.instructions.md`
   - 実装手順: `.github/skills/<topic>/SKILL.md`
3. `.github/design-philosophy/registry.md` に履歴を追記する。
4. 新カテゴリなら `.github/design-philosophy/index.md` へマッピングを追加する。

## Output Contract

- どのファイルを更新したかを明示する。
- 追加ルールは適用条件を含む短文で記載する。
- 重複ルールを増やさず、既存ルールへ統合する。
