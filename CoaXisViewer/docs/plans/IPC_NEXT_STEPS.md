# IPC Next Steps

## 1. 目標
IpcService の最小コマンド実行実装に、実際のNamedPipe受信経路と応答返却を接続する。

## 2. 実装ステップ
1. NamedPipeServerStream の起動/停止管理を IpcService に追加
2. 1行JSONを受信して TryHandleIncomingMessage に渡す
3. 処理結果を response envelope として返却
4. 受信ループを中断可能（キャンセル対応）にする
5. 終了時に Pipe を確実に Dispose する

## 3. 応答フォーマット案

```json
{
  "command": "Result",
  "payload": {
    "ok": true,
    "request": "LoadModel",
    "message": "accepted"
  }
}
```

失敗時:

```json
{
  "command": "Result",
  "payload": {
    "ok": false,
    "request": "ApplyCameraPreset",
    "errorCode": "INVALID_PAYLOAD",
    "message": "fov must be number"
  }
}
```

## 4. テスト観点
- 不正JSON受信時にループ継続できる
- 未対応command受信時に正常に失敗応答を返す
- 連続受信時にメモリリークしない
- Viewer終了時にPipeスレッドが残らない
