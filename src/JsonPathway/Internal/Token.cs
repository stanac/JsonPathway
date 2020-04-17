using System;
using System.Globalization;
using System.Linq;

namespace JsonPathway.Internal
{
    /// <summary>
    /// Represent single token in json path
    /// </summary>
    public abstract class Token
    {
        public virtual int StartIndex { get; protected set; }
        public virtual string StringValue { get; protected set; }

        public override string ToString() => $"{GetType().Name} {StringValue} at {StartIndex}";

        public bool IsStringToken() => this is StringToken;
        public bool IsWhiteSpaceToken() => this is WhiteSpaceToken;
        public bool IsPropertyToken() => this is PropertyToken;
        public bool IsFilterToken() => this is FilterToken;
        public bool IsNumberToken() => this is NumberToken;
        public bool IsBoolToken() => this is BoolToken;
        public bool IsChildPropertiesToken() => this is ChildPropertiesToken;
        public bool IsRecursivePropertiesToken() => this is RecursivePropertiesToken;
        public bool IsAllArrayElementsToken() => this is AllArrayElementsToken;

        public bool IsSymbolToken() => this is SymbolToken;
        public bool IsSymbolToken(char value) => this is SymbolToken && StringValue[0] == value;

        public StringToken CastToStringToken() => (StringToken)this;
        public WhiteSpaceToken CastToWhiteSpaceToken() => (WhiteSpaceToken)this;
        public PropertyToken CastToPropertyToken() => (PropertyToken)this;
        public NumberToken CastToNumberToken() => (NumberToken)this;
        public BoolToken CastToBoolToken() => (BoolToken)this;
        public SymbolToken CastToSymbolToken() => (SymbolToken)this;
        public FilterToken CastToFilterToken() => (FilterToken)this;
        public ChildPropertiesToken CastToChildPropertiesToken() => (ChildPropertiesToken)this;
        public RecursivePropertiesToken CastToRecursivePropertiesToken() => (RecursivePropertiesToken)this;
        public ArrayElementsToken CastToArrayElementsToken() => (ArrayElementsToken)this;
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

        public string GetQuotedValue() => $"'{StringValue.Replace("'", "\\'")}'";
    }

    public class SymbolToken: Token
    {
        public const string SupportedChars = "[]()@?!.=></+-*&|,:";

        internal SymbolToken(PositionedChar c)
            : this (c.Index, c.Value)
        {
        }

        public SymbolToken(int index, char symbol)
        {
            StringValue = new string(new[] { symbol });
            StartIndex = index;
        }

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
        public bool Escaped { get; }

        public PropertyToken(int startIndex, int endIndex, string value, bool escaped) : base(startIndex, endIndex)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            Escaped = escaped;
            StringValue = value;
        }
    }

    /// <summary>
    /// * JSONPath Operator
    /// </summary>
    public class ChildPropertiesToken: Token
    {
        public ChildPropertiesToken(int index)
        {
            StartIndex = index;
            StringValue = "*";
        }
    }

    public class AllArrayElementsToken: MultiCharToken
    {
        public AllArrayElementsToken(int startIndex, int endIndex) : base(startIndex, endIndex)
        {
            StringValue = "[*]";
        }
    }

    /// <summary>
    /// .. JSONPath Operator
    /// </summary>
    public class RecursivePropertiesToken : MultiCharToken
    {
        public RecursivePropertiesToken(int startIndex, int endIndex) : base(startIndex, endIndex)
        {
            StringValue = "..";
        }
    }

    public class ArrayElementsToken: MultiCharToken
    {
        public int[] ExactElementsAccess { get; }
        public int? Start { get; }
        public int? End { get; }
        public int? Step { get; }

        public ArrayElementsToken(int startIndex, int endIndex, string value)
            : base(startIndex, endIndex)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value not set", nameof(value));

            StringValue = value.Trim();

            if (!value.StartsWith("[")) throw new ArgumentException("value must start with [");
            if (!value.EndsWith("]")) throw new ArgumentException("value must end with ]");

            value = value.Substring(1, value.Length - 2).Trim();

            if (value.Contains(":"))
            {
                string[] parts = value.Split(":".ToCharArray(), StringSplitOptions.None);

                if (string.IsNullOrWhiteSpace(parts[0])) Start = 0;
                else if (int.TryParse(parts[0], out int t1)) Start = t1;
                else throw new UnrecognizedCharSequence("Unexpected value in array slice operator at array element operator starting at " + startIndex);

                if (string.IsNullOrWhiteSpace(parts[1])) End = null;
                else if (int.TryParse(parts[1], out int t2)) End = t2;
                else throw new UnrecognizedCharSequence("Unexpected value in array slice operator at array element operator starting at " + startIndex);

                if (parts.Length == 3)
                {
                    if (string.IsNullOrWhiteSpace(parts[2])) Step = null;
                    else if (int.TryParse(parts[2], out int t3)) Step = t3;
                    else throw new UnrecognizedCharSequence("Unexpected value in array slice operator at array element operator starting at " + startIndex);
                }
                else if (parts.Length > 3)
                {
                    throw new ArgumentException("Value contains more than 2 : which isn't valid syntax for array element starting at " + startIndex);
                }
            }
            else
            {
                ExactElementsAccess = value.Split(',').Select(x =>
                    {
                        if (int.TryParse(x, out int t)) return t;
                        throw new UnrecognizedCharSequence($"Failed to parse value {x} for array element access operator starting at " + startIndex);
                    })
                    .ToArray();
            }
        }
    }

    public class FilterToken: MultiCharToken
    {
        public FilterToken(int startIndex, int endIndex, string value) : base(startIndex, endIndex)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value.StartsWith("[?("))
            {
                value = value.Substring(3);
            }

            if (value.EndsWith(")]"))
            {
                value = value.Substring(0, value.Length - 2);
            }

            StringValue = value;
        }
    }
}
