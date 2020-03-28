using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPath.Net
{
    /// <summary>
    /// Class used in parsing of JsonValuePath
    /// </summary>
    internal struct PositionedChar
    {
        public char Char { get; }
        public int Index { get; }
        public bool IsEscaped { get; }
        public bool IsEscapeChar { get; }

        public PositionedChar(char c, int index)
        {
            Char = c;
            Index = index;
            IsEscapeChar = false;
            IsEscaped = false;
        }

        private PositionedChar(char c, int index, bool isEscaped)
        {
            Char = c;
            Index = index;
            IsEscapeChar = false;
            IsEscaped = isEscaped;
        }

        private PositionedChar(int index, bool isEscape)
        {
            Char = char.MinValue;
            Index = index;
            IsEscapeChar = isEscape;
            IsEscaped = false;
        }

        private static PositionedChar CreateEscapeChar(int index) => new PositionedChar(index, true);

        private static PositionedChar CreateEscapedChar(char c, int index) => new PositionedChar(c, index, true);

        private PositionedChar MakeEscaped() => CreateEscapedChar(Char, Index);

        public static PositionedChar[] CreateFromString(string s)
        {
            if (s is null) throw new ArgumentNullException(nameof(s));

            PositionedChar[] result = new PositionedChar[s.Length];

            for (int i = 0; i < s.Length; i++)
            {
                result[i] = new PositionedChar(s[i], i);
            }

            return result;
        }

        public static PositionedChar[] ReplaceEscapedChars(PositionedChar[] chars)
        {
            List<PositionedChar> ret = new List<PositionedChar>();

            foreach (var c in chars)
            {
                if (ret.Any() && ret.Last().IsEscapeChar)
                {
                    bool replaceChar =
                        c.Char == '\''
                        || c.Char == '\"'
                        || c.Char == '\\';

                    if (replaceChar)
                    {
                        ret.RemoveAt(ret.Count - 1);
                        ret.Add(c.MakeEscaped());
                    }
                    else
                    {
                        throw new ArgumentException($"Unexpected escaped character {c.Char} at position {c.Index}. Escaping is supported for backslash, single quote and double quote");
                    }
                }
                else if (c.Char == '\\')
                {
                    ret.Add(CreateEscapeChar(c.Index));
                }
                else
                {
                    ret.Add(c);
                }
            }

            if (ret.Any() && ret.Last().IsEscapeChar)
            {
                throw new ArgumentException($"Unexpected escape character at position {ret.Last().Index}");
            }

            return ret.ToArray();
        }

        public static string CreateString(PositionedChar[] chars) => new string(chars.Select(x => x.Char).ToArray());

        public override string ToString() => $"{(IsEscapeChar ? "ESCAPE " : "")}{nameof(PositionedChar)} {(IsEscaped ? "ESCAPED " : " ")}{Char} at {Index}";
    }
}
