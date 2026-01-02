using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal;

internal static class JsonElementFactory
{
    private static readonly JsonElement _null = JsonDocument.Parse("null").RootElement.Clone();
    private static readonly JsonElement _true = JsonDocument.Parse("true").RootElement.Clone();
    private static readonly JsonElement _false = JsonDocument.Parse("false").RootElement.Clone();

    public static JsonElement CreateNumber(int number)
    {
        return JsonDocument.Parse(JsonSerializer.Serialize(number)).RootElement.Clone();
    }

    public static JsonElement CreateNumber(double number)
    {
        JsonDocument num = JsonDocument.Parse(JsonSerializer.Serialize(number));
        return num.RootElement.Clone();
    }

    public static JsonElement CreateNull() => _null;

    public static JsonElement CreateArray(IEnumerable<JsonElement> elements)
    {
        if (elements == null) throw new ArgumentNullException(nameof(elements));

        return JsonDocument.Parse(JsonSerializer.Serialize(elements)).RootElement.Clone();
    }

    public static JsonElement CreateArray(List<object> elements)
    {
        if (elements == null) throw new ArgumentNullException(nameof(elements));

        return JsonDocument.Parse(JsonSerializer.Serialize(elements)).RootElement.Clone();
    }

    public static JsonElement CreateBool(bool b)
    {
        return b
            ? _true
            : _false;
    }

    public static JsonElement CreateString(string s)
    {
        if (s is null) throw new ArgumentNullException(nameof(s));

        return JsonDocument.Parse(JsonSerializer.Serialize(s)).RootElement.Clone();
    }
}