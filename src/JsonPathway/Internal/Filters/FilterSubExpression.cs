using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

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

        public abstract JsonElement Execute(JsonElement input);
    }

    internal class PrimitiveFilterSubExpression: FilterSubExpression
    {
        public override bool IsPrimitive() => true;

        public FilterExpressionToken Token { get; }

        public PrimitiveFilterSubExpression(FilterExpressionToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public override string ToString() => base.ToString() + Token;

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
        }

        public override JsonElement Execute(JsonElement input)
        {
            throw new NotSupportedException($"{nameof(PrimitiveFilterSubExpression)} shouldn't be used in this context");
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

        public override JsonElement Execute(JsonElement input)
        {
            return Expression.Execute(input);
        }
    }

    internal class NegationFilterSubExpression : FilterSubExpression
    {
        public FilterSubExpression Expression { get; private set; }

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
            if (Expression is PropertyFilterSubExpression pet)
            {
                Expression = new TruthyFilterSubExpression(pet);
            }
            Expression.ReplaceTruthyExpressions();
        }

        public override JsonElement Execute(JsonElement input)
        {
            JsonElement innerResult = Expression.Execute(input);
            bool result = innerResult.IsTruthy();
            return JsonElementFactory.CreateBool(result);
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
            else if (LeftSide is ArrayAccessFilterSubExpression a1)
            {
                LeftSide = new TruthyFilterSubExpression(a1);
            }
            else
            {
                LeftSide.ReplaceTruthyExpressions();
            }

            if (RightSide is PropertyFilterSubExpression p2)
            {
                RightSide = new TruthyFilterSubExpression(p2);
            }

            else if (RightSide is ArrayAccessFilterSubExpression a2)
            {
                RightSide = new TruthyFilterSubExpression(a2);
            }
            else
            {
                RightSide.ReplaceTruthyExpressions();
            }
        }

        public override JsonElement Execute(JsonElement input)
        {
            bool left = LeftSide.Execute(input).IsTruthy();

            if (left && IsOr) return JsonElementFactory.CreateBool(true);

            bool right = RightSide.Execute(input).IsTruthy();

            bool result;
            if (IsAnd)
            {
                result = left && right;
            }
            else
            {
                result = left || right;
            }

            return JsonElementFactory.CreateBool(result);
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

        public override JsonElement Execute(JsonElement input)
        {
            JsonElement left = LeftSide.Execute(input);
            JsonElement right = RightSide.Execute(input);

            bool result = false;

            if ((IsEqual || IsGreaterOrEqual || IsLessOrEqual) && JsonElementEqualityComparer.Default.Equals(left, right))
            {
                result = true;
            }
            else if (IsNotEqual && !JsonElementEqualityComparer.Default.Equals(left, right))
            {
                result = true;
            }
            else if (left.ValueKind == right.ValueKind)
            {
                int r = JsonElementComparer.Default.Compare(left, right);

                if ((IsGreater || IsGreaterOrEqual) && r > 0)
                {
                    result = true;
                }
                else if ((IsLess || IsLessOrEqual) && r < 0)
                {
                    result = true;
                }
            }

            return JsonElementFactory.CreateBool(result);
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

        public override JsonElement Execute(JsonElement input)
        {
            JsonElement result = JsonElementFactory.CreateNull();

            foreach (var p in PropertyChain)
            {
                if (p == "length" && input.TryGetArrayOrStringLength(out int length))
                {
                    result = JsonElementFactory.CreateNumber(length);
                }
                else if (input.ValueKind == JsonValueKind.Object && input.TryGetProperty(p, out JsonElement t))
                {
                    result = t;
                }
                else
                {
                    return JsonElementFactory.CreateNull();
                }
            }

            return result;
        }
    }

    internal class ArrayAccessFilterSubExpression : FilterSubExpression
    {
        public bool IsAllArrayElemets { get; }
        public int? SliceStart { get; }
        public int? SliceEnd { get; }
        public int? SliceStep { get; }
        public int[] ExactElementsAccess { get; }
        public int TokenStartIndex { get; }
        
        public FilterSubExpression ExecutedOn { get; }
        
        public ArrayAccessFilterSubExpression(ArrayAccessExpressionToken token)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            IsAllArrayElemets = token.IsAllArrayElemets;
            SliceStart = token.SliceStart;
            SliceEnd = token.SliceEnd;
            SliceStep = token.SliceStep;
            ExactElementsAccess = token.ExactElementsAccess;

            ExecutedOn = FilterParser.Parse(new List<FilterSubExpression> { new PrimitiveFilterSubExpression(token.ExecutedOn) });
            TokenStartIndex = token.StartIndex;
        }

        public override void ReplaceTruthyExpressions()
        {
            // do nothing
        }

        public override JsonElement Execute(JsonElement input)
        {
            input = ExecutedOn.Execute(input);

            if (input.ValueKind != JsonValueKind.Array)
            {
                return JsonElementFactory.CreateNull();
            }

            var array = input.EnumerateArray().ToList();

            if (IsAllArrayElemets)
            {
                return input;
            }

            if (ExactElementsAccess != null && ExactElementsAccess.Any())
            {
                var res = array.GetByIndexes(ExactElementsAccess);
                return JsonElementFactory.CreateArray(res);
            }

            var result = array.GetSlice(SliceStart, SliceEnd, SliceStep);
            return JsonElementFactory.CreateArray(result);
        }
    }

    internal class TruthyFilterSubExpression : FilterSubExpression
    {
        public TruthyFilterSubExpression(FilterSubExpression expression)
        {
            Expression = expression ?? throw new ArgumentNullException(nameof(expression));
        }

        public FilterSubExpression Expression { get; }

        public override JsonElement Execute(JsonElement input)
        {
            input = Expression.Execute(input);
            bool result = input.IsTruthy();
            return JsonElementFactory.CreateBool(result);
        }

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

        public override JsonElement Execute(JsonElement input)
        {
            if (input.IsNullOrUndefined()) return JsonElementFactory.CreateNull();

            var args = Arguments.Select(x => x.Execute(input)).ToList();

            input = CalledOnExpression.Execute(input);

            if (input.ValueKind == JsonValueKind.String && input.TryExecuteStringMethod(MethodName, args, out JsonElement result1))
            {
                return result1;
            }

            if (input.ValueKind == JsonValueKind.Array && input.TryExecuteArrayMethod(MethodName, args, out JsonElement result2))
            {
                return result2;
            }

            return JsonElementFactory.CreateNull();
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
        private readonly JsonElement _element;

        public NumberConstantFilterSubExpression(double value)
        {
            Value = value;
            _element = JsonElementFactory.CreateNumber(value);
        }

        public double Value { get; }

        public override JsonElement Execute(JsonElement input) => _element;
    }

    internal class BooleanConstantFilterSubExpression: ConstantBaseFilterSubExpression
    {
        private readonly JsonElement _element;

        public BooleanConstantFilterSubExpression(bool value)
        {
            Value = value;
            _element = JsonElementFactory.CreateBool(value);
        }

        public bool Value { get; }

        public override JsonElement Execute(JsonElement input) => _element;
    }

    internal class StringConstantFilterSubExpression: ConstantBaseFilterSubExpression
    {
        private readonly JsonElement _element;

        public StringConstantFilterSubExpression(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
            _element = JsonElementFactory.CreateString(value);
        }

        public string Value { get; }

        public override JsonElement Execute(JsonElement input) => _element;
    }
}
