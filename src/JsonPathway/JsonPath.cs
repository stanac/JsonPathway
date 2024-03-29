﻿using JsonPathway.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
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
        /// </summary>
        /// <param name="jsonPathExpression">string representation of JsonPath expression</param>
        /// <param name="json">JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return ExecutePath(jsonPathExpression, doc);
            }
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements.
        /// <br/><br/>
        /// In case when same jsonPathExpression needs
        /// to be used multiple times it's faster to call <see cref="ExpressionList.TokenizeAndParse(string)"/>
        /// and reuse <see cref="ExpressionList"/> returned by <see cref="ExpressionList.TokenizeAndParse(string)"/>.
        /// </summary>
        /// <param name="jsonPathExpression">JsonPath expression</param>
        /// <param name="element">Element on which to execute path</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonElement element)
        {
            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(jsonPathExpression);
            ExpressionList exprList = ExpressionList.Parse(tokens);
            return ExecutePath(exprList, element).Select(x => x.Clone()).ToList();
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements.
        /// <br/><br/>
        /// In case when same jsonPathExpression needs
        /// to be used multiple times it's faster to call <see cref="ExpressionList.TokenizeAndParse(string)"/>
        /// and reuse <see cref="ExpressionList"/> returned by <see cref="ExpressionList.TokenizeAndParse(string)"/>.
        /// </summary>
        /// <param name="jsonPathExpression">JsonPath expression</param>
        /// <param name="doc">Parsed JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(string jsonPathExpression, JsonDocument doc)
        {
            IReadOnlyList<Token> tokens = Tokenizer.Tokenize(jsonPathExpression);
            ExpressionList exprList = ExpressionList.Parse(tokens);
            return ExecutePath(exprList, doc).Select(x => x.Clone()).ToList();
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements
        /// </summary>
        /// <param name="jsonPathExpression">Parsed JsonPath expression</param>
        /// <param name="json">JSON document</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, string json)
        {
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                return ExecutePath(jsonPathExpression, doc).Select(x => x.Clone()).ToList();
            }
        }

        /// <summary>
        /// Executes JSONPath and returns matching elements
        /// </summary>
        /// <param name="jsonPathExpression">Parsed JsonPath expression</param>
        /// <param doc="json">Parse JSON document</param>
        /// <param name="doc">JSON document</param>
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

        /// <summary>
        /// Executes JSONPath and returns matching elements
        /// </summary>
        /// <param name="jsonPathExpression">Parsed JsonPath expression</param>
        /// <param doc="json">Parse JSON document</param>
        /// <param name="element">JSON element on which to execute path</param>
        /// <returns>Matching JsonElements</returns>
        public static IReadOnlyList<JsonElement> ExecutePath(ExpressionList jsonPathExpression, JsonElement element)
        {
            try
            {
                return Interpreter.Execute(jsonPathExpression, element);
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

        /// <summary>
        /// Checks if parameter jsonPathExpression is valid JSONPath, returns true if it's valid
        /// </summary>
        /// <param name="jsonPathExpression">Expression string to check</param>
        /// <param name="error">Error message if any</param>
        /// <returns>True if provided string is valid JSONPath</returns>
        public static bool IsPathValid(string jsonPathExpression, out string error)
        {
            error = null;

            try
            {
                IReadOnlyList<Token> tokens = Tokenizer.Tokenize(jsonPathExpression);
                ExpressionList.Parse(tokens);
            }
            catch (JsonPathwayException ex)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                throw new InternalJsonPathwayException("Unexpected internal exception", ex);
            }

            return true;
        }
    }
}
