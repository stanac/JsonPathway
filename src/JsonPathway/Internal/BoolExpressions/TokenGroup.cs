using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPathway.Internal.BoolExpressions
{
    internal class TokenGroup
    {
        public TokenGroup(IReadOnlyList<Token> tokens)
        {
            TokensAndGroups = GetGroups(tokens);
        }

        public TokenGroup(IReadOnlyList<Either<Token, TokenGroup>> tokensAndGroups)
        {
            TokensAndGroups = tokensAndGroups ?? throw new ArgumentNullException(nameof(tokensAndGroups));
        }

        public IReadOnlyList<Either<Token, TokenGroup>> TokensAndGroups { get; }

        public static IReadOnlyList<Either<Token, TokenGroup>> GetGroups(IReadOnlyList<Token> tokens)
        {
            List<Either<Token, TokenGroup>> ret = new List<Either<Token, TokenGroup>>();

            int openCount = 0;
            int startIndex = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];

                if (t.IsSymbolToken('('))
                {
                    openCount++;
                    if (openCount == 1)
                    {
                        startIndex = i;
                    }
                }
                else if (t.IsSymbolToken(')'))
                {
                    openCount--;
                    if (openCount < 0) throw new UnexpectedTokenException($") at position {t.StartIndex} doesn't have opening (");

                    if (openCount == 0)
                    {
                        List<Token> groupTokens = GetSubCollection(tokens, startIndex + 1, i).ToList();

                        ret.Add(new Either<Token, TokenGroup>(new TokenGroup(groupTokens)));
                    }
                }
                else if (openCount == 0)
                {
                    ret.Add(new Either<Token, TokenGroup>(t));
                }
            }

            return ret;
        }

        private static IEnumerable<T> GetSubCollection<T>(IReadOnlyList<T> list, int startInclusive, int endExclusive)
        {
            for (int i = startInclusive; i < endExclusive; i++)
            {
                yield return list[i];
            }
        }
    }
}
