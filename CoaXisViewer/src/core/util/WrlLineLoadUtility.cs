using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Utility for loading line primitives from VRML(.wrl) files.
/// </summary>
public static class WrlLineLoadUtility
{
    private const float TubeRadiusMeters = 0.010f;
    private const float JointRadiusMeters = 0.010f;
    private const float MinSegmentLengthMeters = 0.0001f;
    private const int TubeRadialSegments = 12;

    #region Public Methods

    /// <summary>
    /// Load WRL line data and attach it under ModelComponents.Line.
    /// </summary>
    /// <param name="modelNode">Target model node.</param>
    /// <param name="path">WRL file path.</param>
    /// <returns>True when line data was loaded and attached.</returns>
    public static bool LoadLines(ModelNode modelNode, string path)
    {
        if (modelNode == null)
        {
            throw new ArgumentNullException(nameof(modelNode));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            Application.Log.Warn($"WrlLineLoadUtility: empty wrl path. model='{modelNode.Name}'");
            return false;
        }

        ModelComponents components = modelNode.Components;
        if (components == null)
        {
            Application.Log.Error($"WrlLineLoadUtility: components are not initialized. model='{modelNode.Name}'");
            return false;
        }

        string content = ReadWrlText(path);
        if (string.IsNullOrWhiteSpace(content))
        {
            Application.Log.Error($"WrlLineLoadUtility: failed to read wrl file. path='{path}'");
            return false;
        }

        if (!TryParse(content, out List<Vector3> points, out List<int> coordIndex))
        {
            Application.Log.Error($"WrlLineLoadUtility: failed to parse line data. path='{path}'");
            return false;
        }

        int segmentCount = AttachLineMesh(components, points, coordIndex);
        if (segmentCount <= 0)
        {
            Application.Log.Warn($"WrlLineLoadUtility: no line segment found. path='{path}'");
            return false;
        }

        Application.Log.Info($"WrlLineLoadUtility: loaded {segmentCount} segments from '{path}'.");
        return true;
    }

    #endregion

    #region Parse Helpers

    private static bool TryParse(string content, out List<Vector3> points, out List<int> coordIndex)
    {
        points = new List<Vector3>();
        coordIndex = new List<int>();

        string pointBlock = ExtractArrayBlock(content, "point");
        string coordIndexBlock = ExtractArrayBlock(content, "coordIndex");

        if (string.IsNullOrWhiteSpace(pointBlock) || string.IsNullOrWhiteSpace(coordIndexBlock))
        {
            return false;
        }

        pointBlock = StripInlineComments(pointBlock);
        coordIndexBlock = StripInlineComments(coordIndexBlock);

        MatchCollection pointMatches = Regex.Matches(
            pointBlock,
            @"[-+]?(?:\d+\.\d+|\d+|\.\d+)(?:[eE][-+]?\d+)?",
            RegexOptions.CultureInvariant);

        if (pointMatches.Count < 3)
        {
            return false;
        }

        for (int i = 0; i + 2 < pointMatches.Count; i += 3)
        {
            if (!float.TryParse(pointMatches[i].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                || !float.TryParse(pointMatches[i + 1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y)
                || !float.TryParse(pointMatches[i + 2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
            {
                return false;
            }

            Vector3 catiaPoint = new Vector3(x, y, z);
            points.Add(CoordinateSystemUtility.CatiaToGodot(catiaPoint));
        }

        MatchCollection indexMatches = Regex.Matches(coordIndexBlock, @"-?\d+");
        if (indexMatches.Count == 0)
        {
            return false;
        }

        foreach (Match match in indexMatches)
        {
            if (!int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                return false;
            }

            coordIndex.Add(index);
        }

        return points.Count > 0 && coordIndex.Count > 0;
    }

    private static string ExtractArrayBlock(string content, string key)
    {
        Match match = Regex.Match(
            content,
            key + @"\s*\[(.*?)\]",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private static string StripInlineComments(string source)
    {
        return Regex.Replace(source, @"#.*$", string.Empty, RegexOptions.Multiline);
    }

    #endregion

    #region Scene Helpers

    private static int AttachLineMesh(ModelComponents components, List<Vector3> points, List<int> coordIndex)
    {
        ClearLineNodes(components.Line);

        var lineRoot = new Node3D { Name = "WrlLine" };
        components.Line.AddChild(lineRoot);

        BaseMaterial3D material = CreateLineMaterial();
        var cylinderMesh = new CylinderMesh
        {
            TopRadius = TubeRadiusMeters,
            BottomRadius = TubeRadiusMeters,
            Height = 1.0f,
            RadialSegments = TubeRadialSegments,
            Material = material
        };

        var jointMesh = new SphereMesh
        {
            Radius = JointRadiusMeters,
            Height = JointRadiusMeters * 2.0f,
            RadialSegments = TubeRadialSegments,
            Rings = 8,
            Material = material
        };

        int segmentCount = 0;
        int previousIndex = -1;
        var jointIndices = new HashSet<int>();

        foreach (int index in coordIndex)
        {
            if (index == -1)
            {
                previousIndex = -1;
                continue;
            }

            if (index < 0 || index >= points.Count)
            {
                previousIndex = -1;
                continue;
            }

            if (previousIndex >= 0 && previousIndex < points.Count)
            {
                if (AddTubeSegment(lineRoot, cylinderMesh, points[previousIndex], points[index]))
                {
                    jointIndices.Add(previousIndex);
                    jointIndices.Add(index);
                    segmentCount++;
                }
            }

            previousIndex = index;
        }

        if (segmentCount <= 0)
        {
            lineRoot.QueueFree();
            return 0;
        }

        foreach (int jointIndex in jointIndices)
        {
            AddJointSphere(lineRoot, jointMesh, points[jointIndex]);
        }

        return segmentCount;
    }

    private static bool AddTubeSegment(Node3D parent, Mesh tubeMesh, Vector3 start, Vector3 end)
    {
        Vector3 delta = end - start;
        float length = delta.Length();
        if (length < MinSegmentLengthMeters)
        {
            return false;
        }

        Vector3 direction = delta / length;
        var segment = new MeshInstance3D
        {
            Name = "WrlTube",
            Mesh = tubeMesh,
            Position = (start + end) * 0.5f,
            Quaternion = CreateRotationFromUp(direction),
            Scale = new Vector3(1.0f, length, 1.0f)
        };

        parent.AddChild(segment);
        return true;
    }

    private static void AddJointSphere(Node3D parent, Mesh sphereMesh, Vector3 position)
    {
        var joint = new MeshInstance3D
        {
            Name = "WrlJoint",
            Mesh = sphereMesh,
            Position = position
        };

        parent.AddChild(joint);
    }

    private static Quaternion CreateRotationFromUp(Vector3 direction)
    {
        Vector3 from = Vector3.Up;
        float dot = Mathf.Clamp(from.Dot(direction), -1.0f, 1.0f);

        if (dot > 0.9999f)
        {
            return Quaternion.Identity;
        }

        if (dot < -0.9999f)
        {
            return new Quaternion(Vector3.Right, Mathf.Pi);
        }

        Vector3 axis = from.Cross(direction).Normalized();
        float angle = Mathf.Acos(dot);
        return new Quaternion(axis, angle);
    }

    private static BaseMaterial3D CreateLineMaterial()
    {
        Color lineColor = Color.FromHtml(Application.Setting.Service.Current.Color.MeasurementLineColor);
        var material = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
            NoDepthTest = false,
            AlbedoColor = lineColor,
            EmissionEnabled = true,
            Emission = lineColor
        };

        return material;
    }

    private static void ClearLineNodes(Node lineRoot)
    {
        foreach (Node child in lineRoot.GetChildren())
        {
            child.QueueFree();
        }
    }

    #endregion

    #region IO Helpers

    private static string ReadWrlText(string path)
    {
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
        {
            using Godot.FileAccess file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
            return file == null ? string.Empty : file.GetAsText();
        }

        string absolutePath = Path.IsPathRooted(path)
            ? path
            : ProjectSettings.GlobalizePath(path);

        if (!File.Exists(absolutePath))
        {
            return string.Empty;
        }

        return File.ReadAllText(absolutePath);
    }

    #endregion
}
