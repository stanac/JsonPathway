using System.Text.Json;

namespace JsonPathway.Internal
{
    internal static class JsonElementHelper
    {
        public static JsonElement CreateNumber(int number)
        {
            return JsonDocument.Parse(JsonSerializer.Serialize(number)).RootElement;
        }
    }
}
