# IPC Protocol Guide（統合済み）

この文書は、詳細契約の正式版を分離し、要点だけを残すための最小化版です。

## 正本

- 統合仕様書: [../../specification/specification_integrated.md](../../specification/specification_integrated.md)
- 技術ナレッジ: [../../rules/technical-knowledge-base.md](../../rules/technical-knowledge-base.md)

## 重要な原則

- IPC は `command` + `payload` の JSON 構造を基本とする。
- Viewer は受信コマンドを分岐し、失敗時は `Result` を返す。
- 未対応コマンド、無効 JSON、欠損キーは警告ログに残し、失敗を明示する。
- 外部境界は CATIA 座標系、内部計算は Godot 座標系で扱う。

## 代表コマンド

- `LoadModel`
- `Select` / `Highlight`
- `ClearHighlight`
- `Focus`
- `ApplyViewPreset`
- `ApplyCameraPreset`

## 必須チェック

- `command` が欠けていないか
- payload の必須キーが揃っているか
- エラー時に `Result` を返しているか
- 仕様書と実装でコマンド一覧が一致しているか

詳細なエラーコードや契約の列挙は統合仕様書へ集約する。