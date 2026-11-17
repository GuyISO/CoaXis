using System;
using System.Collections.Generic;
using System.Linq;
using CoaXis.Protocol.Viewer;
using Godot;

/// <summary>EntityとPropertyの検索結果契約を検証する。</summary>
public partial class ModelSearchContract : Node
{
    /// <summary>検索結果の契約を検証する。</summary>
    public bool Run()
    {
        try
        {
            RunChecks();
            return true;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            return false;
        }
    }

    private void RunChecks()
    {
        Guid doorAId = Guid.NewGuid();
        Guid doorBId = Guid.NewGuid();
        Guid windowId = Guid.NewGuid();
        Guid materialAId = Guid.NewGuid();
        Guid gradeAId = Guid.NewGuid();
        Guid materialBId = Guid.NewGuid();
        Guid materialWindowId = Guid.NewGuid();
        Guid[] entityIds = { doorAId, doorBId, windowId };

        Application.Model.ModelSet.Add(
            new[]
            {
                new ModelEntityDto { Id = doorAId, Type = "Part", Name = "Door A" },
                new ModelEntityDto { Id = doorBId, Type = "Part", Name = "Door B" },
                new ModelEntityDto { Id = windowId, Type = "Part", Name = "Window" }
            },
            new[]
            {
                new ModelPropertyDto
                {
                    Id = materialAId, ParentId = doorAId, PropertyType = "Material",
                    ValueType = "string", Value = "Steel"
                },
                new ModelPropertyDto
                {
                    Id = gradeAId, ParentId = materialAId, PropertyType = "Grade",
                    ValueType = "string", Value = "A"
                },
                new ModelPropertyDto
                {
                    Id = materialBId, ParentId = doorBId, PropertyType = "Material",
                    ValueType = "string", Value = "Wood"
                },
                new ModelPropertyDto
                {
                    Id = materialWindowId, ParentId = windowId, PropertyType = "Material",
                    ValueType = "string", Value = "Steel"
                }
            });

        try
        {
            IReadOnlyList<Guid> entityResults = Application.Model.Search.Find(
                "Part", "Door", "Grade", "string", "a");
            IReadOnlyList<Guid> propertyResults = Application.Model.Search.FindProperties(
                "Part", "Door", "Grade", "string", "a");
            Check(entityResults.SequenceEqual(new[] { doorAId }), "検索条件に一致するEntity結果が不正");
            Check(propertyResults.SequenceEqual(new[] { gradeAId }), "階層下の一致Property結果が不正");

            IReadOnlyList<Guid> unmatchedPropertyResults = Application.Model.Search.FindProperties(
                "Part", "Door", "Material", "string", "a");
            Check(unmatchedPropertyResults.Count == 0, "異なるPropertyに分かれた条件を一致扱いしている");

            IReadOnlyList<Guid> entityFilteredProperties = Application.Model.Search.FindProperties(
                "Part", "Door", string.Empty, string.Empty, string.Empty);
            Check(entityFilteredProperties.SequenceEqual(new[] { materialAId, gradeAId, materialBId }),
                "Entity条件のみ指定したProperty結果が不正");
        }
        finally
        {
            Application.Model.ModelSet.Remove(entityIds, Array.Empty<Guid>());
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
