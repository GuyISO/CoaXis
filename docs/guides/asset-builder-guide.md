# Asset Builder Guide（統合済み）

この文書は、外部で管理される Builder が生成したアセットを Viewer が利用する際の境界を示す参照ページである。SceneBuilder 自体は本プロジェクトの管理対象外とする。

## 正本

- 統合仕様書: [../specification/specification_integrated.md](../specification/specification_integrated.md)
- ルート README: [../../README.md](../../README.md)

## 重要な方針

- 3D アセット構築責務は Viewer ではなく、CoaXis の管理対象外である外部 Builder に委譲する。
- 外部 Builder が GLB / LineSet を前処理し、最終的に `.scn` を生成する。
- Viewer は事前生成済みアセットの読み込みと描画に集中する。

## 使い分け

- 役割と管理範囲: ルート README
- 仕様と契約の詳細: 統合仕様書

外部 Builder の設計・実装・実行手順は本プロジェクトでは管理しない。
