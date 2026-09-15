/*
用途: sql/20_seed に置く初期データ投入用のひな形
ファイル名例: 001_insert_default_roles.sql
対象データベース:
*/

-- 再実行時に重複しない条件を確認してからINSERTする
SELECT DB_NAME() AS CurrentDatabase;