# 実装規約（クラス構成・region運用）

この文書は、CoaXis の C# 実装で「クラス内のどこに何を書くか」を統一するための規約です。
Godot C# / 非Godot C# の両方に適用します。

## 1. 目的

1. クラス内の探索性を上げ、保守時の迷いをなくす。
2. レビュー時に差分の意味を読み取りやすくする。
3. PoC 由来の自由配置を段階的に整理し、実装品質を安定化する。

## 2. クラス内の標準順序

クラスメンバーは次の順序を基本とする。

1. `Signals`（必要な場合のみ）
2. `Fields`
3. `Properties`
4. `Lifecycle`
5. `Events`（必要な場合のみ）
6. `Public API`
7. `Internal Helpers`

補足:
- `Signals` は Godot の `[Signal]` 宣言がある場合のみ使用する。
- `Events` は、シグナル/イベント購読のコールバックに加え、ユーザー操作を起点に直接呼び出される処理（ボタン押下、メニュー実行など）も配置する。
- 該当する要素がないセクションは省略してよい。
- 1クラスが小さい場合でも、将来拡張を見越して順序は維持する。

## 3. region 名の標準

`#region` 名は次の語に統一する。

- `Signals`
- `Fields`
- `Properties`
- `Lifecycle`
- `Events`
- `Public API`
- `Internal Helpers`

### 3.1 EventHub クラスの例外

`CoaXisViewer/src/autoload/event/*EventHub.cs` については、通知責務の見通しを優先して次の region 名を許可する。

- `--------------------------------------- Request ---------------------------------------`
- `--------------------------------------- Notification ---------------------------------------`

この例外は EventHub クラスに限定し、他クラスでは使用しない。

EventHub では次の順序を推奨する。

1. `Properties`
2. `Lifecycle`
3. `Request` region
4. `Notification` region

禁止例（統一のため使用しない）:
- `Event Handlers`
- `Signal Handlers`
- `Input Handling`
- `Drawing`
- `State Management`
- `Node Resolution`
- `Parsing Helpers`

補足:
- `Request` / `Notification` の装飾付き region は EventHub 例外でのみ許可する。

`Events` 以外で詳細分類が必要な場合は、`Internal Helpers` の中でメソッド順・コメントで意図を示す。

## 4. コメント方針

1. `public` クラス/メソッド/プロパティには XML ドキュメントコメントを付与する。
2. 複雑な処理ブロックには、日本語で「意図・理由・制約」が分かるコメントを付与する。
3. 自明な逐語コメントは避ける。

## 5. WinForms UI 定義方針

1. 新規 WinForms フォームは、原則として Designer で UI を定義する。
2. フォームの子コントロールを `InitializeComponent` 以外で大量に動的生成しない。
3. 実行時に動的生成が必要な場合は、対象を限定した例外として扱い、理由をコードコメントまたは仕様に記録する。
4. `tests/` 配下のテスト用フォームでも原則は同じで、コードで組み立てる場合は「既存の例外・移行対象」であることを明示する。
5. Designer で空に見えるフォームでも、実行時に UI が成立する設計は避け、できるだけ Designer 上で構造が追える形にする。

補足:
- 既存のコード生成フォームを発見した場合は、すぐに壊さず、移行方針を別タスクまたは別変更で整理する。
- グラフやエディタ内ビューなど、コンテンツの性質上動的生成が適切な画面は例外として許容する。

## 6. C# 命名規約

1. クラス・interface・record・enum・delegate は `PascalCase` を使用する。
2. public/protected/internal のメソッド名は `PascalCase` を使用する。
3. public/protected/internal のプロパティ名は `PascalCase` を使用する。
4. private フィールドは `_camelCase` を使用する。
5. private static readonly フィールド（疑似定数を含む）も `_camelCase` を使用する。
6. ローカル変数とメソッド引数は `camelCase` を使用する。
7. `const` フィールドはアクセス修飾子に関わらず `PascalCase` を使用する。
8. bool のメソッド/プロパティは、意図が伝わる `Is` / `Has` / `Can` などの接頭語を優先する。
9. イベントハンドラーは `OnXxx` 形式で命名し、非同期メソッドは `Async` サフィックスを付与する。

補足:
- Godot シグナル名や JSON キー名など、外部仕様で名称が固定される場合は、その仕様を優先する。
- 既存コードへ段階適用する際は、機能変更と命名変更を可能な限り分離する。

## 7. 運用ルール

1. 既存クラスへ変更を入れる際、可能な範囲で本規約の順序へ揃える。
2. 大規模改修時は、ロジック変更と構造整理を同一コミットに混在させない。
3. 規約変更が必要な場合は、本ファイルを先に更新してから実装へ反映する。

## 8. 適用対象（現時点）

- `CoaXisViewer/src/**/*.cs`
- `tests/**/*.cs`

将来的に他プロジェクトへ拡大する場合は、本章に追記する。
