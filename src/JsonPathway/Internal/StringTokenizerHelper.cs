using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    /// <summary>
    /// Gets collection of <see cref="StringToken"/> from <see cref="string"/>
    /// </summary>
    internal static class StringTokenizerHelper
    {
        public static IReadOnlyList<StringToken> GetStringTokens(string s) => GetStringTokensInternal(s).ToList();

        private static IEnumerable<StringToken> GetStringTokensInternal(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));

            PositionedChar[] chars = PositionedChar.GetFromString(s);

            PositionedCharStringList currentString = new PositionedCharStringList();

            int start = int.MinValue;
            const char singleQuote = '\'';
            const char doubleQuote = '"';
            char openQuote = char.MinValue;

            for (int i = 0; i < chars.Length; i++)
            {
                PositionedChar c = chars[i];

                if (start >= 0)
                {
                    if (c.Value == openQuote && !currentString.IsLastEscapeSymbol)
                    {
                        string value = currentString.ToString();
                        currentString = new PositionedCharStringList();
                        openQuote = char.MinValue;
                        start = int.MinValue;
                        yield return new StringToken(start, i, value);
                    }
                    else
                    {
                        currentString.Add(c);
                    }
                }
                else if (c.Value == singleQuote)
                {
                    start = i;
                    openQuote = singleQuote;
                }
                else if (c.Value == doubleQuote)
                {
                    start = i;
                    openQuote = doubleQuote;
                }
            }

            if (start != int.MinValue) throw new UnclosedStringException(openQuote, start);
        }
    }
}
