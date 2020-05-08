using System.Linq;

namespace JsonPathway.Tests
{
    public static class StringExtensions
    {
        public static string RemoveWhiteSpace(this string s)
        {
            if (s == null) return null;

            return new string(s.Where(c => !char.IsWhiteSpace(c)).ToArray());
        }
    }
}
