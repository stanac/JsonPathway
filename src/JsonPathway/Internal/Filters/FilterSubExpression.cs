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

        public abstract void ReplaceTruthyExpressions();
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

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
        }
    }

    internal class GroupFilterSubExpression: FilterSubExpression
    {
        public FilterSubExpression Expression { get; }

        public GroupFilterSubExpression(FilterSubExpression expression)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public GroupFilterSubExpression(List<FilterSubExpression> expressions)
        {
            Expression = FilterParser.Parse(expressions);
        }

        public override void ReplaceTruthyExpressions()
        {
            Expression.ReplaceTruthyExpressions();
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

        public override void ReplaceTruthyExpressions()
        {
            Expression.ReplaceTruthyExpressions();
        }
    }

    internal class LogicalFilterSubExpression: FilterSubExpression
    {
        public bool IsAnd { get; }
        public bool IsOr { get; }
        public FilterSubExpression LeftSide { get; private set; }
        public FilterSubExpression RightSide { get; private set; }

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

        public override void ReplaceTruthyExpressions()
        {
            if (LeftSide is PropertyFilterSubExpression p1)
            {
                LeftSide = new TruthyFilterSubExpression(p1);
            }
            else
            {
                LeftSide.ReplaceTruthyExpressions();
            }

            if (RightSide is PropertyFilterSubExpression p2)
            {
                RightSide = new TruthyFilterSubExpression(p2);
            }
            else
            {
                RightSide.ReplaceTruthyExpressions();
            }
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
        public FilterSubExpression LeftSide { get; set; }
        public FilterSubExpression RightSide { get; set; }

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

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
        }

    }

    internal class PropertyFilterSubExpression: FilterSubExpression
    {
        public string[] PropertyChain { get; }

        public PropertyFilterSubExpression(PropertyExpressionToken token)
        {
            PropertyChain = token?.PropertyChain?.Select(x => x.StringValue)?.ToArray() ?? throw new ArgumentNullException(nameof(token));
        }

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
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

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
        }
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

        public override void ReplaceTruthyExpressions()
        {
            foreach (var a in Arguments)
            {
                a.ReplaceTruthyExpressions();
            }
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

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
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
