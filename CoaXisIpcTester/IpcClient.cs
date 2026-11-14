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
/// Viewerとの接続を維持し、応答と非同期通知を受信する。
/// </summary>
public sealed class IpcClient : IDisposable
{
    private readonly NamedPipeClientStream _pipe;
    private readonly CancellationTokenSource _cancellation = new();
    private readonly Action<string> _notificationReceived;
    private readonly Action<Exception> _connectionLost;
    private StreamReader _reader;
    private TaskCompletionSource<string> _pendingResponse;
    private string _pendingEventId;
    private bool _disposed;

    /// <summary>
    /// 接続先と受信時の処理を指定する。
    /// </summary>
    /// <param name="pipeName">Viewerが待ち受けるNamed Pipe名</param>
    /// <param name="notificationReceived">応答以外のメッセージを受信したときの処理</param>
    /// <param name="connectionLost">予期せず接続が切断されたときの処理</param>
    public IpcClient(string pipeName, Action<string> notificationReceived, Action<Exception> connectionLost)
    {
        _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        _notificationReceived = notificationReceived;
        _connectionLost = connectionLost;
    }

    /// <summary>
    /// Viewerに接続し、受信を開始する。
    /// </summary>
    public async Task ConnectAsync()
    {
        await _pipe.ConnectAsync(3000, _cancellation.Token);
        _reader = new StreamReader(_pipe, Encoding.UTF8, false, 1024, leaveOpen: true);
        _ = ReadLoopAsync();
    }

    /// <summary>
    /// 要求を送信し、対応するResultを待つ。
    /// </summary>
    /// <param name="envelope">送信するIPC要求</param>
    /// <returns>Viewerから受け取った生の応答JSON</returns>
    public async Task<string> SendAsync(IpcEnvelope envelope)
    {
        if (_pendingResponse != null)
        {
            throw new InvalidOperationException("別の要求の応答を待っています。");
        }

        var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        _pendingEventId = envelope.EventId;
        _pendingResponse = completion;
        try
        {
            string requestJson = JsonSerializer.Serialize(envelope, IpcJsonOptions.Default);
            byte[] data = Encoding.UTF8.GetBytes(requestJson + "\n");
            await _pipe.WriteAsync(data, _cancellation.Token);
            return await completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
        }
        finally
        {
            _pendingResponse = null;
            _pendingEventId = null;
        }
    }

    /// <summary>
    /// 接続を終了し、応答待ちを解除する。
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _cancellation.Cancel();
        _pendingResponse?.TrySetException(new IOException("Viewerとの接続が切断されました。"));
        _pipe.Dispose();
        _reader?.Dispose();
        _cancellation.Dispose();
    }

    private async Task ReadLoopAsync()
    {
        try
        {
            while (true)
            {
                string json = await _reader.ReadLineAsync(_cancellation.Token);
                if (json == null)
                {
                    throw new IOException("Viewerとの接続が切断されました。");
                }

                IpcEnvelope message = JsonSerializer.Deserialize<IpcEnvelope>(json, IpcJsonOptions.Default)
                    ?? throw new InvalidDataException("Viewerから空のメッセージを受信しました。");
                if (message.EventType == IpcEventType.Common.Result &&
                    _pendingResponse != null &&
                    message.CorrelationId == _pendingEventId &&
                    _pendingResponse.TrySetResult(json))
                {
                    continue;
                }

                _notificationReceived(json);
            }
        }
        catch (Exception exception)
        {
            if (!_disposed)
            {
                Dispose();
                _connectionLost(exception);
            }
        }
    }
}
