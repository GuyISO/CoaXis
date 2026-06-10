using Godot;
using System;
using System.Globalization;

public static class JsonUtility
{
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
		catch (Exception)
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

	public static bool TryGetFloat(Godot.Collections.Dictionary dictionary, string key, out float value)
	{
		value = 0.0f;
		if (!dictionary.ContainsKey(key))
		{
			return false;
		}

		return TryConvertToFloat(dictionary[key], out value);
	}

	public static bool TryGetInt(Godot.Collections.Dictionary dictionary, string key, out int value)
	{
		value = 0;
		if (!dictionary.ContainsKey(key))
		{
			return false;
		}

		return TryConvertToInt(dictionary[key], out value);
	}

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
		catch (Exception)
		{
			converted = 0.0f;
			return false;
		}
	}

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
		catch (Exception)
		{
			converted = 0;
			return false;
		}
	}

	public static bool TryConvertVariantToFloat(Variant value, out float converted)
	{
		try
		{
			converted = (float)value;
			return true;
		}
		catch (Exception)
		{
			return float.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out converted);
		}
	}

	public static bool TryConvertVariantToInt(Variant value, out int converted)
	{
		try
		{
			converted = (int)value;
			return true;
		}
		catch (Exception)
		{
			return int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out converted);
		}
	}
	
}