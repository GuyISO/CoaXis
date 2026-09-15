/*
目的: 接続先のSQL Serverとデータベースを確認する
対象: CoaXisServer
実行前提: 対象データベースへ接続済みであること
*/

SELECT
    @@SERVERNAME AS ServerName,
    DB_NAME() AS DatabaseName,
    SUSER_SNAME() AS LoginName,
    GETDATE() AS CurrentTime;