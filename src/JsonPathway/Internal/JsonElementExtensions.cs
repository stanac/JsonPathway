﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class JsonElementExtensions
    {
        public static bool IsNullOrUndefined(this JsonElement e) => e.ValueKind == JsonValueKind.Null || e.ValueKind == JsonValueKind.Undefined;

        public static bool IsArrayOrString(this JsonElement e) => e.ValueKind == JsonValueKind.Array || e.ValueKind == JsonValueKind.String;

        public static int GetArrayOrStringLength(this JsonElement e)
        {
            if (e.ValueKind == JsonValueKind.Array) return e.GetArrayLength();

            if (e.ValueKind == JsonValueKind.String) return e.GetString().Length;

            throw new ArgumentException($"input element is {e.ValueKind} and expected is string or array");
        }

        public static bool TryGetArrayOrStringLength(this JsonElement e, out int length)
        {
            if (e.IsArrayOrString())
            {
                length = e.GetArrayOrStringLength();
                return true;
            }

            length = -1;
            return false;
        }

        public static bool IsTruthy(this JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.True:
                case JsonValueKind.Object:
                case JsonValueKind.Array:
                    return true;
                case JsonValueKind.False:
                case JsonValueKind.Null:
                case JsonValueKind.Undefined:
                    return false;
                case JsonValueKind.Number:
                    return e.GetDouble() != 0.0;
                case JsonValueKind.String:
                    return e.GetString().Length > 0;
            }

            throw new ArgumentException($"Value kind {e.ValueKind} not implemented");
        }

        public static List<JsonElement> EnumerateRecursively(this JsonElement e)
        {
            List<JsonElement> result = new List<JsonElement>();
            EnumerateRecursively(e, result);
            return result;
        }

        private static void EnumerateRecursively(JsonElement e, List<JsonElement> result)
        {
            if (e.ValueKind == JsonValueKind.Object)
            {
                result.Add(e);
                foreach (JsonElement c in e.EnumerateObject().Select(x => x.Value))
                {
                    EnumerateRecursively(c, result);
                }
            }
            else if (e.ValueKind == JsonValueKind.Array)
            {
                result.Add(e);
                foreach (JsonElement c in e.EnumerateArray())
                {
                    EnumerateRecursively(c, result);
                }
            }
        }
    }
}
