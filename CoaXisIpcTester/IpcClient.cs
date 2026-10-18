using CoaXis.Protocol.Viewer;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CoaXis.IpcTester;

/// <summary>
/// ViewerのNamed PipeへIPC要求を1件送信し、応答を受け取る。
/// </summary>
public static class IpcClient
{
    /// <summary>
    /// エンベロープをViewerへ送り、応答エンベロープを返す。
    /// </summary>
    /// <param name="pipeName">Viewerが待ち受けるNamed Pipe名</param>
    /// <param name="envelope">送信するIPC要求</param>
    /// <param name="notificationReceived">応答前に届いた非同期通知の受信処理</param>
    /// <returns>Viewerから受け取った生の応答JSON</returns>
    public static async Task<string> SendAsync(
        string pipeName,
        IpcEnvelope envelope,
        Action<string> notificationReceived = null)
    {
        using var pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await pipe.ConnectAsync(3000);

        using var reader = new StreamReader(pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
        using var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\n"
        };

        string requestJson = JsonSerializer.Serialize(envelope, IpcJsonOptions.Default);
        await writer.WriteLineAsync(requestJson);

        DateTime responseDeadline = DateTime.UtcNow.AddSeconds(5);
        while (true)
        {
            TimeSpan remainingTime = responseDeadline - DateTime.UtcNow;
            if (remainingTime <= TimeSpan.Zero)
            {
                throw new TimeoutException("Viewerからの応答がタイムアウトしました。");
            }

            string responseJson = await reader.ReadLineAsync().WaitAsync(remainingTime);
            if (responseJson == null)
            {
                throw new IOException("Viewerから応答がありませんでした。");
            }

            IpcEnvelope response = JsonSerializer.Deserialize<IpcEnvelope>(responseJson, IpcJsonOptions.Default);
            if (response?.EventType == IpcEventType.Common.Result &&
                response.CorrelationId == envelope.EventId)
            {
                return responseJson;
            }

            notificationReceived?.Invoke(responseJson);
        }
    }
}