# AutoLoad ガイド（統合済み）

この文書は、専用の詳細説明を残す代わりに、正式ルールを中央集約するために簡略化したものです。

## 正本

- 実装規約: [../../rules/implementation-conventions.md](../../rules/implementation-conventions.md)
- 技術ナレッジ: [../../rules/technical-knowledge-base.md](../../rules/technical-knowledge-base.md)

## 重要な原則

- AutoLoad の入口は Application とし、利用側は Facade 経由でアクセスする。
- Event / Service / Facade の責務を分離し、責務が混ざらないようにする。
- 生成・購読・破棄はライフサイクル順序を固定し、_ExitTree で購読解除と後始末を行う。
- 既存の Node 参照を直接広めず、必要なときだけ Facade または ModelEntity の entityId 経由で扱う。

## 実装時の最低確認

- Application が責務の入口になっているか
- AutoLoad の生成が責務ごとに分かれているか
- _ExitTree で購読解除とリソース解放をしているか
- 仕様書やルールと齟齬がないか

詳細は中央の規約書に統一して管理する。