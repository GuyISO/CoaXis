// TODO: リファクタリング確認後に削除

using Godot;

/// <summary>
/// ViewportCameraHubで利用する副作用のないカメラ計算を提供する。
/// </summary>
internal static class ViewportCameraUtility
{
    /// <summary>
    /// 回転を適用したワールド基準位置を計算する。
    /// </summary>
    internal static bool TryTranslate(
        Vector3 position,
        Quaternion rotation,
        Vector3 translation,
        SpaceMode spaceMode,
        out Vector3 targetPosition)
    {
        targetPosition = spaceMode switch
        {
            SpaceMode.World => position + translation,
            SpaceMode.FocalPoint or SpaceMode.Camera => position + new Basis(rotation) * translation,
            _ => default
        };
        return spaceMode is SpaceMode.World or SpaceMode.FocalPoint or SpaceMode.Camera;
    }

    /// <summary>
    /// 指定座標系での回転後姿勢と、カメラ位置を維持するための注視点位置を計算する。
    /// </summary>
    internal static bool TryRotate(
        Vector3 position,
        Quaternion currentRotation,
        Quaternion rotation,
        SpaceMode spaceMode,
        float cameraDistance,
        out Quaternion targetRotation,
        out Vector3 targetPosition)
    {
        targetRotation = currentRotation;
        targetPosition = position;
        switch (spaceMode)
        {
            case SpaceMode.World:
                targetRotation = rotation * currentRotation;
                return true;
            case SpaceMode.FocalPoint:
                targetRotation = currentRotation * rotation;
                return true;
            case SpaceMode.Camera:
                targetRotation = currentRotation * rotation;
                Vector3 distance = new Vector3(0, 0, cameraDistance);
                Vector3 cameraPosition = position + currentRotation * distance;
                targetPosition = cameraPosition - targetRotation * distance;
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// ズーム操作後の距離と正投影サイズを計算する。
    /// </summary>
    internal static (float Distance, float Size) Zoom(
        float exponent,
        float zoomBase,
        float minZoomValue,
        Camera3D.ProjectionType projectionType,
        float currentDistance,
        float currentSize)
    {
        float scale = Mathf.Pow(zoomBase, exponent);
        return projectionType == Camera3D.ProjectionType.Orthogonal
            ? (currentDistance, Mathf.Max(currentSize * scale, minZoomValue))
            : (Mathf.Max(currentDistance * scale, minZoomValue), currentSize);
    }

    /// <summary>
    /// 透視投影距離を正投影サイズから計算する。
    /// </summary>
    internal static float GetPerspectiveDistance(float orthographicSize, float fov)
    {
        return orthographicSize / CalculateSizeAtZ1(fov);
    }

    /// <summary>
    /// 正投影サイズを透視投影距離から計算する。
    /// </summary>
    internal static float GetOrthographicSize(float perspectiveDistance, float fov)
    {
        return Mathf.Abs(perspectiveDistance) * CalculateSizeAtZ1(fov);
    }

    /// <summary>
    /// 対象AABBを画角内に収める距離またはサイズを計算する。
    /// </summary>
    internal static float CalculateFitValue(
        Aabb worldAabb,
        Quaternion cameraRotation,
        Camera3D.ProjectionType projectionType,
        float fov,
        float aspect,
        float near,
        float fitPadding,
        float minZoomValue)
    {
        Vector3 center = worldAabb.Position + worldAabb.Size * 0.5f;
        Basis inverseBasis = new Basis(cameraRotation).Inverse();
        if (projectionType == Camera3D.ProjectionType.Perspective)
        {
            float halfVerticalFov = Mathf.DegToRad(fov) * 0.5f;
            float tanHalfY = Mathf.Max(Mathf.Tan(halfVerticalFov), 1e-5f);
            float tanHalfX = Mathf.Max(tanHalfY * aspect, 1e-5f);
            float maxZ = float.NegativeInfinity;
            float requiredDistance = 0f;

            foreach (Vector3 corner in WorldAabbUtility.GetAabbCorners(worldAabb))
            {
                Vector3 local = inverseBasis * (corner - center);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.X) / tanHalfX);
                requiredDistance = Mathf.Max(requiredDistance, local.Z + Mathf.Abs(local.Y) / tanHalfY);
                maxZ = Mathf.Max(maxZ, local.Z);
            }

            requiredDistance = Mathf.Max(requiredDistance, maxZ + near * 1.5f);
            return Mathf.Max(requiredDistance * fitPadding, minZoomValue);
        }

        float maxAbsX = 0f;
        float maxAbsY = 0f;
        foreach (Vector3 corner in WorldAabbUtility.GetAabbCorners(worldAabb))
        {
            Vector3 local = inverseBasis * (corner - center);
            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(local.X));
            maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(local.Y));
        }

        float requiredHeight = 2f * Mathf.Max(maxAbsY, maxAbsX / aspect);
        return Mathf.Max(requiredHeight * fitPadding, minZoomValue);
    }

    /// <summary>
    /// 法線へ向ける回転を計算し、入力が無効な場合は false を返す。
    /// </summary>
    internal static bool TryAlignNormal(Quaternion currentRotation, Vector3 normal, out Quaternion rotation)
    {
        rotation = currentRotation;
        if (normal.LengthSquared() < Mathf.Epsilon)
        {
            return false;
        }

        Basis currentBasis = new Basis(currentRotation);
        Vector3 targetBack = normal.Normalized();
        Vector3 currentUp = currentBasis.Y.Normalized();
        Vector3 projectedUp = currentUp - targetBack * currentUp.Dot(targetBack);
        if (projectedUp.LengthSquared() < Mathf.Epsilon)
        {
            Vector3 currentRight = currentBasis.X.Normalized();
            Vector3 projectedRight = currentRight - targetBack * currentRight.Dot(targetBack);
            if (projectedRight.LengthSquared() < Mathf.Epsilon)
            {
                projectedRight = Mathf.Abs(targetBack.Dot(Vector3.Up)) < 0.999f
                    ? Vector3.Up.Cross(targetBack)
                    : Vector3.Right.Cross(targetBack);
            }

            projectedUp = targetBack.Cross(projectedRight.Normalized());
        }

        Vector3 up = projectedUp.Normalized();
        Vector3 right = up.Cross(targetBack).Normalized();
        up = targetBack.Cross(right).Normalized();
        rotation = new Basis(right, up, targetBack).GetRotationQuaternion();
        return true;
    }

    private static float CalculateSizeAtZ1(float fov)
    {
        return Mathf.Tan(Mathf.DegToRad(fov) / 2f) * 2f;
    }
}
