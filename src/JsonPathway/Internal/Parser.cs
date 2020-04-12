using System;
using System.Collections.Generic;

namespace JsonPathway.Internal
{
    internal static class Parser
    {
        public static IEnumerable<Expression> Parse(IReadOnlyList<Token> tokens)
        {
            EnsureTokensAreValid(tokens);

            yield break;
        }

        private static void EnsureTokensAreValid(IReadOnlyList<Token> tokens)
        {

        }
    }
}
