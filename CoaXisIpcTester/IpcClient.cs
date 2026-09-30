using CoaXis.Protocol.Viewer;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CoaXis.IpcTester;

/// <summary>
/// ViewerのNamed PipeへIPC要求を1件送信し、応答を受け取る。
/// </summary>
internal static class IpcClient
{
    /// <summary>
    /// エンベロープをViewerへ送り、応答エンベロープを返す。
    /// </summary>
    /// <param name="pipeName">Viewerが待ち受けるNamed Pipe名</param>
    /// <param name="envelope">送信するIPC要求</param>
    /// <returns>Viewerから受け取った生の応答JSON</returns>
    public static async Task<string> SendAsync(string pipeName, IpcEnvelope envelope)
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
        return await reader.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(5));
    }
}