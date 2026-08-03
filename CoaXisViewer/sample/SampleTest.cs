using Godot;
using System;
using System.Collections.Generic;

public static class SampleTest
{
    public static void Run()
    {
        GD.Print("SampleTest: Run");

        string csvPath = "res://sample/modeldata.csv";
        List<ModelDto> models = ModelCsvLoader.Load(csvPath);

        if (models.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from CSV.");
            return;
        }

        foreach (ModelDto dto in models)
        {
            Application.Model.Factory.CreateFromDto(dto, dto.ParentId);
        }

        GD.Print($"SampleTest: created {models.Count} models from CSV.");
    }
}
