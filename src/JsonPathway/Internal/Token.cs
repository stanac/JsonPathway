using System;
using System.Globalization;
using System.Linq;

namespace JsonPathway.Internal
{
    /// <summary>
    /// Represent single token in path
    /// </summary>
    public abstract class Token
    {
        public virtual int StartIndex { get; protected set; }
        public virtual string StringValue { get; protected set; }

        public override string ToString() => $"{GetType().Name} {StringValue} at {StartIndex}";

        public bool IsStringToken() => this is StringToken;
        public bool IsWhiteSpaceToken() => this is WhiteSpaceToken;
        public bool IsPropertyToken() => this is PropertyToken;
        public bool IsNumberToken() => this is NumberToken;
        public bool IsBoolToken() => this is BoolToken;

        public bool IsSymbolToken() => this is SymbolToken;
        public bool IsSymbolTokenOpenSquareBracket() => this is SymbolToken && StringValue == "[";
        public bool IsSymbolTokenCloseSquareBracket() => this is SymbolToken && StringValue == "]";
        public bool IsSymbolTokenOpenRoundBracket() => this is SymbolToken && StringValue == "(";
        public bool IsSymbolTokenCloseRoundBracket() => this is SymbolToken && StringValue == ")";
        public bool IsSymbolTokenAtSign() => this is SymbolToken && StringValue == "@";
        public bool IsSymbolTokenQuestionMark() => this is SymbolToken && StringValue == "?";
        public bool IsSymbolTokenPoint() => this is SymbolToken && StringValue == ".";

        public StringToken CastToStringToken() => (StringToken)this;
        public WhiteSpaceToken CastToWhiteSpaceToken() => (WhiteSpaceToken)this;
        public PropertyToken CastToPropertyToken() => (PropertyToken)this;
        public NumberToken CastToNumberToken() => (NumberToken)this;
        public BoolToken CastToBoolToken() => (BoolToken)this;
        public SymbolToken CastToSymbolToken() => (SymbolToken)this;
    }

    public abstract class MultiCharToken: Token
    {
        public virtual int EndIndex { get; protected set; }

        public MultiCharToken(int startIndex, int endIndex)
        {
            if (startIndex > endIndex) throw new ArgumentException($"{nameof(startIndex)} cannot be > than {nameof(endIndex)}");

            StartIndex = startIndex;
            EndIndex = endIndex;
        }

        public bool IntersectesInclusive(int value) => value >= StartIndex && value <= EndIndex;
    }

    public class StringToken: MultiCharToken
    {
        public StringToken(int startIndex, int endIndex, string value)
            : base(startIndex, endIndex)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            StartIndex = startIndex;
            EndIndex = endIndex;
            StringValue = value;
        }
    }

    public class SymbolToken: Token
    {
        public const string SupportedChars = "[]()@?!.=></+-*";

        internal SymbolToken(PositionedChar c)
            : this (c.Index, c.Value)
        {
        }

        public SymbolToken(int index, char symbol)
        {
            StringValue = new string(new[] { symbol });
            StartIndex = index;
        }

        public bool IsOpenSquareBracket => StringValue == "[";
        public bool IsCloseSquareBracket => StringValue == "]";
        public bool IsOpenRoundBracket => StringValue == "(";
        public bool IsCloseRoundBracket => StringValue == ")";
        public bool IsAtSign => StringValue == "@";
        public bool IsQuestionMark => StringValue == "?";
        public bool IsExlamationMark => StringValue == "!";
        public bool IsPoint => StringValue == ".";
        public bool IsEqualSign => StringValue == "=";
        public bool IsGreaterThan => StringValue == ">";
        public bool IsLessThan => StringValue == "<";
        public bool IsForwardSlash => StringValue == "/";
        public bool IsPlus => StringValue == "+";
        public bool IsMinus => StringValue == "-";
        public bool IsAsterisk => StringValue == "*";

        public bool IsRecognizedSymbol() => IsCharSupported(StringValue[0]);

        public static bool IsCharSupported(char c)
        {
            return SupportedChars.Contains(c);
        }

        public void EnsureItsRecognizedSymbol()
        {
            if (!IsRecognizedSymbol()) throw new UnrecognizedSymbolException(StringValue[0], StartIndex);
        }
    }

    public class WhiteSpaceToken: Token
    {
        public WhiteSpaceToken(int index)
        {
            StartIndex = index;
            StringValue = " ";
        }
    }

    public class BoolToken: MultiCharToken
    {
        public bool BoolValue { get; }

        public BoolToken(int startIndex, int endIndex, bool value) : base(startIndex, endIndex)
        {
            BoolValue = value;
            StringValue = value.ToString();
            EndIndex = EndIndex;
        }
    }

    public class NumberToken: MultiCharToken
    {
        public static NumberStyles AllowedStyle => NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign;

        public double NumberValue { get; }

        public NumberToken(int startIndex, int endIndex, double number) : base(startIndex, endIndex)
        {
            NumberValue = number;
            StringValue = NumberValue.ToString(CultureInfo.InvariantCulture);
        }
    }

    public class PropertyToken: MultiCharToken
    {
        public PropertyToken(int startIndex, int endIndex, string value) : base(startIndex, endIndex)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            StringValue = value;
        }
    }
}
