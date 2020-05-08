using JsonPathway.Internal;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JsonPathway
{
    public static class JsonPath
    {
        /// <summary>
        /// Executes JSONPath and returns matching elements.
        /// <br/><br/>
        /// In case when same jsonPathExpression needs
        /// to be used multiple times it's faster to call <see cref="ExpressionList.TokenizeAndParse(string)"/>
        /// and reuse <see cref="ExpressionList"/> returned by <see cref="ExpressionList.TokenizeAndParse(string)"/>.
        /// <br/><br/>
        /// <see cref="ExpressionList"/> has <see cref="ExpressionList.SerializerToJson"/> and
        /// <see cref="ExpressionList.DeserializeFromJson(string)"/>. Deserializing expression list from JSON
        /// is a lot faster than parsing expression again.
        /// </summary>
        /// <param name="jsonPathExpression">string representation of JsonPath expression</param>
        /// <param name="json">JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, string json)
        {
            JsonDocument doc = JsonDocument.Parse(json);
            return ExecutePath(jsonPathExpression, doc);
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements.
        /// <br/><br/>
        /// In case when same jsonPathExpression needs
        /// to be used multiple times it's faster to call <see cref="ExpressionList.TokenizeAndParse(string)"/>
        /// and reuse <see cref="ExpressionList"/> returned by <see cref="ExpressionList.TokenizeAndParse(string)"/>.
        /// <br/> <br/>
        /// <see cref="ExpressionList"/> has <see cref="ExpressionList.SerializerToJson"/> and
        /// <see cref="ExpressionList.DeserializeFromJson(string)"/>. Deserializing expression list from JSON
        /// is a lot faster than parsing expression again.
        /// </summary>
        /// <param name="jsonPathExpression">JsonPath expression</param>
        /// <param name="doc">Parsed JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonDocument doc)
        {
            var tokens = Tokenizer.Tokenize(jsonPathExpression);
            var exprList = ExpressionList.Parse(tokens);
            return ExecutePath(exprList, doc);
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements
        /// </summary>
        /// <param name="jsonPathExpression">Parsed JsonPath expression</param>
        /// <param name="json">JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, string json)
        {
            JsonDocument doc = JsonDocument.Parse(json);
            return ExecutePath(jsonPathExpression, doc);
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements
        /// </summary>
        /// <param name="jsonPathExpression">Parsed JsonPath expression</param>
        /// <param doc="json">Parse JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, JsonDocument doc)
        {
            try
            {
                return Interpreter.Execute(jsonPathExpression, doc);
            }
            catch (JsonPathwayException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InternalJsonPathwayException("Unexpected internal exception", ex);
            }
        }
    }
}
