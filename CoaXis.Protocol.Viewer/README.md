# CoaXis.Protocol.Viewer

`CoaXis.Protocol.Viewer` は、CoaXisViewerと外部クライアント間のIPCで共有する.NETクラスライブラリです。イベント名、メッセージ形式、payload DTOを共通化し、送信側と受信側で同じ型を使えるようにします。

## 役割と範囲

このライブラリが提供するもの:

- IPCメッセージの共通エンベロープと生成ヘルパー
- プロトコルバージョン、イベント名、標準エラーコードなどの定数
- Entity / Property DTOと各イベントのpayload型
- JSONシリアライズ設定と、イベント名からpayload型を引く対応表

このライブラリ自体はNamed Pipeの接続・読み書き、要求の実行、Viewer内のモデル状態管理を行いません。また、payloadを受け取っただけで業務上の妥当性を保証するものでもありません。通信と受信値の検証は各アプリケーションの責務です。

## メッセージの概念

すべてのメッセージは `IpcEnvelope` を共通の外枠として使います。

| 項目 | 意味 |
|---|---|
| `EventId` | メッセージごとの識別子 |
| `EventType` | イベント種別。`IpcEventType` の定数を使用 |
| `Version` | プロトコルバージョン。`IpcProtocolVersion.Current` を使用 |
| `Timestamp` | UTCの送信時刻 |
| `Source` | 送信元を示す文字列 |
| `CorrelationId` | 要求と応答を結ぶID。応答では要求の`EventId`を指定 |
| `Payload` | イベントごとのデータを格納するJSON値 |

要求に対する応答は `Result` イベントで返り、`CorrelationId` で元の要求と対応付けます。Viewerからの選択・Manipulate通知は要求応答ではないため、要求との対応付けには使いません。応答と通知は別々に届くことがあるため、イベント種別と相関IDの両方を確認してください。

### イベントの方向

| 定数 | 方向 | 用途 |
|---|---|---|
| `IpcEventType.ToViewer.ReplaceModelSet` | クライアント → Viewer | モデル集合を置換 |
| `IpcEventType.ToViewer.ClearModelSet` | クライアント → Viewer | モデル集合を消去 |
| `IpcEventType.ToViewer.AddModelSet` | クライアント → Viewer | モデル集合を追加 |
| `IpcEventType.ToViewer.RemoveModelSet` | クライアント → Viewer | IDを指定して削除 |
| `IpcEventType.ToViewer.ModifyModelSet` | クライアント → Viewer | 既存データを変更 |
| `IpcEventType.ToViewer.ReadModelSet` | クライアント → Viewer | IDを指定して読み出し |
| `IpcEventType.Common.Result` | Viewer → クライアント | 要求の処理結果 |
| `IpcEventType.FromViewer.ModelEntitySelected` | Viewer → クライアント | Entity選択の通知 |
| `IpcEventType.FromViewer.ModelEntityManipulated` | Viewer → クライアント | EntityのManipulate操作通知 |
| `IpcEventType.FromViewer.ModelPropertyManipulated` | Viewer → クライアント | PropertyのManipulate操作通知 |

## 使い方

リポジトリ内のプロジェクトからは、プロジェクト参照を追加して利用します。

```xml
<ProjectReference Include="..\CoaXis.Protocol.Viewer\CoaXis.Protocol.Viewer.csproj" />
```

### 要求を作成する

次の例は、Root直下にEntityを1件作って `ReplaceModelSet` 要求のJSONを生成します。生成したJSONをNamed Pipeへ送る処理は呼び出し側で行います。

```csharp
using System;
using System.Collections.Generic;
using System.Text.Json;
using CoaXis.Protocol.Viewer;

var entities = new List<ModelEntityDto>
{
    new ModelEntityDto
    {
        Id = Guid.NewGuid(),
        ParentId = ViewerConstants.RootModelEntityId,
        Type = "Part",
        Name = "Sample"
    }
};

ModelSetPayload payload = ModelSetPayload.Create(
    entities,
    new List<ModelPropertyDto>());

IpcEnvelope request = IpcEnvelopeBuilder.Create(
    IpcEventType.ToViewer.ReplaceModelSet,
    "MyClient",
    payload);

string json = JsonSerializer.Serialize(request, IpcJsonOptions.Default);
string line = json + "\n";
```

`IpcEnvelopeBuilder.Create` はイベントID、現在のプロトコルバージョン、UTC時刻を設定します。応答ではないメッセージを作る場合、`correlationId`は省略します。応答を作る場合は第4引数に対応する要求の`EventId`を渡してください。

### 受信メッセージを処理する

エンベロープは共通設定で読み込み、イベントに対応するpayload型がある場合は `IpcPayloadTypeMap` で型を取得できます。

```csharp
using System;
using System.Text.Json;
using CoaXis.Protocol.Viewer;

IpcEnvelope message = JsonSerializer.Deserialize<IpcEnvelope>(
    json,
    IpcJsonOptions.Default);

if (message != null &&
    IpcPayloadTypeMap.TryGetPayloadType(message.EventType, out Type payloadType))
{
    object payload = message.Payload.Deserialize(
        payloadType,
        IpcJsonOptions.Default);
}
```

`IpcPayloadTypeMap` に登録されていないイベントもあり得るため、取得できない場合の扱いは呼び出し側で決めてください。`Result` の通常payloadは `IpcResultPayload` です。`ReadModelSet` の成功応答は追加の `modelSet` を持つ `ReadModelSetResultPayload` なので、その結果を読む場合は該当する派生型としてデシリアライズします。

要求への応答を待つクライアントは、受信したメッセージの `EventType` が `Result` であることと、`CorrelationId` が送信要求の `EventId` と一致することを確認します。通知は応答と誤認せず、別のイベントとして処理します。

## 型とバージョンの使い分け

- `ModelEntityDto` と `ModelPropertyDto` は、IPCでモデルデータを受け渡すためのDTOです。Viewer内部のドメインモデルではありません。
- payload生成には、一覧のコピーや引数確認を行う `Create` メソッドがある型では、そのメソッドを利用します。たとえば `ModelSetPayload.Create` はEntity一覧が空の場合に例外を送出します。
- JSONのプロパティ名は `IpcJsonOptions.Default` によりcamelCaseになります。送受信の両方でこの設定を使ってください。
- バージョン値を固定文字列で重複定義せず、`IpcProtocolVersion.Current` を参照してください。互換性を確認する場合は `IpcProtocolVersion.IsCompatible` を使います。
- `IpcErrorCode` はViewerの `Result` が返す標準エラーコードです。具体的な検証条件はViewer側で判定されます。

## 動作確認

Viewerとの接続、要求送信、応答・通知の確認には、リポジトリ内の[CoaXisIpcTesterガイド](../CoaXisIpcTester/README.md)を参照してください。
