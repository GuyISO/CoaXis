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

    public Selection Hub => _app.Selection;

    public ApplicationSelectionService(Application app)
    {
        _app = app;
    }

    public IReadOnlyCollection<AnyModel> GetModels => Selection.GetModels;
    public int Count => Selection.Count;

    public bool Contains(AnyModel model) => Selection.Contains(model);
    public AnyModel[] GetModelArray() => Selection.GetModelArray();
    public void Set(AnyModel model) => Selection.Set(model);
    public void Set(AnyModel[] models) => Selection.Set(models);
    public bool Add(AnyModel model) => Selection.Add(model);
    public void Add(AnyModel[] models) => Selection.Add(models);
    public bool Remove(AnyModel model) => Selection.Remove(model);
    public void Remove(AnyModel[] models) => Selection.Remove(models);
    public void Toggle(AnyModel model) => Selection.Toggle(model);
    public void Toggle(AnyModel[] models) => Selection.Toggle(models);
    public bool Clear() => Selection.Clear();
}

public sealed class ApplicationUiService
{
    private readonly Application _app;

    public UiManager Hub => _app.UiManager;

    public ApplicationUiService(Application app)
    {
        _app = app;
    }

    public void Show(Container container) => UiManager.Show(container);
}

public sealed class ApplicationSettingsService
{
    private readonly Application _app;

    public SettingsService Hub => _app.SettingsService;

    public ApplicationSettingsService(Application app)
    {
        _app = app;
    }

    public ViewerSettings Current => SettingsService.Current;
}
