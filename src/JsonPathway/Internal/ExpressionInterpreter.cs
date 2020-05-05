using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class ExpressionInterpreter
    {
        public static IReadOnlyList<JsonElement> Execute(ExpressionList expressions, JsonDocument doc)
        {
            if (expressions is null) throw new ArgumentNullException(nameof(expressions));
            if (doc is null) throw new ArgumentNullException(nameof(doc));

            JsonElement element = doc.RootElement;
            List<JsonElement> result = new List<JsonElement>();

            foreach (Expression e in expressions)
            {
                result = Interpreter.Execute(e, element).ToList();
                element = JsonDocument.Parse(JsonSerializer.Serialize(result)).RootElement;
            }

            return result;
        }
    }
}
