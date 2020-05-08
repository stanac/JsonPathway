using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class Interpreter
    {
        public static IReadOnlyList<JsonElement> Execute(ExpressionList expressions, JsonDocument doc)
        {
            if (expressions is null) throw new ArgumentNullException(nameof(expressions));
            if (doc is null) throw new ArgumentNullException(nameof(doc));

            List<JsonElement> result = new List<JsonElement>();
            List<JsonElement> input = new List<JsonElement>();
            input.Add(doc.RootElement);

            foreach (var ex in expressions)
            {
                result = input.SelectMany(inp => Execute(ex, inp)).ToList();
                input = result;
            }

            return result;
        }

        internal static IEnumerable<JsonElement> Execute(Expression expression, JsonElement element)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            
            switch (expression)
            {
                case PropertyAccessExpression pae:
                    return Execute(pae, element);

                case ArrayElementsExpression aee:
                    return Execute(aee, element);

                case FilterExpression fe:
                    return Execute(fe, element);
            }

            throw new ArgumentOutOfRangeException($"No interpreter implementation found for {expression.GetType()}");
        }

        private static IEnumerable<JsonElement> Execute(PropertyAccessExpression expr, JsonElement e)
        {
            if (expr.ChildProperties)
            {
                return PropertyAccessor.GetChildPropertyValues(e);
            }
            else if (expr.RecursiveProperties)
            {
                return PropertyAccessor.GetRecursiveProperties(e);
            }
            else if (expr.Properties != null && expr.Properties.Any())
            {
                return PropertyAccessor.GetPropertyValueFromChain(e, expr.Properties);
            }

            throw new InvalidOperationException($"PropertyAccessExpression");
        }

        private static IEnumerable<JsonElement> Execute(ArrayElementsExpression expr, JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                List<JsonElement> elements = element.EnumerateArray().ToList();

                if (expr.AllElements) return elements;

                if (expr.Indexes != null) return elements.GetByIndexes(expr.Indexes).ToList();

                return elements.GetSlice(expr.SliceStart, expr.SliceEnd, expr.SliceStep).ToList();
            }

            if (element.ValueKind == JsonValueKind.Object && expr.AllElements)
            {
                return element.EnumerateObject().Select(x => x.Value).ToList();
            }

            return new List<JsonElement>();
        }

        private static IEnumerable<JsonElement> Execute(FilterExpression expr, JsonElement e)
        {
            throw new NotImplementedException();
        }
    }
}
