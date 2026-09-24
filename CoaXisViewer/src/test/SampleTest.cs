using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

public static class SampleTest
{
    public static void RunLoadCsv()
    {
        GD.Print("SampleTest: RunLoadCsv");

        Application.Model.LoadService.ClearModels();

        // 意図: サンプルデータはGodotプロジェクト外のsamplesへ移設済みのため、res://からの相対脱出で実パスへ解決する。
        string entityCsvPath = ProjectSettings.GlobalizePath("res://../samples/modelentity.csv");
        List<ModelEntityDto> entityDtos = ModelEntityCsvLoader.Load(entityCsvPath);

        if (entityDtos.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from CSV.");
            return;
        }

        Application.Model.EntityFactory.CreateEntities(entityDtos);

        GD.Print($"SampleTest: created {entityDtos.Count} models from CSV.");

        string propertyCsvPath = ProjectSettings.GlobalizePath("res://../samples/modelproperty.csv");
        List<ModelPropertyDto> propertyDtos = ModelPropertyCsvLoader.Load(propertyCsvPath);

        if (propertyDtos.Count == 0)
        {
            GD.Print("SampleTest: no property DTOs were loaded from CSV.");
            return;
        }

        Application.Model.PropertyFactory.CreateProperties(propertyDtos);

        GD.Print($"SampleTest: created {propertyDtos.Count} properties from CSV.");

    }

    public static void RunLoadJson()
    {
        GD.Print("SampleTest: RunLoadJson");

        Application.Model.LoadService.ClearModels();

        // 意図: サンプルデータはGodotプロジェクト外のsamplesへ移設済みのため、res://からの相対脱出で実パスへ解決する。
        string jsonPath = ProjectSettings.GlobalizePath("res://../samples/modelentity.json");
        List<ModelEntityDto> dtos = JsonDtoLoader.Load<ModelEntityDto>(jsonPath);

        if (dtos.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from JSON.");
            return;
        }

        Application.Model.EntityFactory.CreateEntities(dtos);

        GD.Print($"SampleTest: created {dtos.Count} models from JSON.");
    }
}
