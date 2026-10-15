# ModelNode シーン構成メモ

## 目的

Entity 数増加時のノード数を抑えるため、`ModelComponents` 階層（Components/Pivot/Content）を廃止した。
ロードしたシーンは `ModelNode` 直下へ直接追加する（Entity あたりの追加ノード 0 個）。

## 基本方針

- `SceneAssetLoader` は `ModelNode.AttachScene()` でインスタンスを直下へ追加し、`ModelNode.SceneRoot` が参照する。
- Pivot は `Emphasize` / `SpinAppeal` の初回実行時のみ遅延生成し、`SceneRoot` だけを配下へ付け替える（子 ModelNode は巻き込まない）。
- `AlignToAabbCenter` が true の Entity は、Pivot 生成時に AABB 中心補正を行う（ロード時には行わない）。

## 重要ルール

1. シーンのノード構成を固定パスで参照しない（`SceneRoot` 経由）。
2. Pick / Selection の解決は `ModelNode` を遡る設計とする。
3. Effect 用ノードは廃止（未使用だったため）。必要になった時点で再導入する。

詳細な実装上の注意点は技術ナレッジと実装規約に集約する。