using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.BoolExpressions
{
    public abstract class ExpressionToken
    {
        public override string ToString() => GetType().Name + ": ";
    }

    public class PrimitiveExpressionToken: ExpressionToken
    {
        public PrimitiveExpressionToken(Token token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public Token Token { get; }

        public override string ToString() => base.ToString() + Token;
    }

    public class OpenGroupToken: ExpressionToken
    {
        public OpenGroupToken(SymbolToken token, int groupId)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            GroupId = groupId;
        }

        public SymbolToken Token { get; }
        public int GroupId { get; }

        public override string ToString() => base.ToString() + $"{Token} group id: {GroupId}";
    }

    public class CloseGroupToken : ExpressionToken
    {
        public CloseGroupToken(SymbolToken token, int groupId)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
            GroupId = groupId;
        }

        public SymbolToken Token { get; }
        public int GroupId { get; }

        public override string ToString() => base.ToString() + $"{Token} group id: {GroupId}";
    }

    public class PropertyExpressionToken: ExpressionToken
    {
        public PropertyToken[] PropertyChain { get; }

        public PropertyExpressionToken(PropertyToken[] tokens)
        {
            PropertyChain = tokens ?? throw new ArgumentNullException(nameof(tokens));
        }

        public override string ToString() => base.ToString() + ToInternalString();

        public string ToInternalString() => string.Join(" ", PropertyChain.Select(x => x.ToString()));

        public PropertyExpressionToken AllButLast(out string lastName)
        {
            lastName = PropertyChain.Last().StringValue;
            return new PropertyExpressionToken(PropertyChain.Take(PropertyChain.Length - 1).ToArray());
        }
    }

    public class NegationExpressionToken: ExpressionToken
    {
        public NegationExpressionToken(SymbolToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));

            if (!token.IsSymbolToken('!')) throw new ArgumentException("Symbol token is not ! token");
        }

        public SymbolToken Token { get; }
    }

    public abstract class OperatorExpressionToken : ExpressionToken
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

    public abstract class ConstantBaseExpressionToken: ExpressionToken
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

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class ConstantNumberExpressionToken : ConstantBaseExpressionToken
    {
        public ConstantNumberExpressionToken(NumberToken token)
            : base(token.StringValue)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public NumberToken Token { get; }

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class ConstantStringExpressionToken: ConstantBaseExpressionToken
    {
        public ConstantStringExpressionToken(StringToken token)
            : base(token.StringValue)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public StringToken Token { get; }

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class MethodCallExpressionToken: ExpressionToken
    {
        public MethodCallExpressionToken(ExpressionToken property, string methodName, ExpressionToken[] arguments)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Value not provided", nameof(methodName));
            }

            CalledOnExpression = property ?? throw new ArgumentNullException(nameof(property));
            MethodName = methodName;
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public ExpressionToken CalledOnExpression { get; }
        public string MethodName { get; }
        public ExpressionToken[] Arguments { get; private set; }

        public void ReplaceArgumentTokens(IEnumerable<ExpressionToken> tokens)
        {
            Arguments = tokens.ToArray();
        }

        public override string ToString() => base.ToString() + $"{CalledOnExpression} {MethodName} "
            + string.Join(", ", Arguments.Select(x => x.ToString()));
    }
}
