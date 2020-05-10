using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.Filters
{
    public abstract class FilterExpressionToken
    {
        public override string ToString() => GetType().Name + ": ";
        public abstract int StartIndex { get; }
    }

    public class PrimitiveExpressionToken: FilterExpressionToken
    {
        public PrimitiveExpressionToken(Token token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public Token Token { get; }

        public override int StartIndex => Token.StartIndex;

        public override string ToString() => base.ToString() + Token;
    }

    public class OpenGroupToken: FilterExpressionToken
    {
        public OpenGroupToken(SymbolToken token, int groupId, int deptLevel)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            GroupId = groupId;
            DeptLevel = DeptLevel;
        }

        public override int StartIndex => Token.StartIndex;

        public SymbolToken Token { get; }
        public int GroupId { get; }
        public int DeptLevel { get; }

        public override string ToString() => base.ToString() + $"{Token} group id: {GroupId}";
    }

    public class CloseGroupToken : FilterExpressionToken
    {
        public CloseGroupToken(SymbolToken token, int groupId)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            GroupId = groupId;
        }
        
        public override int StartIndex => Token.StartIndex;

        public SymbolToken Token { get; }
        public int GroupId { get; }

        public override string ToString() => base.ToString() + $"{Token} group id: {GroupId}";
    }

    public class PropertyExpressionToken: FilterExpressionToken
    {
        public PropertyToken[] PropertyChain { get; }
        public bool ChildProperties { get; }
        public bool RecursiveProperties { get; }

        public PropertyExpressionToken(PropertyToken[] tokens, int startIndex)
        {
            PropertyChain = tokens ?? throw new ArgumentNullException(nameof(tokens));
            StartIndex = startIndex;
        }

        public PropertyExpressionToken(PropertyToken[] tokens, ChildPropertiesToken last, int startIndex)
        {
            if (last is null) throw new ArgumentNullException(nameof(last));
            PropertyChain = tokens ?? throw new ArgumentNullException(nameof(tokens));
            StartIndex = startIndex;
            ChildProperties = true;
        }

        public PropertyExpressionToken(PropertyToken[] tokens, RecursivePropertiesToken last, int startIndex)
        {
            if (last is null) throw new ArgumentNullException(nameof(last));
            PropertyChain = tokens ?? throw new ArgumentNullException(nameof(tokens));
            StartIndex = startIndex;
            RecursiveProperties = true;
        }

        public override int StartIndex { get; }

        public override string ToString() => base.ToString() + ToInternalString();

        public string ToInternalString() => string.Join(" ", PropertyChain.Select(x => x.ToString()));

        public PropertyExpressionToken AllButLast(out string lastName)
        {
            lastName = PropertyChain.Last().StringValue;
            return new PropertyExpressionToken(PropertyChain.Take(PropertyChain.Length - 1).ToArray(), StartIndex);
        }
    }

    public class NegationExpressionToken: FilterExpressionToken
    {
        public NegationExpressionToken(SymbolToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));

            if (!token.IsSymbolToken('!')) throw new ArgumentException("Symbol token is not ! token");
        }
        
        public override int StartIndex => Token.StartIndex;

        public SymbolToken Token { get; }
    }

    public abstract class OperatorExpressionToken : FilterExpressionToken
    {
        public SymbolToken[] Tokens { get; private set; }
        public string StringValue { get; private set; }
        private static readonly string[] logicalBinOps = new[] { "||", "&&" };
        private static readonly string[] comparisonOps = new[] { ">", ">=", "<", "<=", "==", "!=" };

        public static OperatorExpressionToken Create(SymbolToken[] tokens)
        {
            if (tokens == null) throw new ArgumentNullException(nameof(tokens));
            if (tokens.Length == 0) throw new ArgumentException("empty array not allowed", nameof(tokens));

            string value = new string(tokens.Select(x => x.StringValue[0]).ToArray());

            if (tokens.Length > 2 || (!logicalBinOps.Contains(value) && !comparisonOps.Contains(value)))
                throw new UnrecognizedCharSequence($"Unrecognized sequence of symbols {value} starting at {tokens[0].StartIndex}");

            OperatorExpressionToken op;
            if (logicalBinOps.Contains(value))
            {
                op = new LogicalBinaryOperatorExpressionToken(value);
            }
            else
            {
                op = new ComparisonOperatorExpressionToken(value);
            }

            op.Tokens = tokens;
            op.StringValue = value;
            return op;
        }
        
        public override int StartIndex => Tokens.First().StartIndex;

        public override string ToString() => base.ToString() + $" {StringValue} at {Tokens.First().StartIndex}";
    }

    public class LogicalBinaryOperatorExpressionToken: OperatorExpressionToken
    {
        internal LogicalBinaryOperatorExpressionToken(string value)
        {
            if (value == "||") IsOr = true;
            else if (value == "&&") IsAnd = true;
            else throw new ArgumentException($"Unrecognized value {value}", nameof(value));
        }

        public bool IsOr { get; }
        public bool IsAnd { get; }
    }

    public class ComparisonOperatorExpressionToken: OperatorExpressionToken
    {
        internal ComparisonOperatorExpressionToken(string value)
        {
            if (value == "==") IsEquals = true;
            else if (value == "!=") IsNotEqual = true;
            else if (value == ">") IsGreater = true;
            else if (value == ">=") IsGreaterOrEqual = true;
            else if (value == "<") IsLess = true;
            else if (value == "<=") IsLessOrEqual = true;
            else throw new ArgumentException($"Unrecognized value {value}", nameof(value));
        }

        public bool IsEquals { get; }
        public bool IsNotEqual { get; }
        public bool IsGreater { get; }
        public bool IsGreaterOrEqual { get; }
        public bool IsLess { get; }
        public bool IsLessOrEqual { get; }

    }

    public abstract class ConstantBaseExpressionToken: FilterExpressionToken
    {
        public string StringValue { get; }

        public ConstantBaseExpressionToken(string value)
        {
            StringValue = value;
        }
    }
    
    public class ConstantBoolExpressionToken : ConstantBaseExpressionToken
    {
        public ConstantBoolExpressionToken (BoolToken token)
            : base(token.StringValue)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public BoolToken Token { get; }
        
        public override int StartIndex => Token.StartIndex;

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class ConstantNumberExpressionToken : ConstantBaseExpressionToken
    {
        public ConstantNumberExpressionToken(NumberToken token)
            : base(token.StringValue)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public NumberToken Token { get; private set; }
        
        public override int StartIndex => Token.StartIndex;

        public override string ToString() => base.ToString() + Token.StringValue;

        public ConstantNumberExpressionToken CreateNegativeNumber()
        {
            return new ConstantNumberExpressionToken(new NumberToken(Token.StartIndex, Token.EndIndex, -Token.NumberValue));
        }
    }

    public class ConstantStringExpressionToken: ConstantBaseExpressionToken
    {
        public ConstantStringExpressionToken(StringToken token)
            : base(token.StringValue)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public StringToken Token { get; }
        
        public override int StartIndex => Token.StartIndex;

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class MethodCallExpressionToken: FilterExpressionToken
    {
        public MethodCallExpressionToken(FilterExpressionToken calledOnExpression, string methodName, FilterExpressionToken[] arguments)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Value not provided", nameof(methodName));
            }

            CalledOnExpression = calledOnExpression ?? throw new ArgumentNullException(nameof(calledOnExpression));
            MethodName = methodName;
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public FilterExpressionToken CalledOnExpression { get; }
        public string MethodName { get; }
        public FilterExpressionToken[] Arguments { get; private set; }
        
        public override int StartIndex => CalledOnExpression.StartIndex;

        public void ReplaceArgumentTokens(IEnumerable<FilterExpressionToken> tokens)
        {
            Arguments = tokens.ToArray();
        }

        public override string ToString() => base.ToString() + $"{CalledOnExpression} {MethodName} "
            + string.Join(", ", Arguments.Select(x => x.ToString()));
    }

    public class ArrayAccessExpressionToken: FilterExpressionToken
    {
        public bool IsAllArrayElemets { get; }
        public int? SliceStart { get; }
        public int? SliceEnd { get; }
        public int? SliceStep { get; }
        public int[] ExactElementsAccess { get; }

        public override int StartIndex { get; }
        public FilterExpressionToken ExecutedOn { get; }

        private ArrayAccessExpressionToken(FilterExpressionToken executedOn, MultiCharToken arrayToken)
        {
            if (arrayToken == null) throw new ArgumentNullException(nameof(arrayToken));

            ExecutedOn = executedOn ?? throw new ArgumentNullException(nameof(executedOn));

            if (!(executedOn is PropertyExpressionToken) && !(executedOn is FilterExpressionToken))
            {
                throw new UnexpectedTokenException(arrayToken, "Array access can be applied to property or filter");
            }
        }

        public ArrayAccessExpressionToken(FilterExpressionToken executedOn, AllArrayElementsToken token)
            : this (executedOn, token as MultiCharToken)
        {
            if (token is null) throw new ArgumentNullException(nameof(token));
            IsAllArrayElemets = true;
            StartIndex = token.StartIndex;
        }

        public ArrayAccessExpressionToken(FilterExpressionToken executedOn, ArrayElementsToken token)
            : this(executedOn, token as MultiCharToken)
        {
            if (token == null) throw new ArgumentNullException(nameof(token));

            SliceStart = token.SliceStart;
            SliceEnd = token.SliceEnd;
            SliceStep = token.SliceStep;
            ExactElementsAccess = token.ExactElementsAccess;
            StartIndex = token.StartIndex;
        }

        public override string ToString() => base.ToString() + $"Executed on {ExecutedOn} with index: {GetIndexValues()}";

        public string GetIndexValues()
        {
            if (IsAllArrayElemets) return "*";

            if (ExactElementsAccess != null) return string.Join(",", ExactElementsAccess.Select(x => x.ToString()));

            return $"{SliceStart}:{SliceEnd}:{SliceStep}";
        }
    }
}
