using System.Collections.Immutable;
using C3Tracer.Cli.Input;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;
using Xunit;

namespace C3Tracer.Tests;

internal static class TestPaths
{
    public static string Fixture(string name) => Path.Combine(AppContext.BaseDirectory, "fixtures", name);
    public static HierarchyDocument Document(string name)
    {
        var parsed = HierarchyJsonReader.Parse(File.ReadAllBytes(Fixture(name)));
        Assert.Empty(parsed.Diagnostics);
        return Assert.IsType<HierarchyDocument>(parsed.Document);
    }
    public static ValidatedGraph Graph(string name) => Assert.IsType<ValidatedGraph>(GraphValidator.Validate(Document(name).Hierarchy).Graph);
    public static Hierarchy Hierarchy(params (string Name, string[] Bases)[] classes) =>
        new(classes.Select(c => new ClassDefinition(c.Name, c.Bases.ToImmutableArray())).ToImmutableArray());
}

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "c3-tests-" + Guid.NewGuid().ToString("N"));
    public TempDirectory() => Directory.CreateDirectory(Path);
    public string File(string name) => System.IO.Path.Combine(Path, name);
    public void Dispose() => Directory.Delete(Path, true);
}
