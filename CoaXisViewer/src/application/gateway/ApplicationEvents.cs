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

    public ModelEventHub Hub => _app.ModelEventHubNode;

    public ApplicationModelEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyRootModel() => Hub.RequestNotifyRootModel();
    public void RequestSetMultiSelectionMode(bool enable) => Hub.RequestSetMultiSelectionMode(enable);
    public void RequestClearSelection() => Hub.RequestClearSelection();
    public void RequestToggleModelVisibility(AnyModel model) => Hub.RequestToggleModelVisibility(model);
    public void RequestAddModel(AnyModel childModel, AnyModel parentModel = null) => Hub.RequestAddModel(childModel, parentModel);
    public void RequestLoadModel(string path) => Hub.RequestLoadModel(path);

    public void NotifyRootModel(RootModel rootModel) => Hub.NotifyRootModel(rootModel);
    public void NotifyModelSelectionState(AnyModel model, bool isSelected) => Hub.NotifyModelSelectionState(model, isSelected);
    public void NotifyModelVisibilityState(AnyModel model, bool isVisible) => Hub.NotifyModelVisibilityState(model, isVisible);
}

public sealed class ApplicationPickEvents
{
    private readonly Application _app;

    public PickEventHub Hub => _app.PickEventHubNode;

    public ApplicationPickEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyPickHandlingMode() => Hub.RequestNotifyPickHandlingMode();
    public void NotifyPickHandlingMode(PickHandlingMode mode) => Hub.NotifyPickHandlingMode(mode);
    public void NotifyPickResult(PickResult pickResult) => Hub.NotifyPickResult(pickResult);
    public void NotifyPickResults(PickResult[] pickResults) => Hub.NotifyPickResults(pickResults);
}

public sealed class ApplicationViewportEvents
{
    private readonly Application _app;

    public ViewportEventHub Hub => _app.ViewportEventHubNode;

    public ApplicationViewportEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyState() => Hub.RequestNotifyState();
    public void RequestMovePositionTo(Vector3 position, bool useTween = false) => Hub.RequestMovePositionTo(position, useTween);
    public void RequestMoveRotationTo(Quaternion rotation, bool useTween = false) => Hub.RequestMoveRotationTo(rotation, useTween);
    public void RequestSetDistance(float distance, bool useTween = false) => Hub.RequestSetDistance(distance, useTween);
    public void RequestSetSizeTo(float size, bool useTween = false) => Hub.RequestSetSizeTo(size, useTween);
    public void RequestSetFov(float fov, bool useTween = false) => Hub.RequestSetFov(fov, useTween);
    public void RequestTranslate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => Hub.RequestTranslate(translation, spaceMode, useTween);
    public void RequestRotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => Hub.RequestRotate(rotation, spaceMode, useTween);
    public void RequestZoom(float exponent, bool useTween = false) => Hub.RequestZoom(exponent, useTween);
    public void RequestSetProjectionType(Camera3D.ProjectionType type) => Hub.RequestSetProjectionType(type);
    public void RequestToggleProjectionType() => Hub.RequestToggleProjectionType();
    public void RequestFit(AnyModel[] targetModels, bool useTween = false) => Hub.RequestFit(targetModels, useTween);
    public void RequestAlignNormalTo(Vector3 normal, bool useTween = false) => Hub.RequestAlignNormalTo(normal, useTween);
    public void RequestDecidePickRect(Vector2 startPosition, Vector2 endPosition) => Hub.RequestDecidePickRect(startPosition, endPosition);

    public void NotifyInteractionMode(ViewportInteractionMode mode) => Hub.NotifyInteractionMode(mode);
    public void NotifyPosition(Vector3 position) => Hub.NotifyPosition(position);
    public void NotifyRotation(Quaternion rotation) => Hub.NotifyRotation(rotation);
    public void NotifyDistance(float distance) => Hub.NotifyDistance(distance);
    public void NotifySize(float size) => Hub.NotifySize(size);
    public void NotifyFov(float fov) => Hub.NotifyFov(fov);
    public void NotifyProjectionType(Camera3D.ProjectionType type) => Hub.NotifyProjectionType(type);
    public void NotifyArcballRadius(float radius) => Hub.NotifyArcballRadius(radius);
    public void NotifyArcballHandle(Vector3 position) => Hub.NotifyArcballHandle(position);
    public void NotifyPickRect(Vector2 startPosition, Vector2 endPosition) => Hub.NotifyPickRect(startPosition, endPosition);
    public void NotifyPickResult(PickResult pickResult) => Hub.NotifyPickResult(pickResult);
}

public sealed class ApplicationMeasurementEvents
{
    private readonly Application _app;

    public MeasurementEventHub Hub => _app.MeasurementEventHubNode;

    public ApplicationMeasurementEvents(Application app)
    {
        _app = app;
    }

    public void RequestNotifyMeasurementResult() => Hub.RequestNotifyMeasurementResult();
    public void RequestPickPoint(int pointIndex) => Hub.RequestPickPoint(pointIndex);
    public void NotifyMeasurementResult(MeasurementResult result) => Hub.NotifyMeasurementResult(result);
}
