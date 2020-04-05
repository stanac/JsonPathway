using System;

namespace JsonPathway.Internal
{
    internal struct PositionedChar
    {
        public char Value { get; }
        public int Index { get; }
        public bool IsEscaped { get; }
        public bool IsEscapeSymbol => Value == '\\';

        public PositionedChar(int index, char value, bool isEscaped = false)
        {
            Index = index;
            Value = value;
            IsEscaped = isEscaped;
        }

        public static PositionedChar[] GetFromString(string s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));

            PositionedChar[] ret = new PositionedChar[s.Length];

            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = new PositionedChar(i, s[i]);
            }

            return ret;
        }

        public static PositionedChar Escape(PositionedChar c)
        {
            if (c.Value != '\'' && c.Value != '"' && c.Value != '\\')
            {
                throw new UnescapedCharacterException($"Failed to escape character '{c.Value}' and position {c.Index}. "
                    + "Escaping is possible only for single/double quotes and backslash, other escaped "
                    + "characters are not supported.");
            }

            return new PositionedChar(c.Index, c.Value, true);
        }

        public override string ToString() => $"{Value} at {Index}";
    }
}
