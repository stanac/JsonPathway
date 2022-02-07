using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.Filters
{
    /// <summary>
    /// Converter from FilterExpressionTokens to FilterSubExpression
    /// </summary>
    internal static class FilterParser
    {
        public static FilterSubExpression Parse(IEnumerable<FilterExpressionToken> tokens) => Parse(tokens.ToList());

        private static FilterSubExpression Parse(List<FilterExpressionToken> tokens)
        {
            List<FilterSubExpression> expressions = tokens
                .Select(x => new PrimitiveFilterSubExpression(x))
                .Cast<FilterSubExpression>()
                .ToList();

            FilterSubExpression expr = Parse(expressions);
            expr.ReplaceTruthyExpressions();

            return expr;
        }

        public static FilterSubExpression Parse(List<FilterSubExpression> exprs)
        {
            int callCount = 0;

            while (exprs.Any(x => x.IsPrimitive()) || exprs.Count != 1)
            {
                if (exprs.Count == 0) throw new InternalJsonPathwayException("Unexpected expression count 0");

                exprs = ParseInner(exprs, ref callCount);
            }

            return exprs.Single();
        }

        private static List<FilterSubExpression> ParseInner(List<FilterSubExpression> exprs, ref int callCount)
        {
            callCount++;
            if (callCount > 5_000) throw new InternalJsonPathwayException("FilterParser.ParseInner call count exceeded max allowed number of calls");

            exprs = ReplaceConstantAndPropsAndMethodsAndArraysExpressions(exprs);

            bool groupParsed;
            do
            {
                exprs = ParseTopGroup(exprs, out groupParsed);
            }
            while (groupParsed);

            exprs = ParseLogicalExpressions(exprs);
            exprs = ParseComparisonExpressions(exprs);
            exprs = ReplaceNegationExpressions(exprs);

            while (exprs.Any(x => x.IsPrimitive()))
            {
                exprs = ParseInner(exprs, ref callCount);
            }

            return exprs;
        }

        private static List<FilterSubExpression> ParseTopGroup(List<FilterSubExpression> exprs, out bool parsed)
        {
            List<FilterSubExpression> groups = exprs.Where(x => x.IsPrimitive<OpenGroupToken>()).ToList();

            if (groups.Count == 0)
            {
                parsed = false;
                return exprs;
            }

            List<FilterSubExpression> ret = exprs.ToList();

            parsed = true;

            int topGroupLevel = groups.Max(x => x.AsPrimitive<OpenGroupToken>().DeptLevel);
            FilterSubExpression open = groups.First(x => x.AsPrimitive<OpenGroupToken>().DeptLevel == topGroupLevel);
            int topGroupId = open.AsPrimitive<OpenGroupToken>().GroupId;
            FilterSubExpression closed = exprs.First(x => x.AsPrimitive<CloseGroupToken>()?.GroupId == topGroupId);

            List<FilterSubExpression> groupExpr = new List<FilterSubExpression>();

            int openIndex = ret.IndexOf(open);
            int closedIndex = ret.IndexOf(closed);

            for (int i = openIndex + 1; i < closedIndex; i++)
            {
                groupExpr.Add(ret[i]);
                ret[i] = null;
            }

            ret[closedIndex] = null;

            ret[openIndex] = new GroupFilterSubExpression(groupExpr);

            return ret.Where(x => x != null).ToList();
        }

        private static List<FilterSubExpression> ParseLogicalExpressions(List<FilterSubExpression> exprs)
        {
            List<FilterSubExpression> leftSide = new List<FilterSubExpression>();

            for (int i = 0; i < exprs.Count; i++)
            {
                if (exprs[i].IsPrimitive<LogicalBinaryOperatorExpressionToken>())
                {
                    List<FilterSubExpression> rightSide = new List<FilterSubExpression>();

                    for (int j = i + 1; j < exprs.Count; j++)
                    {
                        rightSide.Add(exprs[j]);
                    }

                    bool isAnd = exprs[i].AsPrimitive<LogicalBinaryOperatorExpressionToken>().IsAnd;

                    return new List<FilterSubExpression>
                    {
                        new LogicalFilterSubExpression(isAnd, leftSide, rightSide)
                    };
                }
                else
                {
                    leftSide.Add(exprs[i]);
                }
            }

            return exprs;
        }
        
        private static List<FilterSubExpression> ParseComparisonExpressions(List<FilterSubExpression> exprs)
        {
            List<FilterSubExpression> leftSide = new List<FilterSubExpression>();

            for (int i = 0; i < exprs.Count; i++)
            {
                if (exprs[i].IsPrimitive<ComparisonOperatorExpressionToken>())
                {
                    List<FilterSubExpression> rightSide = new List<FilterSubExpression>();

                    for (int j = i + 1; j < exprs.Count; j++)
                    {
                        rightSide.Add(exprs[j]);
                    }

                    string oper = exprs[i].AsPrimitive<ComparisonOperatorExpressionToken>().StringValue;

                    return new List<FilterSubExpression>
                    {
                        new ComparisonFilterSubExpression(oper, leftSide, rightSide)
                    };
                }
                else
                {
                    leftSide.Add(exprs[i]);
                }
            }

            return exprs;
        }

        private static List<FilterSubExpression> ReplaceConstantAndPropsAndMethodsAndArraysExpressions(List<FilterSubExpression> exprs)
        {
            List<FilterSubExpression> ret = exprs.ToList();

            for (int i = 0; i < ret.Count; i++)
            {
                if (ret[i].IsPrimitive<ConstantBaseExpressionToken>())
                {
                    ret[i] = ConstantBaseFilterSubExpression.Create(ret[i].AsPrimitive<ConstantBaseExpressionToken>());
                }
                else if (ret[i].IsPrimitive<NullExpressionToken>())
                {
                    ret[i] = ConstantBaseFilterSubExpression.Create(ret[i].AsPrimitive<NullExpressionToken>());
                }
                else if (ret[i].IsPrimitive<PropertyExpressionToken>())
                {
                    ret[i] = new PropertyFilterSubExpression(ret[i].AsPrimitive<PropertyExpressionToken>());
                }
                else if (ret[i].IsPrimitive<MethodCallExpressionToken>())
                {
                    ret[i] = new MethodCallFilterSubExpression(ret[i].AsPrimitive<MethodCallExpressionToken>());
                }
                else if (ret[i].IsPrimitive<ArrayAccessExpressionToken>())
                {
                    ret[i] = new ArrayAccessFilterSubExpression(ret[i].AsPrimitive<ArrayAccessExpressionToken>());
                }
            }

            return ret;
        }

        private static List<FilterSubExpression> ReplaceNegationExpressions(List<FilterSubExpression> exprs)
        {
            List<FilterSubExpression> ret = exprs.ToList();

            for (int i = exprs.Count - 2; i >= 0; i--)
            {
                if (ret[i] != null && ret[i].IsPrimitive<NegationExpressionToken>())
                {
                    ret[i] = new NegationFilterSubExpression(new List<FilterSubExpression> { ret[i + 1] });
                    ret[i + 1] = null;
                }
            }

            return ret.Where(x => x != null).ToList();
        }
    }
}
