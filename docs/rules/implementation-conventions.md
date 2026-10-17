# C# 実装規約

CoaXisのC#コードに適用する正本。機能・外部契約の要件が優先する。

## Region

クラスのregionは必要なものだけを使い、次の順に置く: `Fields / Properties / Signals / Lifecycle / Events / Methods / Helpers`。

Application配下のHubは、この7regionを空でもすべて置く。`[Signal]` 宣言と `NotifyXxx` はSignals、`OnXxx` と購読・解除はEvents、操作はMethods、その他の補助はHelpersに置く。`Request` / `Notification` / `Actions` 等の独自regionは作らない。Nodeの購読解除順は [Godot lifecycle skill](../../.github/skills/godot-node-lifecycle-rules/SKILL.md) に従う。

## コメント

- public型・メンバーにXML `summary` を付け、引数・戻り値は必要に応じて `param` / `returns` で説明する。
- Application HubはLifecycle以外の全メンバーにXML `summary` を付ける。
- 設計意図・制約が必要な箇所だけ日本語で説明し、自明な逐語コメントは書かない。仕様変更時はコメントも同期する。

## 命名・変更

- 型・公開メンバーは `PascalCase`、privateフィールドは `_camelCase`、引数・ローカル変数は `camelCase`。`const` は `PascalCase`。
- ハンドラーは `OnXxx`、非同期メソッドは `Async`。外部契約上固定の名前を優先する。
- 大きな整理では構造変更と振る舞い変更を分け、既存コードは変更範囲内で段階適用する。
- WinFormsフォームの改名時は `.cs` / `.Designer.cs` / `.resx` と partial class を揃え、solution buildで確認する。
