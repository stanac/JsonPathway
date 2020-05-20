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
            tokens = ConvertWildcardTokens(tokens.ToList());
            tokens = ConvertMultiplePropertyTokens(tokens);
            tokens = ConvertArrayAccessTokens(tokens);

            if (tokens.Count > 2 && tokens.First() is PropertyToken pt2 && pt2.StringValue == "$"
                && tokens[1].IsSymbolToken('.') && tokens[2].IsPropertyToken())
            {
                tokens = tokens.Skip(2).ToList();
            }
            else if (tokens.Count > 1 && tokens.First() is PropertyToken pt && pt.StringValue == "$")
            {
                tokens = tokens.Skip(1).ToArray();
            }

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
                if (tokens[i].IsSymbolToken('[') && tokens[i + 1].IsStringToken() && tokens[i + 2].IsSymbolToken(']'))
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
                if (tokens[i].IsSymbolToken('[') && tokens[i+1].IsSymbolToken('?') && tokens[i+2].IsSymbolToken('('))
                {
                    var closedSquareBracket = tokens.FirstOrDefault(x => x.StartIndex > tokens[i + 1].StartIndex && x.IsSymbolToken(']'));

                    if (closedSquareBracket != null)
                    {
                        var previousToken = tokens[tokens.IndexOf(closedSquareBracket) - 1];
                        if (previousToken.IsSymbolToken(')'))
                        {
                            open = i;
                            closed = tokens.IndexOf(closedSquareBracket);
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

        private static List<Token> ConvertWildcardTokens(IReadOnlyList<Token> tokens)
        {
            List<Token> ret = tokens
                .Select(x =>
                {
                    if (x.IsSymbolToken('*')) return new ChildPropertiesToken(x.StartIndex);
                    return x;
                })
                .ToList();

            for (int i = ret.Count - 1; i > 0; i--) // i > 0 is intentional
            {
                if (ret[i].IsSymbolToken('.') && ret[i - 1].IsSymbolToken('.'))
                {
                    var toRemove1 = ret[i - 1];
                    var toRemove2 = ret[i];

                    ret.Insert(i - 1, new RecursivePropertiesToken(toRemove1.StartIndex, toRemove2.StartIndex));
                    ret.Remove(toRemove1);
                    ret.Remove(toRemove2);
                }
            }

            for (int i = ret.Count - 1; i > 1; i--) // i > 1 is intentional
            {
                if (i < ret.Count && ret[i].IsSymbolToken(']') && ret[i - 1].IsChildPropertiesToken() && ret[i - 2].IsSymbolToken('['))
                {
                    var toRemove1 = ret[i - 2];
                    var toRemove2 = ret[i - 1];
                    var toRemove3 = ret[i];

                    ret.Insert(i - 2, new AllArrayElementsToken(toRemove1.StartIndex, toRemove3.StartIndex));
                    ret.Remove(toRemove1);
                    ret.Remove(toRemove2);
                    ret.Remove(toRemove3);
                }
            }

            return ret;
        }

        private static IReadOnlyList<Token> ConvertMultiplePropertyTokens(IReadOnlyList<Token> tokens)
        {
            List<Token> ret = tokens.ToList();
            List<(int start, int end)> openCloseIndexes = FindOpenClosedTokens(ret);

            List<MultiplePropertiesToken> converted = new List<MultiplePropertiesToken>();

            foreach (var oc in openCloseIndexes)
            {
                var tokensToConvert = ret.Where(x => x.StartIndex > oc.start && x.StartIndex < oc.end).ToList();

                if (tokensToConvert.All(x => x.IsSymbolToken(',') || x.IsStringToken()))
                {
                    ValidateMultiPropertyTokenOrder(tokensToConvert);
                    var stringTokens = tokensToConvert.Where(x => x.IsStringToken()).Select(x => x.CastToStringToken()).ToArray();

                    converted.Add(new MultiplePropertiesToken(oc.start, oc.end, stringTokens));
                }
            }

            List<Token> ret2 = new List<Token>();

            foreach (var token in tokens)
            {
                var intersecting = converted.FirstOrDefault(x => x.IntersectesInclusive(token.StartIndex));

                if (intersecting != null)
                {
                    if (!ret2.Contains(intersecting)) ret2.Add(intersecting);
                }
                else
                {
                    ret2.Add(token);
                }
            }

            return ret2;
        }

        private static IReadOnlyList<Token> ConvertArrayAccessTokens(IReadOnlyList<Token> tokens)
        {
            List<Token> ret = tokens.ToList();
            List<(int start, int end)> openCloseIndexes = FindOpenClosedTokens(ret);

            var arrayAccessTokens = openCloseIndexes.Select(x =>
            {
                IEnumerable<string> toConvert = ret
                    .Where(t => t.StartIndex >= x.start && t.StartIndex <= x.end)
                    .Select(t => t.StringValue);

                string value = string.Join("", toConvert);

                return new ArrayElementsToken(x.start, x.end, value);
            })
            .ToList();

            List<Token> retList = new List<Token>();

            foreach (var t in ret)
            {
                var intersecting = arrayAccessTokens.FirstOrDefault(x => x.IntersectesInclusive(t.StartIndex));

                if (intersecting != null)
                {
                    if (!retList.Contains(intersecting)) retList.Add(intersecting);
                }
                else
                {
                    retList.Add(t);
                }
            }

            return retList;
        }

        private static List<(int start, int end)> FindOpenClosedTokens(IReadOnlyList<Token> ret)
        {
            var openTokens = ret.Where(x => x.IsSymbolToken('[')).ToList();

            List<(int start, int end)> openCloseIndexes = new List<(int, int)>();

            foreach (var ot in openTokens)
            {
                var ct = ret.FirstOrDefault(x => x.StartIndex > ot.StartIndex && x.IsSymbolToken(']'));

                if (ct != null && ret.Any(x => x.StartIndex > ot.StartIndex && x.StartIndex < ct.StartIndex
                                           && (x.IsNumberToken() || x.IsSymbolToken(',') || x.IsSymbolToken(':'))
                                        )
                   )
                {
                    openCloseIndexes.Add((ot.StartIndex, ct.StartIndex));
                }
            }

            return openCloseIndexes;
        }

        private static void ValidateMultiPropertyTokenOrder(List<Token> tokens)
        {
            int startIndex = tokens.First().StartIndex;
            string error = $"Failed to convert strings to multiple properties token starting at {startIndex}";

            if (tokens.Any(x => !x.IsSymbolToken(',') && !x.IsStringToken()))
                throw new UnrecognizedCharSequence(error);

            if (tokens.First().IsSymbolToken(',') || tokens.Last().IsSymbolToken(','))
                throw new UnrecognizedCharSequence(error);

            for (int i = 0; i < tokens.Count - 1; i++)
            {
                if (tokens[i].GetType() == tokens[i+1].GetType())
                    throw new UnrecognizedCharSequence(error);
            }
        }
    }
}
