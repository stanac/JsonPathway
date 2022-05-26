using System;
using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace JsonPathway.Tests
{
    public class VersionLog
    {
        private readonly ITestOutputHelper _testOutput;

        public VersionLog(ITestOutputHelper testOutput)
        {
            _testOutput = testOutput;
        }

        [Fact]
        public void Log()
        {
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(typeof(JsonDocument).Assembly.Location);
            _testOutput.WriteLine($"Testing using: {info.FileVersion}");
        }
    }
}
