using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway.Internal
{
    public static class ExpressionInterpreter
    {
        public static IReadOnlyList<JsonElement> Execute(ExpressionList expressions, JsonDocument doc)
        {
            if (expressions is null) throw new ArgumentNullException(nameof(expressions));
            if (doc is null) throw new ArgumentNullException(nameof(doc));

            throw new Exception();
        }
    }
}
