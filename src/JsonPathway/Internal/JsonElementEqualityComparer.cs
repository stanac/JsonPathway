using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal
{
    public class JsonElementEqualityComparer : IEqualityComparer<JsonElement>
    {
        public static JsonElementEqualityComparer Default { get; } = new JsonElementEqualityComparer();

        public bool Equals(JsonElement x, JsonElement y)
        {
            if (x.ValueKind != y.ValueKind) return false;
            return x.ToString() == y.ToString();
        }

        public int GetHashCode(JsonElement obj) => obj.ToString().GetHashCode();
    }
}
