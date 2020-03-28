using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPath.Net
{
    public static class JsonValuePathParser
    {
        public const string ArrayAccessPrefix = "ArrayAccess_01e70be1cda4ZWIYOPQc62a8125f4e5fdbc3f3_";
        public const string ArrayAccessLast = ArrayAccessPrefix + "last";
        public const string ArrayAccessAny = ArrayAccessPrefix + "any";
        public const string ArrayAccessNone = ArrayAccessPrefix + "none";

        public static IReadOnlyList<string> GetPathParts(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Value not provided");

            IReadOnlyList<SecondLeveToken> tokens = Token.GetSecondLevelTokens(path);
            Token.EnsureSecondLevelTokensAreValid(tokens);

            return tokens.Where(x => x is PathToken)
                .Cast<PathToken>()
                .Where(x => !x.IsUnEscapedEmptyPath)
                .Select(x => x.GetPathVariableName())
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
