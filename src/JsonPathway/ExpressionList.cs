using JsonPathway.Internal;
using System.Collections;
using System.Collections.Generic;

namespace JsonPathway
{
    public class ExpressionList: IReadOnlyList<JsonPathExpression>
    {
        private readonly List<JsonPathExpression> _expressions = new List<JsonPathExpression>();

        private ExpressionList(IReadOnlyList<Token> tokens)
        {
            _expressions.AddRange(Parser.Parse(tokens));

            foreach (var ex in _expressions)
            {
                if (ex is FilterExpression fe)
                {
                    fe.EnsureMethodNamesAreValid();
                }
            }
        }

        public JsonPathExpression this[int index] => _expressions[index];

        public int Count => _expressions.Count;

        internal static ExpressionList Parse(IReadOnlyList<Token> tokens) => new ExpressionList(tokens);

        internal static ExpressionList TokenizeAndParse(string jsonPathExpression)
        {
            var tokens = Tokenizer.Tokenize(jsonPathExpression);
            return Parse(tokens);
        }

        public IEnumerator<JsonPathExpression> GetEnumerator()
        {
            foreach (var expr in _expressions) yield return expr;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
