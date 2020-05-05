﻿using System;
using System.IO;
using System.Linq;

namespace JsonPathway.Tests
{
    public static class TestDataLoader
    {
        public static string AbcArray() => LoadFile("AbcArray.json");

        public static string Store() => LoadFile("Store.json");

        private static string LoadFile(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Value not set", nameof(name));
            }

            var asm = typeof(TestDataLoader).Assembly;

            var names = asm.GetManifestResourceNames();

            if (!name.EndsWith(".json")) name += ".json";
            
            var foundName = names.FirstOrDefault(x => x.EndsWith("." + name));

            if (foundName != null)
            {
                using (var s = asm.GetManifestResourceStream(foundName))
                using (var reader = new StreamReader(s))
                {
                    return reader.ReadToEnd();
                }
            }

            throw new InvalidOperationException($"Test file {name} not found");
        }
    }
}
