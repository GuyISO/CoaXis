using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>Property選択通知のIPC契約を検証する。</summary>
public partial class ModelPropertySelectedIpcContract : Node
{
    /// <summary>分離されたNamed Pipeでテストを実行する。</summary>
    public async void Run()
    {
        try
        {
            await RunChecks();
            GD.Print("ModelPropertySelected IPC contract passed.");
            GetTree().Quit(0);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            GetTree().Quit(1);
        }
    }

    private async Task RunChecks()
    {
        Check(IpcPayloadTypeMap.TryGetPayloadType(IpcEventType.FromViewer.ModelPropertySelected, out Type payloadType)
            && payloadType == typeof(ModelPropertySelectedPayload), "payload型の対応が不正");

        Guid entityId = Guid.NewGuid();
        Guid firstId = Guid.NewGuid();
        Guid secondId = Guid.NewGuid();
        Application.Model.ModelSet.Add(
            new[] { new ModelEntityDto { Id = entityId, Name = "IPC selection test" } },
            new[]
            {
                new ModelPropertyDto { Id = firstId, ParentId = entityId, PropertyType = "First" },
                new ModelPropertyDto { Id = secondId, ParentId = entityId, PropertyType = "Second" }
            });

        Application.Ipc.StopPipeServer();
        string pipeName = $"CoaXisSelectionTest-{Guid.NewGuid():N}";
        Application.Ipc.StartPipeServer(pipeName);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        await client.ConnectAsync(timeout.Token);
        while (!Application.Ipc.IsClientConnected)
        {
            timeout.Token.ThrowIfCancellationRequested();
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        }
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        using var reader = new StreamReader(client, Encoding.UTF8, false, 1024, leaveOpen: true);
        var property = Application.Model.Property.Selection;
        property.Set(firstId);
        await ExpectProperty(reader, timeout.Token, firstId);
        property.Add(firstId);
        property.Add(secondId);
        await ExpectProperty(reader, timeout.Token, firstId, secondId);
        property.Remove(firstId);
        await ExpectProperty(reader, timeout.Token, secondId);
        property.Toggle(secondId);
        await ExpectProperty(reader, timeout.Token);

        property.Add(new[] { firstId, secondId });
        await ExpectProperty(reader, timeout.Token, firstId, secondId);
        Application.Model.ModelSet.Remove(Array.Empty<Guid>(), new[] { firstId });
        await ExpectProperty(reader, timeout.Token, secondId);
        Application.Model.Entity.Selection.Set(entityId);
        await ExpectProperty(reader, timeout.Token);
        IpcEnvelope entityNotification = await ReadNotification(reader, timeout.Token);
        Check(entityNotification.EventType == IpcEventType.FromViewer.ModelEntitySelected,
            "Entity切替時のEntity通知が失われた");
        var entityPayload = entityNotification.Payload.Deserialize<ModelEntitySelectedPayload>(IpcJsonOptions.Default);
        Check(entityPayload != null && entityPayload.EntityIds.SequenceEqual(new[] { entityId }),
            "Entity切替時のEntity一覧が不正");

        Application.Model.Property.Selection.Set(secondId);
        entityNotification = await ReadNotification(reader, timeout.Token);
        Check(entityNotification.EventType == IpcEventType.FromViewer.ModelEntitySelected,
            "Property切替時のEntity解除通知が失われた");
        entityPayload = entityNotification.Payload.Deserialize<ModelEntitySelectedPayload>(IpcJsonOptions.Default);
        Check(entityPayload != null && entityPayload.EntityIds.Count == 0, "Entity解除通知が空ではない");
        await ExpectProperty(reader, timeout.Token, secondId);
        property.Clear();
        await ExpectProperty(reader, timeout.Token);

        Application.Ipc.StopPipeServer();
        Application.Model.ModelSet.Clear();
    }

    private static async Task ExpectProperty(StreamReader reader, CancellationToken token, params Guid[] expected)
    {
        IpcEnvelope envelope = await ReadNotification(reader, token);
        Check(envelope.EventType == IpcEventType.FromViewer.ModelPropertySelected, "Property選択イベント名が不正");
        Check(envelope.Source == "Viewer" && envelope.CorrelationId == null, "通知エンベロープが不正");
        Check(envelope.Payload.TryGetProperty("propertyIds", out JsonElement ids) && ids.ValueKind == JsonValueKind.Array,
            "propertyIds配列が存在しない");
        var payload = envelope.Payload.Deserialize<ModelPropertySelectedPayload>(IpcJsonOptions.Default);
        Check(payload != null && payload.PropertyIds.SequenceEqual(expected.OrderBy(id => id)), "Property選択集合が不正");
    }

    private static async Task<IpcEnvelope> ReadNotification(StreamReader reader, CancellationToken token)
    {
        string line = await reader.ReadLineAsync(token);
        Check(line != null, "通知を受信する前に接続が切断された");
        IpcEnvelope envelope = JsonSerializer.Deserialize<IpcEnvelope>(line, IpcJsonOptions.Default);
        Check(envelope != null, "通知エンベロープが空");
        return envelope;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
