using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal
{
    internal static class Parser
    {
        public static IEnumerable<Expression> Parse(IReadOnlyList<Token> tokens)
        {
            EnsureTokensAreValid(tokens.ToList());

            yield break;
        }

        private static void EnsureTokensAreValid(List<Token> tokens)
        {
            if (!tokens.Any()) return;

            Token unexpectedToken = tokens.FirstOrDefault(x => !x.IsFilterToken() && !x.IsSymbolToken('.') && !x.IsPropertyToken());
            if (unexpectedToken != null) throw new UnexpectedTokenException(unexpectedToken);

            if (tokens[0].IsSymbolToken('.')) throw new UnexpectedTokenException(tokens[0]);
            if (tokens.Last().IsSymbolToken('.')) throw new UnexpectedTokenException(tokens.Last());

            var pointTokens = tokens.Where(x => x.IsSymbolToken('.'));
            
            foreach (SymbolToken pt in pointTokens)
            {
                int index = tokens.IndexOf(pt);
                var next = tokens[index + 1];

                if (!next.IsPropertyToken()) throw new UnexpectedTokenException(next, "Expected property accessor");

                if (next.CastToPropertyToken().Escaped) throw new UnexpectedTokenException(next, "Expected unescaped property accessor");
            }

        }
    }
}
