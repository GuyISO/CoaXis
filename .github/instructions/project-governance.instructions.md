---
description: "Use when changing CoaXis project scope, specifications, documentation, or development procedures."
---
# プロジェクト運用

- 仕様・契約・手順を変えたら、対応する正本を同じ作業で更新する。本文は日本語、ファイル名は英語のkebab-caseとする。
- C#変更は `dotnet build .\CoaXis.sln`、文書変更は `scripts/quality/check-mojibake.ps1` で確認する。
- ファイル移動・削除時は参照を更新し、リンク切れがないことを確認する。
- 再利用する設計判断はルールへ反映し、未反映の判断だけを設計履歴に残す。