using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class PropertyAccessor
    {
        public static IEnumerable<JsonElement> GetPropertyValueFromChain(JsonElement element, List<string> propertyChain)
        {
            if (propertyChain is null || propertyChain.Count == 0)
                throw new System.ArgumentException(nameof(propertyChain) + " is empty or null");

            JsonElement? result = element;

            foreach (string p in propertyChain)
            {
                if (result.HasValue)
                {
                    result = GetPropertyValue(result.Value, p);
                }
            }

            if (result.HasValue)
            {
                yield return result.Value;
            }
        }

        public static IEnumerable<JsonElement> GetRecursiveProperties(JsonElement element)
        {
            List<JsonElement> result = new List<JsonElement>();

            GetRecursivePropertiesInner(element, result);

            return result;
        }

        public static IEnumerable<JsonElement> GetChildPropertyValues(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonProperty p in element.EnumerateObject())
                {
                    yield return p.Value;
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement e in element.EnumerateArray())
                {
                    yield return e;
                }
            }
        }

        private static void GetRecursivePropertiesInner(JsonElement element, List<JsonElement> elements)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                elements.Add(element);

                foreach (JsonProperty c in element.EnumerateObject())
                {
                    GetRecursivePropertiesInner(c.Value, elements);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                elements.Add(element);

                foreach (JsonElement c in element.EnumerateArray())
                {
                    GetRecursivePropertiesInner(c, elements);
                }
            }
        }

        private static JsonElement? GetPropertyValue(JsonElement element, string propertyName)
        {
            if (propertyName == "length")
            {
                if (element.ValueKind == JsonValueKind.String) return JsonElementFactory.CreateNumber(element.GetString().Length);
                if (element.ValueKind == JsonValueKind.Array) return JsonElementFactory.CreateNumber(element.GetArrayLength());
            }

            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement result))
            {
                return result;
            }

            return null;
        }
    }
}
