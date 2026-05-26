# HTTP PoC Test Guide

このドキュメントは、`CoaXisViewer` の HTTP PoC をローカル PC と社内 LAN 上の別 PC で確認する手順をまとめたものです。

## 1. テストアプリの場所

HTTP テストサーバーアプリ本体は以下です。

- `tests/HttpTestServer/MainForm.cs`
- `tests/HttpTestServer/Program.cs`

## 2. ローカル PC での確認手順

1. `HttpTestServer` を起動する。
2. 以下を設定して `Start Server` を押す。
   - Host: `localhost`
   - Port: `8088`
   - Path: `api/echo`
3. Godot 側 (`HttpClient` ノード) の設定を合わせる。
   - `serverBaseUrl`: `http://localhost:8088/`
   - `endpointPath`: `api/echo`
4. `SendHttp` ボタンを押す。
5. サーバー側ログで `REQ BODY`、Godot 側で応答文字列を確認する。

## 3. LAN 上の別 PC での確認手順

例: サーバーPC名が `KA24-3246PP` の場合。

### サーバーPC側

1. `HttpTestServer` を起動する。
2. Host は以下のどちらかを設定する。
   - 推奨: `+` (全 NIC で待ち受け)
   - 代替: `KA24-3246PP` (PC名で待ち受け)
3. Port は `8088`、Path は `api/echo` を設定して `Start Server`。

### クライアントPC側 (Godot)

1. `serverBaseUrl` を `http://KA24-3246PP:8088/` に設定。
2. `endpointPath` を `api/echo` に設定。
3. `SendHttp` を押して応答確認。

## 4. PC名とIPアドレスについて

- PC名で名前解決できる環境なら、IPアドレスは必須ではありません。
- `\\KA24-3246PP` は SMB (共有フォルダ) 形式であり、HTTP の URL には使いません。
- HTTP では `http://KA24-3246PP:8088/` の形式で指定します。
- 名前解決が不安定な環境では `http://192.168.x.x:8088/` のように IP 指定へ切り替えてください。

## 5. 接続できない場合のチェック

1. クライアントPCから `ping KA24-3246PP` が通るか確認。
2. サーバーPCのファイアウォールで `TCP 8088` の受信を許可。
3. 必要に応じて URL ACL を追加 (管理者権限で実行)。

```powershell
netsh http add urlacl url=http://+:8088/api/echo/ user=Everyone
```

## 6. 補足

- テストサーバーのレスポンスは `Response template` で変更できます。
- 既定値 `RECEIVED: {request}` の `{request}` は受信文字列に置換されます。
