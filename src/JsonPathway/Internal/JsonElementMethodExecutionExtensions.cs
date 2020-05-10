using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace JsonPathway.Internal
{
    public static class JsonElementMethodExecutionExtensions
    {
        public static bool TryExecuteStringMethod(this JsonElement e, string methodName, List<JsonElement> arguments, out JsonElement result)
        {
            try
            {
                result = e.ExecuteStringMethod(methodName, arguments);
                return true;
            }
            catch
            {
                result = JsonElementFactory.CreateNull();
                return false;
            }
        }

        public static JsonElement ExecuteStringMethod(this JsonElement e, string methodName, List<JsonElement> arguments)
        {
            if (e.ValueKind != JsonValueKind.String) throw new ArgumentException($"ExecuteStringMethod expected JsonElement of JsonValueKind.String but got JsonValueKind.{e.ValueKind}");

            if (ExecuteStringMethodToUpperOrToLowerIfMatched(e, methodName, arguments, out JsonElement result1))
            {
                return result1;
            }

            if (ExecuteStringStartsWithEndsWithOrContainsIfMatched(e, methodName, arguments, out JsonElement result2))
            {
                return result2;
            }

            throw new JsonPatwayMethodNotSupportedException($"Method {methodName} does not exist on string");
        }

        public static bool TryExecuteArrayMethod(this JsonElement e, string methodName, List<JsonElement> arguments, out JsonElement result)
        {
            try
            {
                result = e.ExecuteArrayMethod(methodName, arguments);
                return true;
            }
            catch
            {
                result = JsonElementFactory.CreateNull();
                return false;
            }
        }

        public static JsonElement ExecuteArrayMethod(this JsonElement e, string methodName, List<JsonElement> arguments)
        {
            if (e.ValueKind != JsonValueKind.Array) throw new ArgumentException($"ExecuteArrayMethod expected JsonElement of JsonValueKind.Array but got JsonValueKind.{e.ValueKind}");

            if (methodName != "contains")
            {
                throw new JsonPatwayMethodNotSupportedException($"Method {methodName} does not exist on array");
            }

            if (arguments.Count != 1)
            {
                throw new JsonPatwayMethodNotSupportedException("Method contains on array accepts one and only one argument");
            }

            bool result = e.EnumerateArray().Contains(arguments[0], JsonElementEqualityComparer.Default);

            return JsonElementFactory.CreateBool(result);
        }

        private static bool ExecuteStringMethodToUpperOrToLowerIfMatched(JsonElement e, string methodName, List<JsonElement> arguments, out JsonElement result)
        {
            bool isUpper = methodName == "toUpper" || methodName == "toUpperCase";
            bool isLower = methodName == "toLower" || methodName == "toLowerCase";

            if (isUpper || isLower)
            {
                if (arguments.Count > 0)
                {
                    throw new JsonPatwayMethodNotSupportedException($"Method string.{methodName} expected 0 arguments but got: {arguments.Count}");
                }

                string ret = e.GetString();
                ret = isUpper ? ret.ToUpper() : ret.ToLower();
                result = JsonElementFactory.CreateString(ret);
                return true;
            }

            result = JsonElementFactory.CreateNull();
            return false;
        }

        private static bool ExecuteStringStartsWithEndsWithOrContainsIfMatched(JsonElement e, string methodName, List<JsonElement> arguments, out JsonElement result)
        {
            if (methodName == "startsWith" || methodName == "endsWith" || methodName == "contains")
            {
                EnsureStartsWithEndsWithOrContainsArgumentsAreValid(methodName, arguments);

                string executedOn = e.GetString();
                string argument = arguments[0].GetString();

                if (arguments.Count == 2 && arguments[1].IsTruthy())
                {
                    executedOn = executedOn.ToLower();
                    argument = argument.ToLower();
                }

                bool resultBool;
                if (methodName == "startsWith") resultBool = executedOn.StartsWith(argument);
                else if (methodName == "endsWith") resultBool = executedOn.EndsWith(argument);
                else resultBool = executedOn.Contains(argument);

                result = JsonElementFactory.CreateBool(resultBool);
                return true;
            }

            result = JsonElementFactory.CreateNull();
            return false;
        }

        private static void EnsureStartsWithEndsWithOrContainsArgumentsAreValid(string methodName, List<JsonElement> arguments)
        {
            string exceptionMsg = $"string method {methodName} accepts arguments (value: string [, ignoreCase: boolean]) but got ";

            if (arguments.Count == 0)
            {
                throw new JsonPatwayMethodNotSupportedException(exceptionMsg + "no arguments");
            }

            string argTypes = string.Join(", ", arguments.Select(x => x.ValueKind));
            
            if (arguments.Count > 2)
            {
                throw new JsonPatwayMethodNotSupportedException($"{exceptionMsg}({argTypes})");
            }

            if (arguments.Count == 1 && arguments[0].ValueKind != JsonValueKind.String)
            {
                throw new JsonPatwayMethodNotSupportedException($"{exceptionMsg}({argTypes})");
            }
        }
    }
}
