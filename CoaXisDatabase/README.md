# SQLServerTest

ローカルの SQL Server Express（CoaXisServer）で使用するクエリを管理するフォルダです。

## フォルダ構成

```text
SQLServerTest/
├─ README.md
├─ sql/
│  ├─ 00_admin/   サーバー・データベースの確認、メンテナンス
│  ├─ 01_read/    SELECT、調査、集計。データを変更しないクエリ
│  ├─ 02_write/   INSERT、UPDATE、DELETE。実行前確認が必要
│  ├─ 10_schema/  CREATE TABLE、ALTER TABLEなどのDB構造
│  ├─ 20_seed/    初期データ、固定のマスターデータ
│  ├─ 30_migrations 運用開始後のDB構造・データの差分変更
│  └─ 90_archive/ 使用終了・保管用クエリ
├─ templates/     新しいクエリを作るときのひな形
└─ TestDatabase.xlsx
```

## VS Codeでの実行

1. `sql` 配下の `.sql` ファイルを開く。
2. SQL Server拡張機能で `MS SQL: Connect` を実行し、対象データベースへ接続する。
3. 実行したいSQLを選択する。選択しない場合はファイル全体が対象になる。
4. `Ctrl + Shift + E` または `Execute Query` で実行する。
5. 結果とメッセージを確認し、必要なら実行日時と目的をファイルに記録する。

## 命名規則

- ファイル名は小文字のスネークケースで、`目的_対象.sql` の形式にする。
- 英小文字、数字、アンダースコアを使い、スペース・日本語・曖昧な名前は避ける。
- `kebab-case`（例: `query-template.sql`）も技術的には使えるが、SQLオブジェクト名や既存のSQL規約と揃えやすいスネークケースを採用する。
- 通常の調査SQLには実行順の番号を付けない。`10_schema`、`20_seed`、`30_migrations` 内だけ、3桁の連番を付ける。
- 順番が必要なファイルは `001_create_users.sql`、`002_insert_default_roles.sql` のようにする。同じフォルダ内で番号を重複させない。
- 例: `find_active_users.sql`, `update_customer_status.sql`
- 同じ目的の改訂版は、必要な場合だけ末尾に `_v2` を付ける。日付だけでの大量複製は避ける。

## クエリのルール

- ファイル先頭に目的、対象データベース、前提条件を書く。
- 読み取り系は `01_read`、変更系は `02_write` に置く。
- 初回構築は `10_schema` → `20_seed` の順に実行する。
- 既存DBへの追加・変更は `30_migrations` に置き、ファイル名の番号順に実行する。
- `schema` はテーブル・制約・インデックスなどの構造、`seed` は再作成可能な初期データや固定マスターデータに限定する。
- 適用済みの `schema` / `seed` / `migration` は書き換えず、修正が必要なら次の番号のファイルを追加する。
- `seed` は `IF NOT EXISTS` や `MERGE` などを使い、可能な範囲で再実行しても重複しないようにする。
- 変更系は、最初に同じ条件の `SELECT` または `BEGIN TRAN ... ROLLBACK` で対象件数を確認する。
- `UPDATE` と `DELETE` では `WHERE` 句を省略しない。
- 本番環境や共有DBで実行する前に、接続先と対象件数を必ず確認する。
- パスワード、接続文字列、個人情報をクエリやREADMEに保存しない。
- 実行したくないSQLはコメントアウトではなく、実行範囲を選択してから実行する。

## 変更系クエリの基本形

```sql
USE [YourDatabase];
GO

SET XACT_ABORT ON;

BEGIN TRANSACTION;

-- まず対象件数と対象データを確認する
SELECT TOP (100) *
FROM dbo.YourTable
WHERE <condition>;

-- 内容を確認してから、必要な変更を有効化する
-- UPDATE dbo.YourTable
-- SET <column> = <value>
-- WHERE <condition>;

ROLLBACK TRANSACTION; -- 確認後、保存する場合だけ COMMIT に変更
```

`GO` はSQL Server拡張機能やSSMSが扱うバッチ区切りです。アプリケーションから直接実行するSQLにはそのまま渡さないでください。

## 初回構築と実行順

新しいデータベースを作るときは、次の順序で実行する。

1. `10_schema` の番号順で、テーブル・制約・インデックスを作成する。
2. `20_seed` の番号順で、初期データや固定マスターデータを投入する。
3. 必要なら `01_read` の確認SQLで件数と内容を確認する。
4. 既存DBに変更を加えるときは、`30_migrations` に次の番号のSQLを追加する。

`30_migrations` の適用履歴を複数環境で厳密に管理する必要が出たら、DbUp、Flyway、Liquibaseなどの導入を検討する。手動実行だけで運用する間は、実行日時・接続先・結果を別途記録する。
