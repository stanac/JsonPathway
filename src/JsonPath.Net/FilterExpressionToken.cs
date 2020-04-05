using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonPath.Net
{
    internal abstract class FilterToken : SecondLevelToken
    {
        public override int Index => FilterTokenIndex;

        public virtual int FilterTokenIndex { get; } = int.MinValue;

        public override bool IsEscapedPath => false;

        public override bool IsUnEscapedEmptyPath => false;

        public override bool IsUnEscapedPath => false;

        public override abstract string ToString();
    }

    internal class FilterExpressionToken : FilterToken
    {
        public int StartIndex { get; }
        public int EndIndex { get; }

        public FilterExpressionToken(string value, int startIndex, int endIndex)
            : this (PositionedChar.CreateFromString(value), startIndex, endIndex)
        {
        }

        public FilterExpressionToken(PositionedChar[] chars, int startIndex, int endIndex)
        {
            Chars = chars;
            Value = PositionedChar.CreateString(chars);
            StartIndex = startIndex;
            EndIndex = endIndex;

            EnsureValid();
        }

        public FilterExpressionToken(Token[] tokens, int startIndex, int endIndex)
        {
            var chars = tokens.Select(t =>
            {
                if (t is CharToken ct) return new PositionedChar(ct.Value, ct.Index);
                if (t is OpenStringToken) return new PositionedChar('[', t.Index);
                if (t is CloseStringToken) return new PositionedChar(']', t.Index);
                if (t is PathSeparatorToken) return new PositionedChar('.', t.Index);

                throw new IndexOutOfRangeException();

            }).ToArray();

            Chars = chars;
            Value = PositionedChar.CreateString(chars);
            StartIndex = startIndex;
            EndIndex = endIndex;

            EnsureValid();
        }

        public string Value { get; }

        public PositionedChar[] Chars { get; }

        public override string ToString() => $"FilterExpressionToken {Value}";

        public bool Intersects(int index) => index >= StartIndex && index <= EndIndex;

        private void EnsureValid()
        {

        }
    }

    internal class ComparisonOperatorToken : FilterToken
    {
        public static readonly string[] SupportedOperators = new[] { "==", "!=", ">", "<", "<=", ">=" };

        public ComparisonOperatorToken(PositionedChar[] chars)
            : this(PositionedChar.CreateString(chars))
        {
        }

        public ComparisonOperatorToken(string value)
        {
            Value = value?.Trim();

            if (!SupportedOperators.Contains(Value))
                throw new ArgumentException($"Value {value} not supported as operator");
        }

        public string Value { get; }

        public override string ToString() => $"ComparisonOperatorToken {Value}";
    }

    internal class ComparisonMethodToken: FilterToken
    {
        public static readonly string[] SupportedOperators = new []
        {
            "contains", "startsWith", "endsWith",
            "containsCaseInsensitive", "startsWithCaseInsensitive", "endsWithCaseInsensitive"
        };

        public ComparisonMethodToken(PositionedChar[] chars)
            : this(PositionedChar.CreateString(chars))
        {
        }

        public ComparisonMethodToken(string value)
        {
            Value = value?.Trim();

            if (!SupportedOperators.Contains(Value))
                throw new ArgumentException($"Value {value} not supported as method, supported methods " +
                    $"are: {string.Join(", ", SupportedOperators)}");
        }

        public string Value { get; }

        public override string ToString() => $"ComparisonMethodToken {Value}";
    }

    internal class ConstantToken: FilterToken
    {
        public static NumberStyles AllowedNumberStyle = NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingWhite | NumberStyles.AllowDecimalPoint;

        private ConstantToken(PositionedChar[] chars, bool isString)
            : this(PositionedChar.CreateString(chars), isString)
        {
        }

        private ConstantToken(string value, bool isString)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Value not set", nameof(value));
            }

            Value = value.Trim();
            IsString = isString;
            
            if (!isString)
            {
                IsBoolean = string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase) || string.Equals(value.Trim(), "false", StringComparison.OrdinalIgnoreCase);
                IsNumber = float.TryParse(value, AllowedNumberStyle, CultureInfo.InvariantCulture, out _);

                if (IsNumber) NumberValue = float.Parse(value, AllowedNumberStyle, CultureInfo.InvariantCulture);
                else if (IsBoolean) BooleanValue = bool.Parse(value.Trim());
                else throw new ArgumentException($"Value {Value} cannot be recognized as number or boolean");
            }
        }

        public static ConstantToken CreateString(string value) => new ConstantToken(value, true);
        public static ConstantToken CreateBoolean(bool value) => new ConstantToken(value.ToString().ToLower(), false);
        public static ConstantToken CreateNumber(float value) => new ConstantToken(value.ToString(CultureInfo.InvariantCulture).ToLower(), false);

        public static ConstantToken Create(PositionedChar[] chars) => Create(PositionedChar.CreateString(chars));

        public static ConstantToken Create(string value)
        {
            value = value.Trim();

            if ((value.StartsWith("\"") && value.EndsWith("\"")) || (value.StartsWith("'") && value.EndsWith("'")))
            {

            }
            else if (bool.TryParse(value, out bool b))
            {
                return CreateBoolean(b);
            }
            else if (float.TryParse(value, AllowedNumberStyle, CultureInfo.InvariantCulture, out float f))
            {
                return CreateNumber(f);
            }

            throw new ArgumentException($"String {value} not recognized as constant string, boolean or number. For string constants value needs to be under single/double quotes");
        }

        public override string ToString()
        {
            if (IsString) return $"String ConstantToken: {Value}";
            if (IsNumber) return $"Number ConstantToken: {Value}";
            return $"Boolean ConstantToken: {BooleanValue.Value.ToString()}";
        }

        public string Value { get; }
        public bool? BooleanValue { get; }
        public float? NumberValue { get; }

        public bool IsString { get; }
        public bool IsBoolean { get; }
        public bool IsNumber { get; }

    }

}
