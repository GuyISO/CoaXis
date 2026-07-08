using Godot;
using System.Collections.Generic;

/// <summary>
/// Application 経由でサービス群へアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationServices
{
    public ApplicationSelectionService Selection { get; }
    public ApplicationUiService Ui { get; }
    public ApplicationSettingsService Settings { get; }

    public ApplicationServices(Application app)
    {
        Selection = new ApplicationSelectionService(app);
        Ui = new ApplicationUiService(app);
        Settings = new ApplicationSettingsService(app);
    }
}

public sealed class ApplicationSelectionService
{
    private readonly Application _app;

    public Selection Hub => _app.SelectionNode;

    public ApplicationSelectionService(Application app)
    {
        _app = app;
    }

    public IReadOnlyCollection<AnyModel> GetModels => Hub.GetModels;
    public int Count => Hub.Count;

    public bool Contains(AnyModel model) => Hub.Contains(model);
    public AnyModel[] GetModelArray() => Hub.GetModelArray();
    public void Set(AnyModel model) => Hub.Set(model);
    public void Set(AnyModel[] models) => Hub.Set(models);
    public bool Add(AnyModel model) => Hub.Add(model);
    public void Add(AnyModel[] models) => Hub.Add(models);
    public bool Remove(AnyModel model) => Hub.Remove(model);
    public void Remove(AnyModel[] models) => Hub.Remove(models);
    public void Toggle(AnyModel model) => Hub.Toggle(model);
    public void Toggle(AnyModel[] models) => Hub.Toggle(models);
    public bool Clear() => Hub.Clear();
}

public sealed class ApplicationUiService
{
    private readonly Application _app;

    public UiManager Hub => _app.UiManagerNode;

    public ApplicationUiService(Application app)
    {
        _app = app;
    }

    public void Show(Container container) => Hub.Show(container);
}

public sealed class ApplicationSettingsService
{
    private readonly Application _app;

    public SettingsService Hub => _app.SettingsServiceNode;

    public ApplicationSettingsService(Application app)
    {
        _app = app;
    }

    public ViewerSettings Current => Hub.Current;
}
