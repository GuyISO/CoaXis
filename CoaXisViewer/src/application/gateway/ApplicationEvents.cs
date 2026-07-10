using Godot;

/// <summary>
/// Application 経由で MeasurementEventHub モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationMeasurementEventHub
{
    public MeasurementEventHub EventHub => Application.Instance.MeasurementEventHub;

    public void RequestNotifyMeasurementResult() => EventHub.RequestNotifyMeasurementResult();
    public void RequestPickPoint(int pointIndex) => EventHub.RequestPickPoint(pointIndex);
    public void NotifyMeasurementResult(MeasurementResult result) => EventHub.NotifyMeasurementResult(result);
}

/// <summary>
/// Application 経由で ModelEventHub モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationModelEventHub
{
    public ModelEventHub EventHub => Application.Instance.ModelEventHub;

    public void RequestNotifyRootModel() => EventHub.RequestNotifyRootModel();
    public void RequestSetMultiSelectionMode(bool enable) => EventHub.RequestSetMultiSelectionMode(enable);
    public void RequestClearSelection() => EventHub.RequestClearSelection();
    public void RequestToggleModelVisibility(AnyModel model) => EventHub.RequestToggleModelVisibility(model);
    public void RequestAddModel(AnyModel childModel, AnyModel parentModel = null) => EventHub.RequestAddModel(childModel, parentModel);
    public void RequestLoadModel(string path) => EventHub.RequestLoadModel(path);

    public void NotifyRootModel(RootModel rootModel) => EventHub.NotifyRootModel(rootModel);
    public void NotifyModelSelectionState(AnyModel model, bool isSelected) => EventHub.NotifyModelSelectionState(model, isSelected);
    public void NotifyModelVisibilityState(AnyModel model, bool isVisible) => EventHub.NotifyModelVisibilityState(model, isVisible);
}

/// <summary>
/// Application 経由で PickEventHub モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationPickEventHub
{
    public PickEventHub EventHub => Application.Instance.PickEventHub;

    public void RequestNotifyPickHandlingMode() => EventHub.RequestNotifyPickHandlingMode();

    public void NotifyPickHandlingMode(PickHandlingMode mode) => EventHub.NotifyPickHandlingMode(mode);
    public void NotifyPickResult(PickResult pickResult) => EventHub.NotifyPickResult(pickResult);
    public void NotifyPickResults(PickResult[] pickResults) => EventHub.NotifyPickResults(pickResults);
}

/// <summary>
/// Application 経由で ViewportEventHub モジュールへアクセスするためのゲートウェイ
/// </summary>
public sealed class ApplicationViewportEventHub
{
    public ViewportEventHub EventHub => Application.Instance.ViewportEventHub;

    public void RequestNotifyState() => EventHub.RequestNotifyState();
    public void RequestMovePositionTo(Vector3 position, bool useTween = false) => EventHub.RequestMovePositionTo(position, useTween);
    public void RequestMoveRotationTo(Quaternion rotation, bool useTween = false) => EventHub.RequestMoveRotationTo(rotation, useTween);
    public void RequestSetDistance(float distance, bool useTween = false) => EventHub.RequestSetDistance(distance, useTween);
    public void RequestSetSizeTo(float size, bool useTween = false) => EventHub.RequestSetSizeTo(size, useTween);
    public void RequestSetFov(float fov, bool useTween = false) => EventHub.RequestSetFov(fov, useTween);
    public void RequestTranslate(Vector3 translation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => EventHub.RequestTranslate(translation, spaceMode, useTween);
    public void RequestRotate(Quaternion rotation, SpaceMode spaceMode = SpaceMode.World, bool useTween = false) => EventHub.RequestRotate(rotation, spaceMode, useTween);
    public void RequestZoom(float exponent, bool useTween = false) => EventHub.RequestZoom(exponent, useTween);
    public void RequestSetProjectionType(Camera3D.ProjectionType type) => EventHub.RequestSetProjectionType(type);
    public void RequestToggleProjectionType() => EventHub.RequestToggleProjectionType();
    public void RequestFit(AnyModel[] targetModels, bool useTween = false) => EventHub.RequestFit(targetModels, useTween);
    public void RequestAlignNormalTo(Vector3 normal, bool useTween = false) => EventHub.RequestAlignNormalTo(normal, useTween);
    public void RequestDecidePickRect(Vector2 startPosition, Vector2 endPosition) => EventHub.RequestDecidePickRect(startPosition, endPosition);

    public void NotifyInteractionMode(ViewportInteractionMode mode) => EventHub.NotifyInteractionMode(mode);
    public void NotifyPosition(Vector3 position) => EventHub.NotifyPosition(position);
    public void NotifyRotation(Quaternion rotation) => EventHub.NotifyRotation(rotation);
    public void NotifyDistance(float distance) => EventHub.NotifyDistance(distance);
    public void NotifySize(float size) => EventHub.NotifySize(size);
    public void NotifyFov(float fov) => EventHub.NotifyFov(fov);
    public void NotifyProjectionType(Camera3D.ProjectionType type) => EventHub.NotifyProjectionType(type);
    public void NotifyArcballRadius(float radius) => EventHub.NotifyArcballRadius(radius);
    public void NotifyArcballHandle(Vector3 position) => EventHub.NotifyArcballHandle(position);
    public void NotifyPickRect(Vector2 startPosition, Vector2 endPosition) => EventHub.NotifyPickRect(startPosition, endPosition);
    public void NotifyPickResult(PickResult pickResult) => EventHub.NotifyPickResult(pickResult);
}