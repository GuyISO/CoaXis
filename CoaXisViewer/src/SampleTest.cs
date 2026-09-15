using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;

public static class SampleTest
{
    public static void RunLoadCsv()
    {
        GD.Print("SampleTest: RunLoadCsv");

        Application.Model.Service.Clear();

        // 意図: サンプルデータはGodotプロジェクト外のsamplesへ移設済みのため、res://からの相対脱出で実パスへ解決する。
        string csvPath = ProjectSettings.GlobalizePath("res://../samples/modelentity.csv");
        List<ModelEntityDto> dtos = ModelEntityCsvLoader.Load(csvPath);

        if (dtos.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from CSV.");
            return;
        }

        Application.Model.EntityFactory.CreateEntities(dtos);

        GD.Print($"SampleTest: created {dtos.Count} models from CSV.");
    }

    public static void RunLoadJson()
    {
        GD.Print("SampleTest: RunLoadJson");

        Application.Model.Service.Clear();

        // 意図: サンプルデータはGodotプロジェクト外のsamplesへ移設済みのため、res://からの相対脱出で実パスへ解決する。
        string jsonPath = ProjectSettings.GlobalizePath("res://../samples/modelentity.json");
        List<ModelEntityDto> dtos = ModelEntityJsonLoader.Load(jsonPath);

        if (dtos.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from JSON.");
            return;
        }

        Application.Model.EntityFactory.CreateEntities(dtos);

        GD.Print($"SampleTest: created {dtos.Count} models from JSON.");
    }
}
