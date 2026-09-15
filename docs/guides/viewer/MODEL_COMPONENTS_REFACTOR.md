# Model Components 分離メモ

## 目的

`ModelNode` は階層管理に集中し、Mesh / Collider / Effect といった内部構造は `ModelComponents` 側に閉じ込める。

## 基本方針

- `ModelNode` は親子関係とモデルの識別に専念する。
- `ModelComponents` は実体の生成と保持に専念する。
- `ModelNode` から直接 `Mesh` / `Collider` を参照しない。

## 重要ルール

1. `CreateComponents()` で派生コンポーネントを生成する。
2. `ModelComponents` は冪等な初期化を行う。
3. Pick / Selection の解決は `ModelNode` を遡る設計とする。
4. 既存互換が必要な場合だけ、一時的な補助探索を許容する。

## 代表的な責務分離

- `ModelNode`: 親子管理、状態の入口、ルーティング
- `ModelComponents`: 実体生成、子ノード組み立て、表示/衝突/効果の保持

詳細な実装上の注意点は技術ナレッジと実装規約に集約する。