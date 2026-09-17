using CoaXis.Protocol.Viewer;
using Godot;
using System;
using System.Collections.Generic;
using System.Text;

public static class ModelPropertyCsvLoader
{
    /// <summary>
    /// CSV ファイルからモデルプロパティ情報を読み込み、ModelPropertyDto のリストとして返す
    /// </summary>
    /// <param name="path">CSV ファイルのパス</param>
    /// <returns>ModelPropertyDto のリスト</returns>
    public static List<ModelPropertyDto> Load(string path)
    {
        var properties = new List<ModelPropertyDto>();

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr($"Failed to open CSV: {path}");
            return properties;
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
            // 列順: Id,ParentId,PropertyType,ValueType,Value
            var dto = new ModelPropertyDto
            {
                Id = Guid.Parse(cols[0]),
                ParentId = string.IsNullOrWhiteSpace(cols[1]) ? null : Guid.Parse(cols[1]),
                PropertyType = cols[2],
                ValueType = cols[3],
                Value = cols[4]
            };

            properties.Add(dto);
        }

        return properties;
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