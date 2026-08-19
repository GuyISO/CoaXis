# Agent Operating Policy for Design Philosophy

このリポジトリでは、設計思想に関する依頼を一過性で終わらせず、必ず再利用可能な形で蓄積する。

## Always-On Workflow

1. ユーザーが設計思想・実装方針・命名方針・責務分割の依頼をしたら、まず方針を1行で要約する。
2. 依頼にコード変更が含まれる場合は、先に実装を完了する。
3. 実装後、方針を再利用資産へ反映する。

## Where to Persist

- 汎用ルールとして常時参照したいもの:
  - `.github/instructions/*.instructions.md`
- 特定タスクの手順として使うもの:
  - `.github/skills/<skill-name>/SKILL.md`
- 変更履歴・意思決定ログとして残すもの:
  - `.github/design-philosophy/registry.md`

## Mandatory Update Set per Request

設計思想に関する依頼を処理したときは、少なくとも次を更新する。

- `registry.md` に新規エントリを1件追加
- 必要に応じて `instructions` または `skills` のどちらかを更新
- `index.md` のカテゴリ対応表を最新化

## Decision Rule

- 1ファイル内の表記・スタイル限定: instruction
- 複数ファイル横断の実装手順: skill
- チーム合意や背景を残す: registry
