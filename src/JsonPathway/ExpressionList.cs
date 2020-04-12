using JsonPathway.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

namespace JsonPathway
{
    public class ExpressionList: IReadOnlyList<Expression>
    {
        private readonly List<Expression> _expressions = new List<Expression>();

        private ExpressionList(IReadOnlyList<Token> tokens)
        {
            _expressions.AddRange(Parser.Parse(tokens));
        }

        public Expression this[int index] => _expressions[index];

        public int Count => _expressions.Count;

        internal static ExpressionList Parse(IReadOnlyList<Token> tokens) => new ExpressionList(tokens);

        internal static ExpressionList TokenizeAndParse(string jsonPathExpression)
        {
            var tokens = Tokenizer.Tokenize(jsonPathExpression);
            return Parse(tokens);
        }

        public IEnumerator<Expression> GetEnumerator()
        {
            foreach (var expr in _expressions) yield return expr;
        }

        public string SerializerToJson()
        {
            throw new NotImplementedException();
        }

        public static ExpressionList DeserializeFromJson(string json)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
