using JsonPathway.Internal.Filters;
using System;
using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class FilterInterpreter
    {
        //public static bool IsMatched(FilterSubExpression expr, JsonElement element)
        //{
        //    if (expr is null) throw new ArgumentNullException(nameof(expr));

        //    switch (expr)
        //    {
        //        case GroupFilterSubExpression e: return IsMatched(e, element);
        //        case NegationFilterSubExpression e: return IsMatched(e, element);
        //        case LogicalFilterSubExpression e: return IsMatched(e, element);
        //        case ComparisonFilterSubExpression e: return IsMatched(e, element);
        //        case PropertyFilterSubExpression e: return IsMatched(e, element);
        //        case ArrayAccessFilterSubExpression e: return IsMatched(e, element);
        //        case TruthyFilterSubExpression e: return IsMatched(e, element);
        //        case MethodCallFilterSubExpression e: return IsMatched(e, element);
        //        case NumberConstantFilterSubExpression e: return IsMatched(e, element);
        //        case BooleanConstantFilterSubExpression e: return IsMatched(e, element);
        //        case StringConstantFilterSubExpression e: return IsMatched(e, element);
        //    }

        //    throw new ArgumentOutOfRangeException($"No filter interpreter found for {expr.GetType().Name}");
        //}

        //private static bool IsMatched(GroupFilterSubExpression e, JsonElement element)
        //{
        //    return IsMatched(e.Expression, element);
        //}

        //private static bool IsMatched(NegationFilterSubExpression e, JsonElement element)
        //{
        //    return !IsMatched(e.Expression, element);
        //}

        //private static bool IsMatched(LogicalFilterSubExpression e, JsonElement element)
        //{
        //    if (e.IsOr)
        //    {
        //        return IsMatched(e.LeftSide, element) || IsMatched(e.RightSide, element);
        //    }

        //    return IsMatched(e.LeftSide, element) && IsMatched(e.RightSide, element);
        //}

        //private static bool IsMatched(ComparisonFilterSubExpression e, JsonElement element)
        //{

        //}

        //private static bool IsMatched(PropertyFilterSubExpression e, JsonElement element)
        //{
            
        //}

        //private static bool IsMatched(ArrayAccessFilterSubExpression e, JsonElement element)
        //{
            
        //}

        //private static bool IsMatched(TruthyFilterSubExpression e, JsonElement element)
        //{
            
        //}

        //private static bool IsMatched(MethodCallFilterSubExpression e, JsonElement element)
        //{
            
        //}

        //private static bool IsMatched(NumberConstantFilterSubExpression e, JsonElement element)
        //{

        //}

        //private static bool IsMatched(StringConstantFilterSubExpression e, JsonElement element)
        //{

        //}

        //private static bool IsMatched(BooleanConstantFilterSubExpression e, JsonElement element)
        //{

        //}

        //private static bool IsTruthy(JsonElement e)
        //{

        //}
    }
}
