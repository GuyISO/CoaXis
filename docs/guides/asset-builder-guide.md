# Asset Builder Guide（統合済み）

この文書は、詳細な設計説明を Builder の正式 README に集約するため、簡潔な参照ページに整理した。Viewer 固有ではないため、`docs/guides/` 直下に配置する。

## 正本

- Builder README: [../../CoaXisSceneBuilder/README.md](../../CoaXisSceneBuilder/README.md)
- 統合仕様書: [../specification/specification_integrated.md](../specification/specification_integrated.md)
- ルート README: [../../README.md](../../README.md)

## 重要な方針

- 3D アセット構築責務は Viewer ではなく Builder に集約する。
- GLB / LineSet の前処理を行い、最終的に `.scn` を生成する。
- Viewer は事前生成済みアセットの読み込みと描画に集中する。

## 使い分け

- 役割とプロジェクトの一覧: ルート README
- Builder の設計と実行ガイド: Builder README
- 仕様と契約の詳細: 統合仕様書

重複した説明はここでは残さず、正本へ統一している。
