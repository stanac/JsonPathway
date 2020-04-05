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

            var tokens = GetTokensInternal(input);
            tokens = RemoveWhiteSpaceTokens(tokens);
            tokens = ConvertStringTokensToPropertyTokens(tokens);

            return tokens;
        }

        /// <summary>
        /// Gets all tokens (including white space tokens, doesn't convert escaped property values like ["abc"] to property tokens
        /// </summary>
        /// <param name="s"></param>
        /// <returns>Tokens</returns>
        private static IReadOnlyList<Token> GetTokensInternal(string s)
        {
            int index = 0;

            IReadOnlyList<StringToken> stringTokens = StringTokenizerHelper.GetStringTokens(s);
            

            while (index < s.Length)
            {

            }
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
                    propTokens.Add(new PropertyToken(tokens[i].StartIndex, tokens[i + 2].StartIndex, tokens[i + 1].StringValue));
                }
            }

            List<Token> ret = new List<Token>();
            
            foreach (var token in tokens)
            {
                var intersecting = propTokens.FirstOrDefault(x => x.IntesectesInclusive(token.StartIndex));
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

    }
}
