using CoaXis.Protocol.Viewer;
using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace CoaXis.IpcTester;

/// <summary>
/// テスターで選択されたJSON/CSVファイルをViewer Protocol DTOへ変換する。
/// </summary>
public static class ModelFileLoader
{
    /// <summary>
    /// JSONまたはCSVからモデル実体一覧を読み込む。
    /// </summary>
    /// <param name="path">入力ファイルの絶対パス</param>
    /// <returns>読み込んだモデル実体</returns>
    public static List<ModelEntityDto> LoadEntities(string path)
    {
        string extension = Path.GetExtension(path);
        List<ModelEntityDto> entities = extension.Equals(".csv", StringComparison.OrdinalIgnoreCase)
            ? LoadEntityCsv(path)
            : extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
                ? JsonSerializer.Deserialize<List<ModelEntityDto>>(File.ReadAllText(path), IpcJsonOptions.Default)
                : throw new InvalidDataException("モデル実体は .csv または .json を指定してください。");

        if (entities == null || entities.Count == 0)
        {
            throw new InvalidDataException("モデル実体がありません。JSON配列またはCSVデータを確認してください。");
        }

        return entities;
    }

    /// <summary>
    /// CSVからモデル属性一覧を読み込む。未指定時は空の一覧を返す。
    /// </summary>
    /// <param name="path">入力ファイルの絶対パス。nullまたは空なら属性なし</param>
    /// <returns>読み込んだモデル属性</returns>
    public static List<ModelPropertyDto> LoadProperties(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new List<ModelPropertyDto>();
        }

        var properties = new List<ModelPropertyDto>();
        using var parser = CreateCsvParser(path);
        SkipHeader(parser);

        while (!parser.EndOfData)
        {
            string[] columns = parser.ReadFields();
            if (columns == null || columns.Length == 0)
            {
                continue;
            }

            RequireColumnCount(columns, 5, parser.LineNumber, path);
            properties.Add(new ModelPropertyDto
            {
                Id = Guid.Parse(columns[0]),
                ParentId = ParseOptionalGuid(columns[1]),
                PropertyType = columns[2],
                ValueType = columns[3],
                Value = columns[4]
            });
        }

        return properties;
    }

    private static List<ModelEntityDto> LoadEntityCsv(string path)
    {
        var entities = new List<ModelEntityDto>();
        using var parser = CreateCsvParser(path);
        SkipHeader(parser);

        while (!parser.EndOfData)
        {
            string[] columns = parser.ReadFields();
            if (columns == null || columns.Length == 0)
            {
                continue;
            }

            RequireColumnCount(columns, 16, parser.LineNumber, path);
            entities.Add(new ModelEntityDto
            {
                Id = Guid.Parse(columns[0]),
                ParentId = ParseOptionalGuid(columns[1]),
                Type = columns[2],
                Name = columns[3],
                Position = new[]
                {
                    ParseFloat(columns[4]), ParseFloat(columns[5]), ParseFloat(columns[6])
                },
                Rotation = new[]
                {
                    ParseFloat(columns[7]), ParseFloat(columns[8]), ParseFloat(columns[9]), ParseFloat(columns[10])
                },
                Visibility = columns[11],
                IsCollapsed = bool.Parse(columns[12]),
                IconPath = columns[13],
                ScenePath = columns[14],
                AlignToAabbCenter = bool.Parse(columns[15])
            });
        }

        return entities;
    }

    private static TextFieldParser CreateCsvParser(string path)
    {
        var parser = new TextFieldParser(path);
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;
        parser.TrimWhiteSpace = false;
        return parser;
    }

    private static void SkipHeader(TextFieldParser parser)
    {
        if (!parser.EndOfData)
        {
            parser.ReadFields();
        }
    }

    private static Guid? ParseOptionalGuid(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : Guid.Parse(value);
    }

    private static float ParseFloat(string value)
    {
        return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private static void RequireColumnCount(string[] columns, int expected, long lineNumber, string path)
    {
        if (columns.Length != expected)
        {
            throw new InvalidDataException($"{Path.GetFileName(path)} の{lineNumber}行目は{expected}列必要ですが、{columns.Length}列です。");
        }
    }
}