using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace JsonPathway.Internal
{
    internal static class TokenReaderHelpers
    {
        /// <summary>
        /// Reads all tokens
        /// </summary>
        /// <param name="chars">input</param>
        public static IReadOnlyList<Token> ReadTokens(PositionedChar[] chars) => ReadTokensInternal(chars).ToList();

        private static IEnumerable<Token> ReadTokensInternal(PositionedChar[] chars)
        {
            int index = 0;

            while (index < chars.Length)
            {
                yield return ReadToken(chars, ref index);
            }
        }

        private static Token ReadToken(PositionedChar[] chars, ref int index)
        {
            if (TryReadWhiteSpaceToken(chars, index, out WhiteSpaceToken whiteSpaceToken))
            {
                index++;
                return whiteSpaceToken;
            }

            if (TryReadSymbolToken(chars, index, out SymbolToken symbolToken))
            {
                index++;
                return symbolToken;
            }

            if (TryReadNumberToken(chars, ref index, out NumberToken numberToken))
            {
                return numberToken;
            }

            if (TryReadBoolToken(chars, ref index, out BoolToken boolToken))
            {
                return boolToken;
            }

            if (TryReadPropertyToken(chars, ref index, out PropertyToken propToken))
            {
                return numberToken;
            }

            throw new UnrecognizedCharSequence(chars[index]);
        }

        private static bool TryReadWhiteSpaceToken(PositionedChar[] chars, int index, out WhiteSpaceToken token)
        {
            if (char.IsWhiteSpace(chars[index].Value))
            {
                token = new WhiteSpaceToken(index);
                return true;
            }

            token = null;
            return false;
        }

        private static bool TryReadSymbolToken(PositionedChar[] chars, int index, out SymbolToken token)
        {
            var c = chars[index].Value;

            if (char.IsSymbol(c) && c != '$' && c != '_' && c != '\'' && c != '"')
            {
                token = new SymbolToken(index, c);
                return true;
            }

            token = null;
            return false;
        }

        private static bool TryReadNumberToken(PositionedChar[] chars, ref int index, out NumberToken token)
        {
            int startIndex = index;
            List<PositionedChar> tokenChars = new List<PositionedChar>();

            bool read;
            char c;

            do
            {
                c = chars[index].Value;
                read = char.IsDigit(c) || c == '.';

                if (read)
                {
                    tokenChars.Add(chars[index]);
                    index++;
                }
            }
            while (read);

            if (!tokenChars.Any() || (tokenChars.Count == 1 && tokenChars[0].Value == '.'))
            {
                token = null;
                return false;
            }

            string readString = PositionedChar.CreateString(tokenChars);
            
            if (double.TryParse(readString, NumberToken.AllowedStyle, CultureInfo.InvariantCulture, out double d))
            {
                token = new NumberToken(startIndex, index - 1, d);
                return true;
            }

            throw new UnrecognizedCharSequence($"Failed to create token from number starting at {startIndex}");
        }

        private static bool TryReadBoolToken(PositionedChar[] chars, ref int index, out BoolToken token)
        {
            int startIndex = index;

            if (TryReadPropertyToken(chars, ref index, out PropertyToken t) && bool.TryParse(t.StringValue, out bool b))
            {
                token = new BoolToken(startIndex, index, b);
                return true;
            }

            index = startIndex;
            token = null;
            return false;
        }

        private static bool TryReadPropertyToken(PositionedChar[] chars, ref int index, out PropertyToken token)
        {
            
        }
    }
}
