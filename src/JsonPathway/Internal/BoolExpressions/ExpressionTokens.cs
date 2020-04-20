using System;
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
            if (tokens.Length == 0) throw new ArgumentException("Empty array not allowed");
        }

        public override string ToString() => base.ToString() + ToInternalString();

        public string ToInternalString() => string.Join("", PropertyChain.Select(x => x.ToString()));
    }

    public class ComparisonToken : ExpressionToken
    {
        public SymbolToken[] Tokens { get; }

        public ComparisonToken(params SymbolToken[] tokens)
        {
            Tokens = tokens ?? throw new ArgumentNullException(nameof(tokens));
            if (tokens.Length == 0) throw new ArgumentException("Empty array not allowed");
        }

        public override string ToString() => base.ToString() + ToInternalString();

        public string ToInternalString() => string.Join("", Tokens.Select(x => x.ToString()));
    }

    public class ExpressionConstantBoolToken: ExpressionToken
    {
        public ExpressionConstantBoolToken(BoolToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public BoolToken Token { get; }

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class ExpressionConstantNumberToken : ExpressionToken
    {
        public ExpressionConstantNumberToken(NumberToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public NumberToken Token { get; }

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class ExpressionConstantStringToken: ExpressionToken
    {
        public ExpressionConstantStringToken(StringToken token)
        {
            Token = token ?? throw new ArgumentNullException(nameof(token));
        }

        public StringToken Token { get; }

        public override string ToString() => base.ToString() + Token.StringValue;
    }

    public class MethodCallToken: ExpressionToken
    {
        public MethodCallToken(PropertyExpressionToken property, string methodName, ExpressionToken[] arguments)
        {
            if (string.IsNullOrWhiteSpace(methodName))
            {
                throw new ArgumentException("Value not provided", nameof(methodName));
            }

            Property = property ?? throw new ArgumentNullException(nameof(property));
            MethodName = methodName;
            Arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
        }

        public PropertyExpressionToken Property { get; }
        public string MethodName { get; }
        public ExpressionToken[] Arguments { get; }

        public override string ToString() => base.ToString() + $"{Property.ToInternalString()} {MethodName} "
            + string.Join(", ", Arguments.Select(x => x.ToString()));
    }
}
