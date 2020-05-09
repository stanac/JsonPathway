using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal
{
    public class JsonElementComparer : IComparer<JsonElement>
    {
        public static JsonElementComparer Default { get; } = new JsonElementComparer();

        public int Compare(JsonElement x, JsonElement y)
        {
            if (x.ValueKind != y.ValueKind) throw new ArgumentException($"Cannot compare different value kinds ({x.ValueKind} and {y.ValueKind})");

            if (JsonElementEqualityComparer.Default.Equals(x, y)) return 0;

            if (x.ValueKind == JsonValueKind.Number && x.GetDouble() > y.GetDouble())
            {
                return 1;
            }

            return -1;
        }
    }
}
