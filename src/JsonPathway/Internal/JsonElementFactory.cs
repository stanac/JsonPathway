using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class JsonElementFactory
    {
        private static readonly JsonElement _null = JsonDocument.Parse("null").RootElement;
        private static readonly JsonElement _true = JsonDocument.Parse("true").RootElement;
        private static readonly JsonElement _false = JsonDocument.Parse("false").RootElement;

        public static JsonElement CreateNumber(int number)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(number)).RootElement;
        }

        public static JsonElement CreateNumber(double number)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(number)).RootElement;
        }

        public static JsonElement CreateNull() => _null;

        public static JsonElement CreateArray(IEnumerable<JsonElement> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return JsonDocument.Parse(JsonSerializer.Serialize(elements)).RootElement;
        }

        public static JsonElement CreateArray(List<object> elements)
        {
            if (elements == null) throw new ArgumentNullException(nameof(elements));

            return JsonDocument.Parse(JsonSerializer.Serialize(elements)).RootElement;
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

            return JsonDocument.Parse(JsonSerializer.Serialize(s)).RootElement;
        }
    }
}
