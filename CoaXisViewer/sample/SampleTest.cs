using Godot;
using System;
using System.Collections.Generic;

public static class SampleTest
{
    public static void Run()
    {
        GD.Print("SampleTest: Run");

        // モデルの追加をテスト
        var modelId = Guid.NewGuid();
        var parentModelId = Guid.Empty; // ルートに追加
        Application.Model.Event.AddModel(modelId, parentModelId);

    }
}
