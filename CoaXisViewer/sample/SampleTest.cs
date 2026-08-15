using Godot;
using System;
using System.Collections.Generic;

public static class SampleTest
{
    public static void Run()
    {
        GD.Print("SampleTest: Run");

        Application.Model.Service.Clear();

        string jsonPath = "res://sample/modeldata.json";
        List<ModelDto> models = ModelJsonLoader.Load(jsonPath);

        if (models.Count == 0)
        {
            GD.Print("SampleTest: no DTOs were loaded from JSON.");
            return;
        }

        foreach (ModelDto dto in models)
        {
            Application.Model.Factory.CreateFromDto(dto, dto.ParentId);
        }

        GD.Print($"SampleTest: created {models.Count} models from JSON.");
    }
}
