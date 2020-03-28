using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPath.Net
{
    public static class JsonPathParser
    {
        public static IReadOnlyList<PathElement> GetPathParts(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value not provided");

            IReadOnlyList<SecondLevelToken> tokens = Token.GetSecondLevelTokens(path);
            Token.EnsureSecondLevelTokensAreValid(tokens);

            return tokens.Where(x => x is PathToken)
                .Cast<PathToken>()
                .Where(x => !x.IsUnEscapedEmptyPath)
                .Select(x => x.GetPathElement())
                .ToList();
        }

        public static bool IsValid(string path, out string error)
        {
            try
            {
                GetPathParts(path);
                error = null;
                return true;
            }
            catch (ArgumentException ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}
