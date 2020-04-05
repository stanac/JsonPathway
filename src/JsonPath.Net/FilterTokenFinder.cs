using System;
using System.Collections.Generic;
using System.Linq;

namespace JsonPath.Net
{
    internal static class FilterTokenFinder
    {
        public static IReadOnlyList<Token> FindAndReplaceFilterExpressionTokens(IReadOnlyList<Token> tokens)
        {
            List<(int start, int end)> indexes = new List<(int, int)>();

            for (int i = 0; i < tokens.Count - 4; i++)
            {
                if (tokens[i] is OpenStringToken && tokens[i+1].IsCharToken('?') && tokens[i+2].IsCharToken('('))
                {
                    int start = tokens[i].Index;

                    var closing = tokens.FirstOrDefault(x => x.Index > start && x.IsCharToken(')'));
                    if (closing == null)
                    {
                        throw new ArgumentException($"Open parentheses at index {tokens[i + 1].Index} is missing closing parentheses");
                    }
                    
                    var next = tokens.FirstOrDefault(x => x is CloseStringToken && x.Index > closing.Index);
                    if (next == null)
                    {
                        throw new ArgumentException($"Open square bracket at position {tokens[i].Index} is missing closing square bracket");
                    }

                    indexes.Add((tokens[i].Index, next.Index));
                    i += 2;
                }
            }

            if (indexes.Count == 0) return tokens;

            List<Token[]> tokenGroups = indexes.Select(
                x => tokens
                    .Where(t => t.Index >= x.start && t.Index <= x.end)
                    .ToArray()
                ).ToList();

            List<FilterExpressionToken> filterTokens = tokenGroups.Select(x => new FilterExpressionToken(x, x.Min(y => y.Index), x.Max(y => y.Index))).ToList();

            List<Token> retTokens = new List<Token>();
            
            foreach (var t in tokens)
            {
                var intersecting = filterTokens.FirstOrDefault(x => x.Intersects(t.Index));

                if (intersecting != null)
                {
                    if (!retTokens.Contains(intersecting))
                    {
                        retTokens.Add(intersecting);
                    }
                }
                else
                {
                    retTokens.Add(t);
                }
            }

            return retTokens;
        }
    }
}
