# 文字エンコーディング事故の再発防止ルール

この文書は、文字化け・改行崩壊・コメント崩壊などの事故を再発させないための運用ルールを定義する。

## 1. 目的

1. 文字コード事故を事前検知し、本流ブランチへ流入させない。
2. 事故が発生した場合に、原因・影響・対策を必ず記録して再利用可能な知識へ変換する。
3. 修復時に機能退行を防ぎ、作業手順を標準化する。

## 2. 予防ルール（Must）

1. 一括置換や上書きを伴う操作は、原則として差分編集（`apply_patch` 等）を優先する。
2. PowerShell の `Set-Content` / `>` による C# ソースの一括上書きは原則禁止とする。
3. 文字列置換を広範囲に適用する場合、実行前後で `check: mojibake` を実行する。
4. C# 変更時は `dotnet build .\\CoaXis.sln` の成功を確認する。
5. 文字化けを検知したら、修復より先に対象ファイルを確定し、必要に応じて `git checkout -- <file>` で正常状態へ戻す。

## 3. 自動検知（Must）

1. `scripts/quality/check-mojibake.ps1` を使用し、既知の文字化けパターンを検出する。
2. VS Code タスク `check: mojibake` を、レビュー前の必須チェックとする。
3. VS Code タスク `verify: build + encoding` を、最終確認の標準手順とする。

## 4. 事故発生時の標準フロー（Must）

1. 検知: `check: mojibake` を実行し、対象ファイルと行を確定する。
2. 切り分け: ロジック破損有無をビルドで確認する。
3. 復旧: まず構文を復旧し、次にコメント・ドキュメントを復旧する。
4. 検証: `check: mojibake` と `dotnet build .\\CoaXis.sln` を再実行する。
5. 記録: 技術ナレッジベースへ原因・恒久対策・運用反映を記録する。

## 5. 失敗学習の仕組み（Should）

1. 同種事故が再発した場合、検知パターンを `check-mojibake.ps1` に追加する。
2. 事故の再発防止策は、ルール本文ではなくチェック手順まで必ず実装する。
3. 修復PRは「機能修復」と「コメント整備」を分けてレビューしやすくする。

## 6. 実行コマンド

```powershell
# 文字化け検知
powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\quality\check-mojibake.ps1

# 変更全体の検証
# VS Code Tasks: verify: build + encoding
```
