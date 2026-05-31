# 技術ナレッジベース

この文書は、属人化を防ぐために技術的な判断・制約・運用手順を記録する正本です。

## 記録ルール

1. 実装中に発生した重要判断は、同一作業で本ファイルへ追記する。
2. 口頭・チャット・個人メモのみで完結させない。
3. 各項目は次のテンプレートに従う。

## 運用追補ルール

### 1. ドキュメント同期

1. 仕様変更時は、関連する仕様書・運用手順・テスト観点を同時更新する。
2. 仕様と実装が矛盾する場合、矛盾を記録し解消方針を残す。

### 2. 失敗時のふるまい

1. 失敗時は隠蔽せず、原因・影響範囲・暫定対処・恒久対策を分けて記録する。
2. 同種の再発を防ぐため、学びをルールまたはチェックリストに反映する。

### 3. ルール自体の保守

1. ルールは運用で破綻したら改訂し、例外運用を常態化させない。
2. ルールは Must と Should を分け、違反時の扱いを明示する。

## 記録テンプレート

### [YYYY-MM-DD] タイトル

- 背景:
- 問題:
- 判断:
- 判断理由:
- 採用しなかった代替案:
- 影響範囲:
- 実装/運用手順:
- 検証方法:
- 関連ファイル/関連仕様:
- 備考:

---

### [2026-06-01] Viewer IPCメッセージをcommand/payloadエンベロープへ統一

- 背景: CoaXisViewer の IPC 実装が PoC 段階の文字列送信中心で、仕様書のコマンド契約（LoadModel, ApplyCameraPreset など）と追跡しづらかった。
- 問題: 受信側が自由文字列依存だと、Editor-Viewer間で互換性を維持しにくく、エラー時の切り分けが困難。
- 判断: Viewer 側は JSON の `command` と `payload` を持つエンベロープ形式を標準とし、GameManager でコマンド分配する。
- 判断理由: 仕様書 6.3 のコマンド一覧へ直接マッピングでき、未実装コマンドも `OnError` で明示通知できるため運用時の可観測性が上がる。
- 採用しなかった代替案: 文字列プレフィックス（例: `cmd:xxx|...`）は暫定対応が容易だが、拡張時に破綻しやすいため不採用。
- 影響範囲: CoaXisViewer の IPC 受信処理（GameManager）、送信ユーティリティ（IpcClient）、契約定義（src/global）。
- 実装/運用手順: Editor 側は `{"command":"ApplyCameraPreset","payload":{...}}` 形式で送信し、Viewer からの通知も同形式で受ける。
- 検証方法: `dotnet build .\\CoaXis.sln` 成功と、NamedPipe テストで `ApplyCameraPreset` / `Focus` の処理確認。
- 関連ファイル/関連仕様: docs/specification/specification_integrated.md（6.3 IPC連携）, CoaXisViewer/src/global/ViewerIpcProtocol.cs, CoaXisViewer/src/viewer/GameManager.cs
- 備考: 既存のプレーン文字列メッセージは互換ログとして受理せず無視する設計（移行期間のみ）。

---

### [2026-06-01] C#クラス内構成の標準順序とregion名を統一

- 背景: PoC段階でクラス内の記述順・region名が揺れており、探索性とレビュー効率が低下していた。
- 問題: 開発者ごとに `Event Handlers` / `Input Handling` / `State Management` など命名が分かれ、同種メソッドの所在を予測しづらい。
- 判断: クラス内順序を `Signals -> Fields -> Properties -> Lifecycle -> Public API -> Internal Helpers` に統一し、region名も同名へ限定する。
- 判断理由: Godot系/非Godot系を問わず共通運用でき、学習コストとレビューコストを下げられる。
- 採用しなかった代替案: クラスごと自由命名は柔軟性が高いが、長期保守で認知負荷が上がるため不採用。
- 影響範囲: `CoaXisViewer/src/**/*.cs` および `tests/**/*.cs` のクラス内構成。
- 実装/運用手順: 変更対象クラスに手を入れる際、優先度高でregion名と順序を本規約へ合わせる。
- 検証方法: `dotnet build .\\CoaXis.sln` 成功、および `#region` 名の検索で標準語彙のみを確認。
- 関連ファイル/関連仕様: `docs/rules/implementation-conventions.md`
- 備考: 機能変更と構造整理の同時実施はレビュー難度を上げるため、原則分離する。

---

### [2026-06-01] 文字エンコーディング事故の再発防止フローを標準化

- 背景: region 名の一括置換作業中に、PowerShell 上書きで UTF-8 コメントが破損し、改行崩壊と構文エラーが発生した。
- 問題: 文字化けがコメントだけでなくコード行にも混入し、`if` や `return` がコメントと同一行化してビルド失敗を招いた。
- 判断: 「予防ルール + 自動検知 + 事故時標準フロー」を docs とタスクへ反映し、再発時に同じ調査を繰り返さない運用へ移行する。
- 判断理由: 人手レビューのみでは見落としが発生しやすく、作業者依存を避けるには機械チェックの常設が必要。
- 採用しなかった代替案: 事故ごとに ad-hoc に修復する運用は初動が遅く、再発率が下がらないため不採用。
- 影響範囲: `CoaXisViewer/src/**/*.cs` のコメント整備、`.vscode/tasks.json`、`scripts/quality/check-mojibake.ps1`、`docs/rules`。
- 実装/運用手順: `check: mojibake` 実行後に `dotnet build .\\CoaXis.sln` を実施し、最終確認は `verify: build + encoding` を使用する。
- 検証方法: 文字化けパターン検索ゼロとビルド成功を確認する。
- 関連ファイル/関連仕様: `docs/rules/encoding-incident-prevention.md`, `scripts/quality/check-mojibake.ps1`, `.vscode/tasks.json`
- 備考: 検知漏れが見つかった場合は、同日中に検知パターンを追加する。

---
