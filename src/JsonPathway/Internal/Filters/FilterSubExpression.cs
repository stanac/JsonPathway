using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.Filters
{
    internal abstract class FilterSubExpression
    {
        public virtual bool IsPrimitive() => false;

        public bool IsPrimitive<T>() where T : FilterExpressionToken => IsPrimitive() && AsPrimitive<T>() != null;

        public PrimitiveFilterSubExpression AsPrimitive() => this as PrimitiveFilterSubExpression;

        public T AsPrimitive<T>() where T : FilterExpressionToken => (this as PrimitiveFilterSubExpression)?.Token as T;

        public override string ToString() => GetType().Name + ": ";
    }

    internal class PrimitiveFilterSubExpression: FilterSubExpression
    {
        public override bool IsPrimitive() => true;

        public FilterExpressionToken Token { get; }

        public PrimitiveFilterSubExpression(FilterExpressionToken token)
        {
            Token = token ?? throw new System.ArgumentNullException(nameof(token));
        }

        public override string ToString() => base.ToString() + Token;
    }

    internal class GroupFilterSubExpression: FilterSubExpression
    {
        public FilterSubExpression Expression { get; }

        public GroupFilterSubExpression(FilterSubExpression expression)
        {
            Expression = expression ?? throw new System.ArgumentNullException(nameof(expression));
        }

        public GroupFilterSubExpression(List<FilterSubExpression> expressions)
        {
            Expression = FilterParser.Parse(expressions);
        }
    }

    internal class NegationFilterSubExpression : FilterSubExpression
    {
        public FilterSubExpression Expression { get; }

        public NegationFilterSubExpression(FilterSubExpression expr)
        {
            Expression = expr ?? throw new ArgumentNullException(nameof(expr));
        }

        public NegationFilterSubExpression(List<FilterSubExpression> exprs)
            : this (FilterParser.Parse(exprs))
        {

        }
    }

    internal class LogicalFilterSubExpression: FilterSubExpression
    {
        public bool IsAnd { get; }
        public bool IsOr { get; }
        public FilterSubExpression LeftSide { get; }
        public FilterSubExpression RightSide { get; }

        public LogicalFilterSubExpression(bool isAnd, FilterSubExpression leftSide, FilterSubExpression rightSide)
        {
            IsAnd = isAnd;
            IsOr = !isAnd;

            LeftSide = leftSide ?? throw new ArgumentNullException(nameof(leftSide));
            RightSide = rightSide ?? throw new ArgumentNullException(nameof(rightSide));
        }

        public LogicalFilterSubExpression(bool isAnd, List<FilterSubExpression> exprLeft, List<FilterSubExpression> exprRight)
            : this (isAnd, FilterParser.Parse(exprLeft), FilterParser.Parse(exprRight))
        {
        }
    }

    internal class ComparisonFilterSubExpression: FilterSubExpression
    {
        public bool IsGreater { get; }
        public bool IsLess { get; }
        public bool IsGreaterOrEqual { get; }
        public bool IsLessOrEqual { get; }
        public bool IsEqual { get; }
        public bool IsNotEqual { get; }
        public FilterSubExpression LeftSide { get; }
        public FilterSubExpression RightSide { get; }

        public ComparisonFilterSubExpression(string oper, FilterSubExpression left, FilterSubExpression right)
        {
            if (string.IsNullOrWhiteSpace(oper)) throw new ArgumentException("message", nameof(oper));

            LeftSide = left ?? throw new ArgumentNullException(nameof(left));
            RightSide = right ?? throw new ArgumentNullException(nameof(right));

            switch (oper)
            {
                case "==": IsEqual = true; break;
                case "!=": IsNotEqual = true; break;
                case ">": IsGreater = true; break;
                case ">=": IsGreaterOrEqual = true; break;
                case "<": IsLess = true; break;
                case "<=": IsLessOrEqual = true; break;

                default:
                    throw new ArgumentException($"Unrecognized operator {oper}");
            }
        }

        public ComparisonFilterSubExpression(string oper, List<FilterSubExpression> left, List<FilterSubExpression> right)
            : this (oper, FilterParser.Parse(left), FilterParser.Parse(right))
        {

        }
    }

    internal class TruthyFilterSubExpression : FilterSubExpression
    {
        public string[] PropertyChain { get; }
        public int[] ArrayExactElementsAccess { get; }
        public int? ArraySliceStart { get; }
        public int? ArraySliceEnd { get; }
        public int? ArraySliceStep { get; }

        public TruthyFilterSubExpression(PropertyExpressionToken token)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));

            PropertyChain = token.PropertyChain.Select(x => x.StringValue).ToArray();
        }

        public TruthyFilterSubExpression(ArrayElementsToken token)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));

            ArrayExactElementsAccess = token.ExactElementsAccess;
            ArraySliceStart = token.SliceStart;
            ArraySliceEnd = token.SliceEnd;
            ArraySliceStep = token.SliceStep;
        }
    }
}
