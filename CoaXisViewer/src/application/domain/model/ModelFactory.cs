using Godot;
using System;
using System.Threading.Tasks;

/// <summary>
/// ModelDto から ModelData と ModelNode を生成するファクトリ
/// </summary>
public partial class ModelFactory : Node
{
    #region Public API

    /// <summary>
    /// ModelDto から ModelData を生成し、Registry に登録してノードを生成する
    /// </summary>
    /// <param name="dto">生成元となる DTO</param>
    /// <param name="parentId">親モデルの ID。null または Guid.Empty の場合は Root 配下に追加する</param>
    /// <returns>生成された ModelData</returns>
    public ModelData CreateFromDto(ModelDto dto, Guid? parentId = null)
    {
        if (dto == null)
        {
            throw new ArgumentNullException(nameof(dto));
        }

        if (dto.Id == Guid.Empty)
        {
            throw new ArgumentException("ModelDto.Id must not be empty.", nameof(dto));
        }

        Guid resolvedParentId = parentId ?? dto.ParentId ?? Guid.Empty;

        var modelData = new ModelData(dto.Id, resolvedParentId, dto.Type, dto.Name)
        {
            Position = ConvertPosition(dto.Position),
            Rotation = ConvertRotation(dto.Rotation),
            IconPath = dto.IconFilePath,
            GlbPath = dto.GlbFilePath,
            WrlPath = dto.WrlFilePath
        };

        modelData.Status = ModelStatus.Initialized;

        Application.Model.Registry.Register(modelData);

        ModelNode node = EnsureNode(modelData);
        modelData.Node = node;

        Application.Model.Registry.ResolveHierarchy();
        if (string.IsNullOrWhiteSpace(modelData.GlbPath))
        {
            modelData.Status = ModelStatus.Loaded;
        }
        else
        {
            modelData.Status = ModelStatus.GlbLoading;
            _ = LoadGlbAsync(modelData);
        }

        Application.Model.Event.NotifyModelAdded(modelData.Id, modelData.ParentId);

        return modelData;
    }

    #endregion

    #region Internal Helpers

    private ModelNode EnsureNode(ModelData modelData)
    {
        if (modelData == null)
        {
            throw new ArgumentNullException(nameof(modelData));
        }

        if (modelData.Node != null && IsInstanceValid(modelData.Node))
        {
            return modelData.Node;
        }

        var node = new ModelNode(modelData.Id);
        modelData.Node = node;

        node.Position = modelData.Position;
        node.Quaternion = modelData.Rotation;

        ModelNode parentNode = ResolveParentNode(modelData.ParentId);
        if (parentNode != null)
        {
            parentNode.AddChild(node);
        }
        else
        {
            var rootNode = Application.Model.Service.Root?.Node;
            if (rootNode != null)
            {
                rootNode.AddChild(node);
            }
            else
            {
                Application.Log.Warn($"ModelFactory: parent node not found for modelId='{modelData.Id}'.");
            }
        }

        return node;
    }

    private ModelNode ResolveParentNode(Guid parentId)
    {
        if (parentId == Guid.Empty)
        {
            return null;
        }

        ModelData parentData = Application.Model.Registry.GetModelData(parentId);
        if (parentData == null)
        {
            return null;
        }

        if (parentData.Node != null && IsInstanceValid(parentData.Node))
        {
            return parentData.Node;
        }

        return EnsureNode(parentData);
    }

    private async Task LoadGlbAsync(ModelData modelData)
    {
        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            ModelNode modelNode = modelData.Node;
            if (modelNode == null || !IsInstanceValid(modelNode))
            {
                return;
            }

            if (await ModelLoadUtility.LoadModelAsync(modelNode, modelData.GlbPath))
            {
                modelData.Status = ModelStatus.GlbLoaded;
                modelData.Status = ModelStatus.Loaded;
            }
            else
            {
                modelData.Status = ModelStatus.LoadFailed;
            }
        }
        catch (Exception exception)
        {
            modelData.Status = ModelStatus.LoadFailed;
            Application.Log.Error($"ModelFactory: failed to load glb for modelId='{modelData.Id}', path='{modelData.GlbPath}'. {exception}");
        }
    }

    private static Vector3 ConvertPosition(float[] position)
    {
        if (position == null || position.Length != 3)
        {
            return Vector3.Zero;
        }

        Vector3 catiaVector = new Vector3(position[0], position[1], position[2]);
        return CoordinateSystemUtility.CatiaToGodot(catiaVector);
    }

    private static Quaternion ConvertRotation(float[] rotation)
    {
        if (rotation == null || rotation.Length != 4)
        {
            return Quaternion.Identity;
        }

        Quaternion catiaQuaternion = new Quaternion(rotation[0], rotation[1], rotation[2], rotation[3]);
        Basis catiaBasis = new Basis(catiaQuaternion);
        Basis godotBasis = CoordinateSystemUtility.CatiaToGodot(catiaBasis);
        return godotBasis.GetRotationQuaternion();
    }

    #endregion
}
