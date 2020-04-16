using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    /// <summary>
    /// Tokenizer, converts string to tokens
    /// </summary>
    public static class Tokenizer
    {
        /// <summary>
        /// Splits input into tokens
        /// </summary>
        /// <param name="input">Input to be tokenized</param>
        /// <returns>Collection of tokens</returns>
        public static IReadOnlyList<Token> Tokenize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("Value not set", nameof(input));
            }

            IReadOnlyList<Token> tokens = GetTokensInner(input);
            tokens = RemoveWhiteSpaceTokens(tokens);
            tokens = ConvertStringTokensToPropertyTokens(tokens);
            tokens = ConvertTokensToFilterTokens(tokens.ToList());


            return tokens;
        }

        /// <summary>
        /// Gets all tokens (including white space tokens, doesn't convert escaped property values like ["abc"] to property tokens
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Tokens</returns>
        private static IReadOnlyList<Token> GetTokensInner(string s)
        {
            IReadOnlyList<Either<StringToken, PositionedChar[]>> stringsAndStringTokens = SplitStrings(s);

            List<Token> tokens = new List<Token>();

            foreach (var value in stringsAndStringTokens)
            {
                if (value.Is<StringToken>())
                {
                    tokens.Add(value.Get<StringToken>());
                }
                else
                {
                    tokens.AddRange(TokenReaderHelpers.ReadTokens(value.Get<PositionedChar[]>()));
                }
            }

            return tokens;
        }

        /// <summary>
        /// Splits input into positioned chars and strings
        /// </summary>
        /// <param name="s">Input</param>
        /// <returns>List containing elements that are either PositionedChar[] or StringToken</returns>
        private static List<Either<StringToken, PositionedChar[]>> SplitStrings(string s)
        {
            IReadOnlyList<StringToken> stringTokens = StringTokenizerHelper.GetStringTokens(s);

            List<Either<StringToken, PositionedChar[]>> values = new List<Either<StringToken, PositionedChar[]>>();

            bool ValuesContains(StringToken st)
            {
                return values.Any(x => x.Is<StringToken>() && x.Get<StringToken>() == st);
            }

            List<PositionedChar> currentString = new List<PositionedChar>();

            for (int i = 0; i < s.Length; i++)
            {
                var intersectingToken = stringTokens.FirstOrDefault(x => x.IntersectesInclusive(i));
                if (intersectingToken != null)
                {
                    if (currentString.Any())
                    {
                        values.Add(new Either<StringToken, PositionedChar[]>(currentString.ToArray()));
                        currentString.Clear();
                    }
                    
                    if (!ValuesContains(intersectingToken))
                    {
                        values.Add(new Either<StringToken, PositionedChar[]>(intersectingToken));
                    }
                }
                else
                {
                    currentString.Add(new PositionedChar(i, s[i]));
                }
            }

            if (currentString.Any())
            {
                values.Add(new Either<StringToken, PositionedChar[]>(currentString.ToArray()));
            }

            return values;
        }

        /// <summary>
        /// Removes white space tokens from the collection
        /// </summary>
        /// <param name="tokens">Token collection from which to remove white space tokens</param>
        /// <returns>Token collection without white space</returns>
        private static IReadOnlyList<Token> RemoveWhiteSpaceTokens(IReadOnlyList<Token> tokens) => tokens.Where(t => !t.IsWhiteSpaceToken()).ToList();

        /// <summary>
        /// Converts groups of tokens that represents property to property tokens (e.g. ["abc"])
        /// </summary>
        /// <param name="tokens">Input tokens</param>
        /// <returns>Tokens</returns>
        private static IReadOnlyList<Token> ConvertStringTokensToPropertyTokens(IReadOnlyList<Token> tokens)
        {
            List<PropertyToken> propTokens = new List<PropertyToken>();

            for (int i = 0; i < tokens.Count - 2; i++)
            {
                if (tokens[i].IsSymbolTokenOpenSquareBracket() && tokens[i + 1].IsStringToken() && tokens[i + 2].IsSymbolTokenCloseSquareBracket())
                {
                    propTokens.Add(new PropertyToken(tokens[i].StartIndex, tokens[i + 2].StartIndex, tokens[i + 1].StringValue, true));
                }
            }

            List<Token> ret = new List<Token>();
            
            foreach (var token in tokens)
            {
                var intersecting = propTokens.FirstOrDefault(x => x.IntersectesInclusive(token.StartIndex));
                if (intersecting != null)
                {
                    if (!ret.Contains(intersecting)) ret.Add(intersecting);
                }
                else
                {
                    ret.Add(token);
                }
            }

            return ret;
        }

        private static IReadOnlyList<Token> ConvertTokensToFilterTokens(List<Token> tokens)
        {
            bool converted;

            do
            {
                tokens = ConvertTokensToFilterTokens(tokens, out converted);
            }
            while (converted);

            return tokens;
        }

        private static List<Token> ConvertTokensToFilterTokens(List<Token> tokens, out bool converted)
        {
            // filter token looks like "[?( ... filterExpression ... )]"

            int open = 0;
            int closed = 0;

            for (int i = 0; i < tokens.Count - 4; i++)
            {
                if (tokens[i].IsSymbolTokenOpenSquareBracket() && tokens[i+1].IsSymbolTokenQuestionMark() && tokens[i+2].IsSymbolTokenOpenRoundBracket())
                {
                    var closedRoundBracket = tokens.First(x => x.StartIndex > tokens[i + 1].StartIndex && x.IsSymbolTokenCloseRoundBracket());

                    if (closedRoundBracket != null && closedRoundBracket != tokens.Last())
                    {
                        var nextToken = tokens[tokens.IndexOf(closedRoundBracket) + 1];
                        if (nextToken.IsSymbolTokenCloseSquareBracket())
                        {
                            open = i;
                            closed = tokens.IndexOf(nextToken);
                            break;
                        }
                    }
                }
            }

            if (open > 0 && closed > 0)
            {
                converted = true;

                List<Token> ret = new List<Token>();
                List<Token> toConvert = new List<Token>();
                
                for (int i = 0; i < tokens.Count; i++)
                {
                    if (i >= open && i <= closed)
                    {
                        toConvert.Add(tokens[i]);
                    }
                    else if (toConvert.Any())
                    {
                        ret.Add(CreateFilterToken(toConvert));
                        toConvert.Clear();
                    }
                    else
                    {
                        ret.Add(tokens[i]);
                    }
                }

                if (toConvert.Any())
                {
                    ret.Add(CreateFilterToken(toConvert));
                }

                return ret;
            }
            else
            {
                converted = false;
                return tokens;
            }
        }

        private static FilterToken CreateFilterToken(List<Token> tokens)
        {
            string value = string.Join("", tokens.Select(x => x.IsStringToken() ? x.CastToStringToken().GetQuotedValue() : x.StringValue));

            return new FilterToken(tokens.First().StartIndex, tokens.Last().StartIndex, value);
        }
    }
}
