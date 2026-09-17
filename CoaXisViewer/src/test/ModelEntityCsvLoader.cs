using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public static class ModelEntityCsvLoader
{
    /// <summary>
    /// CSV ファイルからモデル情報を読み込み、ModelEntityDto のリストとして返す
    /// </summary>
    /// <param name="path">CSV ファイルのパス</param>
    /// <returns>ModelEntityDto のリスト</returns>
    public static List<ModelEntityDto> Load(string path)
    {
        var models = new List<ModelEntityDto>();

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open CSV: {path}");
            return models;
        }

        bool isHeader = true;

        while (!file.EofReached())
        {
            var line = file.GetLine();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // ヘッダー行はスキップ
            if (isHeader)
            {
                isHeader = false;
                continue;
            }

            var cols = ParseCsvLine(line);

            // CSV の列順に合わせて DTO を作成
            // 列順: Id,ParentId,Type,Name,PositionX/Y/Z,RotationX/Y/Z/W,Visibility,IsCollapsed,IconPath,ScenePath,AlignToAabbCenter
            var dto = new ModelEntityDto
            {
                Id = Guid.Parse(cols[0]),
                ParentId = string.IsNullOrWhiteSpace(cols[1]) ? null : Guid.Parse(cols[1]),
                Type = cols[2],
                Name = cols[3],
                Position = new float[]
                {
                    float.Parse(cols[4]),
                    float.Parse(cols[5]),
                    float.Parse(cols[6])
                },
                Rotation = new float[]
                {
                    float.Parse(cols[7]),
                    float.Parse(cols[8]),
                    float.Parse(cols[9]),
                    float.Parse(cols[10])
                },
                Visibility = cols[11],
                IsCollapsed = bool.Parse(cols[12]),
                IconPath = cols[13],
                ScenePath = cols[14],
                AlignToAabbCenter = bool.Parse(cols[15])
            };

            models.Add(dto);
        }

        return models;
    }

    private static string[] ParseCsvLine(string line)
    {
        var columns = new List<string>();
        var value = new StringBuilder();
        bool inQuotes = false;

        // 引用符内のカンマは値の一部として扱い、二重引用符は CSV のエスケープ規則に従って復元する。
        for (int index = 0; index < line.Length; index++)
        {
            char character = line[index];

            if (character == '"')
            {
                if (inQuotes && index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (character == ',' && !inQuotes)
            {
                columns.Add(value.ToString());
                value.Clear();
                continue;
            }

            value.Append(character);
        }

        columns.Add(value.ToString());
        return columns.ToArray();
    }
}
