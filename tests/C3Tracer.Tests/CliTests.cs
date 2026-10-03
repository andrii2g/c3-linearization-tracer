using System.Globalization;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using C3Tracer.Cli.Commands;
using C3Tracer.Cli.Reporting;
using Xunit;

namespace C3Tracer.Tests;

public class CliTests
{
    private static async Task<(int Exit, string Out, string Error)> Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        int exit = await CliApplication.RunAsync(args, output, error);
        return (exit, output.ToString(), error.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("trace")]
    [InlineData("trace x --wat")]
    [InlineData("trace x --json")]
    [InlineData("trace x --json -")]
    [InlineData("trace x --json=a")]
    [InlineData("trace x --max-events 1.5")]
    [InlineData("trace x --max-events -1")]
    [InlineData("trace x --max-events 100001")]
    [InlineData("trace x --max-events 999999999999")]
    [InlineData("trace x --max-events 0 --no-trace")]
    [InlineData("trace x --no-trace --no-trace")]
    [InlineData("trace x --force")]
    [InlineData("trace x y")]
    [InlineData("validate x --target A")]
    [InlineData("validate x --svg a")]
    [InlineData("validate x --max-events 1")]
    [InlineData("validate x --no-trace")]
    public async Task UsageErrors(string command)
    {
        var result = await Run(command.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        Assert.Equal(2, result.Exit);
        Assert.Empty(result.Out);
        Assert.Contains("c3-tracer", result.Error);
    }

    [Theory]
    [InlineData("--help")]
    [InlineData("--version")]
    public async Task InformationalCommands(string command)
    {
        var r = await Run(command);
        Assert.Equal(0, r.Exit);
        Assert.Empty(r.Error);
        Assert.Contains("c3-tracer", r.Out);
    }

    [Fact]
    public async Task TargetOverrideOptionsBeforeInputAndValidateSemantics()
    {
        var r = await Run("trace", "--target", "O", TestPaths.Fixture("unknown-target.json"), "--no-trace");
        Assert.Equal(0, r.Exit);
        Assert.Contains("Target MRO: O", r.Out);
        Assert.DoesNotContain("Step ", r.Out);
        r = await Run("validate", TestPaths.Fixture("missing-target.json"));
        Assert.Equal((0, "Graph valid; C3 not evaluated.\n", ""), r);
        Assert.Equal(2, (await Run("validate", TestPaths.Fixture("unknown-target.json"))).Exit);
        Assert.Equal(0, (await Run("validate", TestPaths.Fixture("inconsistent-order.json"))).Exit);
        Assert.Equal(2, (await Run("trace", TestPaths.Fixture("diamond.json"), "--target", "bad name")).Exit);
    }

    [Fact]
    public async Task StructuralDiagnosticsIncludeTargetSyntaxBeforeCycleTraversal()
    {
        using var temp = new TempDirectory();
        string path = temp.File("invalid.json"), json = temp.File("report.json");
        File.WriteAllText(path, """{"target":"bad target","classes":[{"name":"bad class","bases":["Missing"]},{"name":"Cycle","bases":["Cycle"]}]}""");
        var r = await Run("trace", path, "--target", "Cycle", "--json", json);
        Assert.Equal(2, r.Exit);
        using var report = JsonDocument.Parse(File.ReadAllText(json));
        var diagnostics = report.RootElement.GetProperty("diagnostics").EnumerateArray().ToArray();
        Assert.Equal(new[] { "MissingBase", "InvalidName", "InvalidName" }, diagnostics.Select(d => d.GetProperty("code").GetString()));
        Assert.Equal(new[] { "/classes/0/bases/0", "/classes/0/name", "/target" }, diagnostics.Select(d => d.GetProperty("jsonPointer").GetString()));
        Assert.DoesNotContain(diagnostics, d => d.GetProperty("code").GetString() == "InheritanceCycle");
    }

    [Theory]
    [InlineData("inconsistent-order.json", 3, "inconsistent")]
    [InlineData("inheritance-cycle.json", 2, "invalidInput")]
    [InlineData("diamond.json", 0, "success")]
    public async Task ArtifactsOnExpectedOutcomesAndStreamRouting(string fixture, int exit, string status)
    {
        using var temp = new TempDirectory();
        string json = temp.File("nested space/report.json"), svg = temp.File("nested space/report.svg");
        var r = await Run("trace", TestPaths.Fixture(fixture), "--json", json, "--svg", svg);
        Assert.Equal(exit, r.Exit);
        Assert.Equal(exit != 2, string.IsNullOrEmpty(r.Error));
        Assert.Equal(exit == 2, string.IsNullOrEmpty(r.Out));
        using var doc = JsonDocument.Parse(File.ReadAllText(json));
        Assert.Equal(status, doc.RootElement.GetProperty("status").GetString());
        Assert.Equal("svg", XDocument.Load(svg).Root?.Name.LocalName);
        Assert.DoesNotContain('\r', File.ReadAllText(json));
        Assert.DoesNotContain('\r', File.ReadAllText(svg));
        Assert.False(File.ReadAllBytes(json).AsSpan().StartsWith(new byte[] { 239, 187, 191 }));
        Assert.DoesNotContain("schemaVersion", r.Out);
    }

    [Fact]
    public async Task AllOutputsPreflightBeforeWritingAndForcePreservesInput()
    {
        using var temp = new TempDirectory();
        string input = temp.File("input.json"), json = temp.File("out.json"), svg = temp.File("out.svg");
        File.Copy(TestPaths.Fixture("diamond.json"), input);
        string original = File.ReadAllText(input);
        File.WriteAllText(svg, "keep");
        Assert.Equal(1, (await Run("trace", input, "--json", json, "--svg", svg)).Exit);
        Assert.False(File.Exists(json));
        Assert.Equal("keep", File.ReadAllText(svg));
        Assert.Equal(0, (await Run("trace", input, "--json", json, "--svg", svg, "--force")).Exit);
        Assert.Equal(1, (await Run("trace", input, "--json", input, "--force")).Exit);
        Assert.Equal(original, File.ReadAllText(input));
        Assert.Equal(1, (await Run("trace", input, "--json", temp.File("same"), "--svg", temp.File("./same"))).Exit);
        Assert.Equal(1, (await Run("trace", input, "--json", temp.File("out.svg/child.json"), "--force")).Exit);
        Assert.Equal(1, (await Run("trace", temp.File("missing.json"))).Exit);
        Assert.Empty(Directory.GetFiles(temp.Path, ".c3-*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task OperationalOutputFailureTakesPrecedenceOverConflict()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(temp.File("file"), "blocked parent");
        var r = await Run("trace", TestPaths.Fixture("inconsistent-order.json"), "--json", temp.File("file/report.json"));
        Assert.Equal(1, r.Exit);
        Assert.Contains("parent is a file", r.Error);
    }

    [Fact]
    public async Task ValidateReportAndCultureAreStable()
    {
        using var temp = new TempDirectory();
        string path = temp.File("r.json");
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ar-SA");
            var r = await Run("validate", TestPaths.Fixture("diamond.json"), "--json", path);
            Assert.Equal(0, r.Exit);
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            foreach (string property in new[] { "mro", "failure", "metrics" }) Assert.Equal(JsonValueKind.Null, doc.RootElement.GetProperty(property).ValueKind);
            Assert.Empty(doc.RootElement.GetProperty("trace").GetProperty("events").EnumerateArray());
            Assert.Empty(doc.RootElement.GetProperty("classResults").EnumerateArray());
            r = await Run("trace", TestPaths.Fixture("diamond.json"), "--no-trace");
            Assert.Contains("candidateChecks=6", r.Out);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task BoundedTextSnapshots()
    {
        var diamond = await Run("trace", TestPaths.Fixture("diamond.json"), "--no-trace");
        Assert.Equal("Requested target: D\nTarget MRO: D A B O\nTrace truncated: retained 0, dropped 31; result fully computed.\n"
            + "Metrics: classesStarted=4, classesCompleted=4, mergeSteps=5, candidateChecks=6, rejectedCandidates=1, acceptedCandidates=5, tailElementComparisons=5, headRemovals=10, eventsAttempted=31, eventsRetained=0, eventsDropped=31\n", diamond.Out);
        var conflict = await Run("trace", TestPaths.Fixture("inconsistent-order.json"), "--no-trace");
        Assert.StartsWith("Requested target: Z\nNo C3 linearization for target Z; failing class Z.\nIncomplete partial prefix for Z: [Z, A, B]\n"
            + "  Remaining base:0: [X, Y, O]\n  Remaining base:1: [Y, X, O]\n  Remaining direct: []\n"
            + "Precedence cycle (not inheritance): X -> Y -> X\n  X -> Y: base:0[1] -> [2]\n  Y -> X: base:1[1] -> [2]\n", conflict.Out);
        var full = await Run("trace", TestPaths.Fixture("diamond.json"));
        Assert.Contains("O rejected: occurs at remaining index 1 of base:1 [B, O]; B must precede O", full.Out);
    }

    [Fact]
    public void FilesystemLinksAndDirectoryAliasesAreRejected()
    {
        using var temp = new TempDirectory();
        string input = temp.File("input.json"), link = temp.File("link.json");
        File.WriteAllText(input, "keep");
        string real = temp.File("real"), alias = temp.File("alias");
        Directory.CreateDirectory(real);
        if (OperatingSystem.IsWindows())
        {
            // Junctions are directory links available without Windows symlink privileges.
            var start = new ProcessStartInfo("cmd.exe") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            foreach (string a in new[] { "/c", "mklink", "/J", alias, real }) start.ArgumentList.Add(a);
            using var process = Assert.IsType<Process>(Process.Start(start));
            Assert.True(process.WaitForExit(10000));
            Assert.Equal(0, process.ExitCode);
        }
        else
        {
            File.CreateSymbolicLink(link, input);
            Assert.Throws<IOException>(() => OutputWriter.Preflight(input, link, null, true));
            Directory.CreateSymbolicLink(alias, real);
        }
        try
        {
            Assert.Throws<IOException>(() => OutputWriter.Preflight(input, alias, null, true));
            Assert.Throws<IOException>(() => OutputWriter.Preflight(input, temp.File("real/out"), temp.File("alias/out"), true));
            File.WriteAllText(temp.File("real/input.json"), "keep");
            Assert.Throws<IOException>(() => OutputWriter.Preflight(temp.File("real/input.json"), temp.File("alias/input.json"), null, true));
        }
        finally { Directory.Delete(alias); }
    }
}
