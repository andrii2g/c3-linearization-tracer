using System.Text.Json;
using C3Tracer.Cli.Commands;
using Xunit;

namespace C3Tracer.Tests;

public class FixtureTests
{
    public static IEnumerable<object[]> Cases()
    {
        using var json = JsonDocument.Parse(File.ReadAllText(TestPaths.Fixture("expected.json")));
        return json.RootElement.GetProperty("cases").EnumerateArray().Select(c => new object[] { c.GetProperty("file").GetString() ?? "", c.GetRawText() }).ToArray();
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public async Task EveryFixtureMatchesIndependentContract(string file, string expectedJson)
    {
        using var temp = new TempDirectory();
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = await CliApplication.RunAsync(["trace", TestPaths.Fixture(file), "--json", temp.File("report.json")], output, error);
        using var expected = JsonDocument.Parse(expectedJson);
        using var actual = JsonDocument.Parse(await File.ReadAllTextAsync(temp.File("report.json")));
        var e = expected.RootElement;
        var a = actual.RootElement;
        string? status = e.GetProperty("status").GetString();
        Assert.Equal(status, a.GetProperty("status").GetString());
        Assert.Equal(e.GetProperty("target").GetString(), a.GetProperty("target").GetString());
        Assert.Equal(status switch { "success" => 0, "invalidInput" => 2, _ => 3 }, exit);
        if (status == "success") Assert.Equal(Strings(e.GetProperty("mro")), Strings(a.GetProperty("mro")));
        else
        {
            Assert.Equal(JsonValueKind.Null, a.GetProperty("mro").ValueKind);
            if (status == "inconsistent")
            {
                Assert.Equal(e.GetProperty("failingClass").GetString(), a.GetProperty("failure").GetProperty("failingClass").GetString());
                Assert.Equal(Strings(e.GetProperty("partialPrefix")), Strings(a.GetProperty("failure").GetProperty("partialPrefix")));
                Assert.Empty(error.ToString());
            }
            else Assert.Contains(a.GetProperty("diagnostics").EnumerateArray(), d => d.GetProperty("code").GetString() == e.GetProperty("diagnosticCode").GetString());
        }
        Assert.Equal(new[] { "schemaVersion", "command", "status", "target", "graph", "mro", "classResults", "diagnostics", "failure", "metrics", "trace" }, a.EnumerateObject().Select(p => p.Name));
    }

    private static string?[] Strings(JsonElement a) => a.EnumerateArray().Select(x => x.GetString()).ToArray();
}
