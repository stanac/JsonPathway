using System;
using System.Collections.Generic;

namespace JsonPath.Net
{
    internal static class SubStringFinder
    {
        public static IEnumerable<SubString> FindStrings(string path)
        {
            if (path is null) throw new ArgumentNullException(nameof(path));

            var chars = PositionedChar.CreateFromString(path);
            chars = PositionedChar.ReplaceEscapedChars(chars);

            int startIndex = -1;
            char startChar = char.MinValue;

            const char singleQuote = '\'';
            const char doubleQuote = '"';

            foreach (var c in chars)
            {
                if (!c.IsEscaped && (c.Char == singleQuote || c.Char == doubleQuote))
                {
                    if (startIndex == -1)
                    {
                        startIndex = c.Index;
                        startChar = c.Char;
                    }
                    else if (c.Char == startChar)
                    {
                        yield return SubString.CreateFromPositionedChars(chars, startIndex, c.Index, true);

                        startIndex = -1;
                        startChar = char.MinValue;
                    }
                }
            }
        }
    }
}
