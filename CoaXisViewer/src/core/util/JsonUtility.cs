using Godot;
using System;
using System.Globalization;

/// <summary>
/// JSON データの解析を補助するユーティリティ
/// </summary>
public static class JsonUtility
{
    #region Public Methods

    /// <summary>
    /// 指定されたキーに対応する Vector3 を取得する
    /// </summary>
    /// <param name="dictionary">対象の辞書</param>
    /// <param name="key">取得するキー</param>
    /// <param name="value">取得した Vector3</param>
    /// <returns>取得に成功した場合は true、それ以外は false</returns>
    public static bool TryGetVector3(Godot.Collections.Dictionary dictionary, string key, out Vector3 value)
    {
        value = Vector3.Zero;
        if (!dictionary.ContainsKey(key))
        {
            return false;
        }

        Godot.Collections.Array array;
        try
        {
            array = (Godot.Collections.Array)dictionary[key];
        }
        catch (InvalidCastException)
        {
            return false;
        }
        catch (NullReferenceException)
        {
            return false;
        }

        if (array.Count != 3)
        {
            return false;
        }

        if (!TryConvertToFloat(array[0], out float x)
            || !TryConvertToFloat(array[1], out float y)
            || !TryConvertToFloat(array[2], out float z))
        {
            return false;
        }

        value = new Vector3(x, y, z);
        return true;
    }

    /// <summary>
    /// 指定されたキーに対応する float を取得する
    /// </summary>
    /// <param name="dictionary">対象の辞書</param>
    /// <param name="key">取得するキー</param>
    /// <param name="value">取得した float</param>
    /// <returns>取得に成功した場合は true、それ以外は false</returns>
    public static bool TryGetFloat(Godot.Collections.Dictionary dictionary, string key, out float value)
    {
        value = 0.0f;
        if (!dictionary.ContainsKey(key))
        {
            return false;
        }

        return TryConvertToFloat(dictionary[key], out value);
    }

    /// <summary>
    /// 指定されたキーに対応する int を取得する
    /// </summary>
    /// <param name="dictionary">対象の辞書</param>
    /// <param name="key">取得するキー</param>
    /// <param name="value">取得した int</param>
    /// <returns>取得に成功した場合は true、それ以外は false</returns>
    public static bool TryGetInt(Godot.Collections.Dictionary dictionary, string key, out int value)
    {
        value = 0;
        if (!dictionary.ContainsKey(key))
        {
            return false;
        }

        return TryConvertToInt(dictionary[key], out value);
    }

    /// <summary>
    /// 指定された Variant を float に変換する
    /// </summary>
    /// <param name="value">変換する Variant</param>
    /// <param name="converted">変換後の float</param>
    /// <returns>変換に成功した場合は true、それ以外は false</returns>
    public static bool TryConvertToFloat(object value, out float converted)
    {
        switch (value)
        {
            case Variant variantValue:
                return TryConvertVariantToFloat(variantValue, out converted);
            case float floatValue:
                converted = floatValue;
                return true;
            case double doubleValue:
                converted = (float)doubleValue;
                return true;
            case int intValue:
                converted = intValue;
                return true;
            case long longValue:
                converted = longValue;
                return true;
            case string stringValue:
                return float.TryParse(stringValue, NumberStyles.Float, CultureInfo.InvariantCulture, out converted);
        }

        try
        {
            converted = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (InvalidCastException)
        {
            converted = 0.0f;
            return false;
        }
        catch (FormatException)
        {
            converted = 0.0f;
            return false;
        }
        catch (OverflowException)
        {
            converted = 0.0f;
            return false;
        }
    }

    /// <summary>
    /// 指定されたオブジェクトを int に変換する
    /// </summary>
    /// <param name="value">変換するオブジェクト</param>
    /// <param name="converted">変換後の int</param>
    /// <returns>変換に成功した場合は true、それ以外は false</returns>
    public static bool TryConvertToInt(object value, out int converted)
    {
        switch (value)
        {
            case Variant variantValue:
                return TryConvertVariantToInt(variantValue, out converted);
            case int intValue:
                converted = intValue;
                return true;
            case long longValue when longValue <= int.MaxValue && longValue >= int.MinValue:
                converted = (int)longValue;
                return true;
            case float floatValue:
                converted = (int)floatValue;
                return true;
            case double doubleValue:
                converted = (int)doubleValue;
                return true;
            case string stringValue:
                return int.TryParse(stringValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out converted);
        }

        try
        {
            converted = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return true;
        }
        catch (InvalidCastException)
        {
            converted = 0;
            return false;
        }
        catch (FormatException)
        {
            converted = 0;
            return false;
        }
        catch (OverflowException)
        {
            converted = 0;
            return false;
        }
    }

    /// <summary>
    /// 指定された Variant を float に変換する
    /// </summary>
    /// <param name="value">変換する Variant</param>
    /// <param name="converted">変換後の float</param>
    /// <returns>変換に成功した場合は true、それ以外は false</returns>
    public static bool TryConvertVariantToFloat(Variant value, out float converted)
    {
        try
        {
            converted = (float)value;
            return true;
        }
        catch (InvalidCastException)
        {
            return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out converted);
        }
    }

    /// <summary>
    /// 指定された Variant を int に変換する
    /// </summary>
    /// <param name="value">変換する Variant</param>
    /// <param name="converted">変換後の int</param>
    /// <returns>変換に成功した場合は true、それ以外は false</returns>
    public static bool TryConvertVariantToInt(Variant value, out int converted)
    {
        try
        {
            converted = (int)value;
            return true;
        }
        catch (InvalidCastException)
        {
            return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out converted);
        }
    }

    #endregion
}