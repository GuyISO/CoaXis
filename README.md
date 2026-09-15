# CoaXis

CoaXis は、3D モデルの可視化と事前アセット生成を分離して運用するプロジェクトです。要件の変更時は、実装・仕様・運用ルールを同時に揃えることを前提にしています。

## 1. まず読むべき入口

- 実装の起点: [CoaXis.sln](CoaXis.sln)
- プロジェクト全体の説明: [docs/README.md](docs/README.md)
- 仕様の正本: [docs/specification/specification_integrated.md](docs/specification/specification_integrated.md)
- 実装規約: [docs/rules/implementation-conventions.md](docs/rules/implementation-conventions.md)
- 運用ポリシー: [docs/rules/documentation-sync-policy.md](docs/rules/documentation-sync-policy.md)

## 2. 主要プロジェクト

- CoaXisViewer
  - 3D 表示・操作・IPC 連携を担当する Godot プロジェクト
- CoaXisSceneBuilder
  - GLB / LineSet を処理して `.scn` を事前生成する前処理プロジェクト
- CoaXisProtocol
  - 共通契約と IPC で使う DTO / 定義を管理するプロジェクト

## 3. 開発時の基本手順

```powershell
# 全体ビルド
 dotnet build .\CoaXis.sln
```

## 4. 実務ルール

- 仕様・責務・運用手順に変更があれば、同一作業でドキュメントを更新する
- 3D アセット生成の責務は Viewer に持たせず、Builder に寄せる
- ドキュメントと実装がズレた場合は、先に文書を修正してから完了扱いとする

## 5. 参考ドキュメント

- [docs/README.md](docs/README.md)
- [docs/specification/specification_integrated.md](docs/specification/specification_integrated.md)
- [docs/guides/asset-builder-guide.md](docs/guides/asset-builder-guide.md)
- [CoaXisSceneBuilder/README.md](CoaXisSceneBuilder/README.md)
- [docs/rules/documentation-sync-policy.md](docs/rules/documentation-sync-policy.md)
