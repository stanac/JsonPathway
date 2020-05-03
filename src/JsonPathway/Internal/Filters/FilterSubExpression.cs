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

    internal class PropertyFilterSubExpression: FilterSubExpression
    {
        public string[] PropertyChain { get; }

        public PropertyFilterSubExpression(PropertyExpressionToken token)
        {
            PropertyChain = token?.PropertyChain?.Select(x => x.StringValue)?.ToArray() ?? throw new ArgumentNullException(nameof(token));
        }
    }

    internal class TruthyFilterSubExpression : FilterSubExpression
    {
        public string[] PropertyChain { get; }
        public int[] ArrayExactElementsAccess { get; }
        public int? ArraySliceStart { get; }
        public int? ArraySliceEnd { get; }
        public int? ArraySliceStep { get; }

        public TruthyFilterSubExpression(PropertyFilterSubExpression expr)
        {
            if (expr == null) throw new ArgumentNullException(nameof(expr));
            PropertyChain = expr.PropertyChain;
        }

        public TruthyFilterSubExpression(PropertyExpressionToken token)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));

            PropertyChain = token.PropertyChain.Select(x => x.StringValue).ToArray();
        }

        // todo: add support for arrays
    }

    internal class MethodCallFilterSubExpression : FilterSubExpression
    {
        public FilterSubExpression CalledOnExpression { get; }
        public string MethodName { get; }
        public IReadOnlyList<FilterSubExpression> Arguments { get; }

        public MethodCallFilterSubExpression(MethodCallExpressionToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            CalledOnExpression = FilterParser.Parse(new List<FilterExpressionToken> { token.CalledOnExpression });
            MethodName = token.MethodName;

            List<FilterSubExpression> args = new List<FilterSubExpression>();

            foreach (var a in token.Arguments)
            {
                args.Add(FilterParser.Parse(new List<FilterExpressionToken> { a }));
            }

            Arguments = args;
        }
    }

    internal abstract class ConstantBaseFilterSubExpression: FilterSubExpression
    {
        public static ConstantBaseFilterSubExpression Create(ConstantBaseExpressionToken token)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));

            if (token is ConstantBoolExpressionToken b) return new BooleanConstantFilterSubExpression(b.Token.BoolValue);

            if (token is ConstantStringExpressionToken s) return new StringConstantFilterSubExpression(s.StringValue);

            if (token is ConstantNumberExpressionToken n) return new NumberConstantFilterSubExpression(n.Token.NumberValue);

            throw new IndexOutOfRangeException("Unrecognized type: " + token.GetType().Name);
        }
    }

    internal class NumberConstantFilterSubExpression: ConstantBaseFilterSubExpression
    {
        public NumberConstantFilterSubExpression(double value)
        {
            Value = value;
        }

        public double Value { get; }
    }

    internal class BooleanConstantFilterSubExpression: ConstantBaseFilterSubExpression
    {
        public BooleanConstantFilterSubExpression(bool value)
        {
            Value = value;
        }

        public bool Value { get; }
    }

    internal class StringConstantFilterSubExpression: ConstantBaseFilterSubExpression
    {
        public StringConstantFilterSubExpression(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value { get; }
    }
}
