using System;
using System.Globalization;
using System.Reflection;

namespace Mcl.Core.Dotnetdetour.Models.Config;

/// <summary>A stable storage key mapped to a refactor-safe runtime field.</summary>
public sealed class ConfigEntry
{
    private readonly FieldInfo _field;

    public ConfigEntry(string key, string fieldName, string description, string category,
        int? minimum = null, int? maximum = null, string helpText = null)
    {
        Key = key;
        FieldName = fieldName;
        Description = description;
        Category = category;
        Minimum = minimum;
        Maximum = maximum;
        HelpText = helpText;
        _field = typeof(WpfConfig).GetField(fieldName, BindingFlags.Public | BindingFlags.Static)
                 ?? throw new ArgumentException("Unknown configuration field: " + fieldName, nameof(fieldName));
        if (FieldType != typeof(bool) && FieldType != typeof(int) && FieldType != typeof(string))
            throw new ArgumentException("Unsupported configuration field: " + fieldName, nameof(fieldName));
    }

    public string Key { get; }
    public string FieldName { get; }
    public string Description { get; }
    public string Category { get; }
    public Type FieldType => _field.FieldType;
    public int? Minimum { get; }
    public int? Maximum { get; }
    public string HelpText { get; }
    public object GetValue() => _field.GetValue(null);
    internal void SetValue(object value) => _field.SetValue(null, value);

    public object ConvertValue(object value)
    {
        if (value == null) throw new ArgumentException(Description + "：值不能为空。");
        object converted;
        if (FieldType == typeof(string))
        {
            if (!(value is string)) throw new ArgumentException(Description + "：请输入文本。");
            converted = value;
        }
        else if (FieldType == typeof(bool))
        {
            if (value is bool) return value;
            if (!(value is string text) || !bool.TryParse(text, out var boolean))
                throw new ArgumentException(Description + "：请输入 true 或 false。");
            return boolean;
        }
        else
        {
            if (!int.TryParse(Convert.ToString(value, CultureInfo.InvariantCulture),
                    NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                throw new ArgumentException(Description + "：请输入有效整数。");
            if ((Minimum.HasValue && number < Minimum.Value) || (Maximum.HasValue && number > Maximum.Value))
                throw new ArgumentException($"{Description}：范围为 {Minimum}–{Maximum}。");
            converted = number;
        }

        return converted;
    }
}