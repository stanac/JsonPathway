using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace JsonPathway.Tests
{
    public static class TestDataLoader
    {
        public static string AbcArray() => LoadFile("AbcArray.json");

        public static string Store() => LoadFile("Store.json"); // source: https://goessner.net/articles/JsonPath/

        public static string BooksObject() => LoadFile("BooksObject.json"); // modified, source: https://goessner.net/articles/JsonPath/

        public static string BooksWithNulls() => LoadFile("BooksWithNulls.json"); // modified, source: https://goessner.net/articles/JsonPath/

        public static string LoadFile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value not set", nameof(name));
            }

            Assembly asm = typeof(TestDataLoader).Assembly;

            string[] names = asm.GetManifestResourceNames();

            string foundName = names.FirstOrDefault(x => x.EndsWith("." + name));

            if (foundName != null)
            {
                using (Stream s = asm.GetManifestResourceStream(foundName))
                using (StreamReader reader = new StreamReader(s))
                {
                    return reader.ReadToEnd();
                }
            }

            throw new InvalidOperationException($"Test file {name} not found");
        }
    }
}
