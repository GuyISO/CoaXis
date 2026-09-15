# EntityId 中心運用ガイド

## 目的

モデル本体とビューを分離し、ロジックの中心を `ModelEntity` / `ModelProperty` と `ModelRegistry` に統一する。

## 必須ルール

1. `ModelEntity` を正とし、`ModelNode` は再生成可能なビューとする。
2. Signal / Event / UI 通知 / Pick 結果は `ModelNode` ではなく `EntityId` を運ぶ。
3. `ModelNode` からの要求は、まず `EntityId` に変換し、`ModelRegistry` で `ModelEntity` に解決する。
4. `Guid.TryParse` に失敗した payload は破棄し、警告ログを残す。
5. `ModelEntity` から `ModelNode` を長期保持しない。

## 命名

- `ModelNode` → `modelNode`
- `ModelEntity` → `modelEntity`
- `ModelProperty` → `modelProperty`
- `Guid` 実体識別子 → `entityId`
- `Guid` 属性識別子 → `propertyId`

## ルーティング順序

```text
ModelNode / Collider -> EntityId -> ModelRegistry -> ModelEntity
```

## 禁止事項

- Signal の payload に `Guid` を直接定義する
- Signal / Event の payload に `ModelNode` を直接定義する
- ModelEntity から Node 参照を長期保持する

## 実装チェック

- 受信時に `Guid.TryParse` を行っているか
- 内部状態が `EntityId` 集合になっているか
- UI / Selection / Pick の結果が `EntityId` ベースか

詳細は実装規約と技術ナレッジに集約する。