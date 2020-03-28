using System;
using System.Collections.Generic;

namespace JsonPath.Net
{
    /// <summary>
    /// Class used in parsing of JsonValuePath
    /// </summary>
    internal class SubString
    {
        public string String { get; }
        public int StartIndexInclusive { get; }
        public int EndIndexInclusive { get; }

        public SubString(string s, int startIndex, int endIndexInclusive)
        {
            String = s ?? throw new ArgumentNullException(nameof(s));
            StartIndexInclusive = startIndex;
            EndIndexInclusive = endIndexInclusive;
        }

        public static SubString CreateFromPositionedChars(PositionedChar[] chars, int startIndexInclusive, int endIndexInclusive, bool skipQuotes)
        {
            List<char> ret = new List<char>();

            foreach (var c in chars)
            {
                if (c.Index >= startIndexInclusive && c.Index <= endIndexInclusive)
                {
                    if (skipQuotes && (c.Index == startIndexInclusive || c.Index == endIndexInclusive) && (c.Char == '\'' || c.Char == '"'))
                    {
                        continue;
                    }

                    ret.Add(c.Char);
                }
            }

            string s = new string(ret.ToArray());
            return new SubString(s, startIndexInclusive, endIndexInclusive);
        }

        public bool Intersects(int position)
        {
            return position >= StartIndexInclusive && position <= EndIndexInclusive;
        }

        public override string ToString() => $"SubString {String} at {StartIndexInclusive}-{EndIndexInclusive}";
    }
}
