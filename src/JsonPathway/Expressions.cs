using JsonPathway.Internal;
using JsonPathway.Internal.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonPathway
{
    public abstract class JsonPathExpression
    {
        public static ExpressionList Parse(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value not set", nameof(path));

            return ExpressionList.TokenizeAndParse(path);
        }
    }

    public class PropertyAccessExpression: JsonPathExpression
    {
        public List<string> Properties { get; }
        public bool ChildProperties { get; }
        public bool RecursiveProperties { get; }

        internal PropertyAccessExpression(PropertyToken token)
        {
            Properties = new List<string>
            {
                token.StringValue
            };
        }

        internal PropertyAccessExpression(MultiplePropertiesToken token)
        {
            Properties = token.Properties.ToList();
        }

#pragma warning disable RCS1163 // Unused parameter.
#pragma warning disable IDE0060 // Remove unused parameter
        internal PropertyAccessExpression(RecursivePropertiesToken token)
        {
            RecursiveProperties = true;
        }

        internal PropertyAccessExpression(ChildPropertiesToken token)
        {
            ChildProperties = true;
        }
    }

    public class ArrayElementsExpression : JsonPathExpression
    {
        public int? SliceStart { get; }
        public int? SliceEnd { get; }
        public int? SliceStep { get; }

        public int[] Indexes { get; }

        public bool AllElements { get; }

        internal ArrayElementsExpression(AllArrayElementsToken token)
        {
            AllElements = true;
        }
#pragma warning restore IDE0060 // Remove unused parameter
#pragma warning restore RCS1163 // Unused parameter.

        internal ArrayElementsExpression(ArrayElementsToken token)
        {
            SliceStart = token.SliceStart;
            SliceEnd = token.SliceEnd;
            SliceStep = token.SliceStep;

            Indexes = token.ExactElementsAccess;
        }
    }

    public class FilterExpression : JsonPathExpression
    {
        internal FilterSubExpression Expression { get; }

        internal FilterExpression(FilterToken token)
        {
            Expression = FilterParser.Parse(FilterExpressionTokenizer.Tokenize(token.StringValue));
        }

        public IEnumerable<JsonElement> Execute(JsonElement json)
        {
            if (json.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement c in json.EnumerateArray())
                {
                    JsonElement result = Expression.Execute(c);
                    if (result.IsTruthy()) yield return c;
                }
            }
            if (json.ValueKind == JsonValueKind.Object)
            {
                foreach (JsonElement c in json.EnumerateObject().Select(x => x.Value))
                {
                    JsonElement result = Expression.Execute(c);
                    if (result.IsTruthy()) yield return c;
                }
            }
        }
    }
}
