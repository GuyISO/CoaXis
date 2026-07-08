using Godot;

/// <summary>
/// Application 経由で EventHub へアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationEvents
{
    public ApplicationModelEvents Model { get; }
    public ApplicationPickEvents Pick { get; }
    public ApplicationViewportEvents Viewport { get; }
    public ApplicationMeasurementEvents Measurement { get; }

    public ApplicationEvents(Application app)
    {
        Model = new ApplicationModelEvents(app);
        Pick = new ApplicationPickEvents(app);
        Viewport = new ApplicationViewportEvents(app);
        Measurement = new ApplicationMeasurementEvents(app);
    }
}

public sealed class ApplicationModelEvents
{
    private readonly Application _app;

    public ModelEventHub Hub => _app.ModelEventHub;

    public ApplicationModelEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyRootModel() => ModelEventHub.RequestNotifyRootModel();
    public void RequestSetMultiSelectionMode(bool enable) => ModelEventHub.RequestSetMultiSelectionMode(enable);
    public void RequestClearSelection() => ModelEventHub.RequestClearSelection();
    public void RequestToggleModelVisibility(AnyModel model) => ModelEventHub.RequestToggleModelVisibility(model);
    public void RequestAddModel(AnyModel childModel, AnyModel parentModel = null) => ModelEventHub.RequestAddModel(childModel, parentModel);
    public void RequestLoadModel(string path) => ModelEventHub.RequestLoadModel(path);

    public void NotifyRootModel(RootModel rootModel) => ModelEventHub.NotifyRootModel(rootModel);
    public void NotifyModelSelectionState(AnyModel model, bool isSelected) => ModelEventHub.NotifyModelSelectionState(model, isSelected);
    public void NotifyModelVisibilityState(AnyModel model, bool isVisible) => ModelEventHub.NotifyModelVisibilityState(model, isVisible);
}

public sealed class ApplicationPickEvents
{
    private readonly Application _app;

    public PickEventHub Hub => _app.PickEventHub;

    public ApplicationPickEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyPickHandlingMode() => PickEventHub.RequestNotifyPickHandlingMode();
    public void NotifyPickHandlingMode(PickHandlingMode mode) => PickEventHub.NotifyPickHandlingMode(mode);
    public void NotifyPickResult(PickResult pickResult) => PickEventHub.NotifyPickResult(pickResult);
    public void NotifyPickResults(PickResult[] pickResults) => PickEventHub.NotifyPickResults(pickResults);
}

public sealed class ApplicationViewportEvents
{
    private readonly Application _app;

    public ViewportEventHub Hub => _app.ViewportEventHub;

    public ApplicationViewportEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyState() => ViewportEventHub.RequestNotifyState();
    public void RequestMovePositionTo(Vector3 position, bool useTween = false) => ViewportEventHub.RequestMovePositionTo(position, useTween);
    public void RequestMoveRotationTo(Quaternion rotation, bool useTween = false) => ViewportEventHub.RequestMoveRotationTo(rotation, useTween);
    public void RequestSetDistance(float distance, bool useTween = false) => ViewportEventHub.RequestSetDistance(distance, useTween);
    public void RequestSetSizeTo(float size, bool useTween = false) => ViewportEventHub.RequestSetSizeTo(size, useTween);
    public void RequestSetFov(float fov, bool useTween = false) => ViewportEventHub.RequestSetFov(fov, useTween);
    public void RequestTranslate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => ViewportEventHub.RequestTranslate(translation, spaceMode, useTween);
    public void RequestRotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => ViewportEventHub.RequestRotate(rotation, spaceMode, useTween);
    public void RequestZoom(float exponent, bool useTween = false) => ViewportEventHub.RequestZoom(exponent, useTween);
    public void RequestSetProjectionType(Camera3D.ProjectionType type) => ViewportEventHub.RequestSetProjectionType(type);
    public void RequestToggleProjectionType() => ViewportEventHub.RequestToggleProjectionType();
    public void RequestFit(AnyModel[] targetModels, bool useTween = false) => ViewportEventHub.RequestFit(targetModels, useTween);
    public void RequestAlignNormalTo(Vector3 normal, bool useTween = false) => ViewportEventHub.RequestAlignNormalTo(normal, useTween);
    public void RequestDecidePickRect(Vector2 startPosition, Vector2 endPosition) => ViewportEventHub.RequestDecidePickRect(startPosition, endPosition);

    public void NotifyInteractionMode(ViewportInteractionMode mode) => ViewportEventHub.NotifyInteractionMode(mode);
    public void NotifyPosition(Vector3 position) => ViewportEventHub.NotifyPosition(position);
    public void NotifyRotation(Quaternion rotation) => ViewportEventHub.NotifyRotation(rotation);
    public void NotifyDistance(float distance) => ViewportEventHub.NotifyDistance(distance);
    public void NotifySize(float size) => ViewportEventHub.NotifySize(size);
    public void NotifyFov(float fov) => ViewportEventHub.NotifyFov(fov);
    public void NotifyProjectionType(Camera3D.ProjectionType type) => ViewportEventHub.NotifyProjectionType(type);
    public void NotifyArcballRadius(float radius) => ViewportEventHub.NotifyArcballRadius(radius);
    public void NotifyArcballHandle(Vector3 position) => ViewportEventHub.NotifyArcballHandle(position);
    public void NotifyPickRect(Vector2 startPosition, Vector2 endPosition) => ViewportEventHub.NotifyPickRect(startPosition, endPosition);
    public void NotifyPickResult(PickResult pickResult) => ViewportEventHub.NotifyPickResult(pickResult);
}

public sealed class ApplicationMeasurementEvents
{
    private readonly Application _app;

    public MeasurementEventHub Hub => _app.MeasurementEventHub;

    public ApplicationMeasurementEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyMeasurementResult() => MeasurementEventHub.RequestNotifyMeasurementResult();
    public void RequestPickPoint(int pointIndex) => MeasurementEventHub.RequestPickPoint(pointIndex);
    public void NotifyMeasurementResult(MeasurementResult result) => MeasurementEventHub.NotifyMeasurementResult(result);
}
