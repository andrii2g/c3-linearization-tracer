using System.Collections.Immutable;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;
using Xunit;

namespace C3Tracer.Tests;

public class ValidationTests
{
    [Theory]
    [InlineData("empty-classes.json", "InvalidShape")]
    [InlineData("duplicate-base.json", "DuplicateBase")]
    [InlineData("duplicate-class.json", "DuplicateClass")]
    [InlineData("missing-base.json", "MissingBase")]
    [InlineData("inheritance-cycle.json", "InheritanceCycle")]
    [InlineData("self-cycle.json", "InheritanceCycle")]
    [InlineData("disconnected-cycle.json", "InheritanceCycle")]
    public void InvalidGraphsHaveTypedDiagnostics(string file, string code)
    {
        var result = GraphValidator.Validate(TestPaths.Document(file).Hierarchy);
        Assert.Null(result.Graph);
        Assert.Contains(result.Diagnostics, d => d.Code == code);
        foreach (var d in result.Diagnostics.Where(d => d.Code == "InheritanceCycle"))
        {
            Assert.True(d.Path.HasValue);
            Assert.Equal(d.Path.Value[0], d.Path.Value[^1]);
        }
    }

    [Fact]
    public void ReferencesStopTraversalAndDiagnosticsAreSorted()
    {
        var r = GraphValidator.Validate(TestPaths.Hierarchy(("A", ["A", "Missing"]), ("bad name", [])));
        Assert.DoesNotContain(r.Diagnostics, d => d.Code == "InheritanceCycle");
        Assert.Equal(r.Diagnostics.OrderBy(d => d.JsonPointer, StringComparer.Ordinal), r.Diagnostics);
    }

    [Fact]
    public void ForwardReferencesAndCaseDistinctionAreValid()
    {
        Assert.NotNull(TestPaths.Graph("forward-references.json"));
        Assert.NotNull(TestPaths.Graph("case-sensitive.json"));
    }

    [Fact]
    public void LimitsAndMaximumDepth()
    {
        var chain = Enumerable.Range(0, 256).Select(i => new ClassDefinition("N" + i,
            i == 0 ? [] : ["N" + (i - 1)])).ToImmutableArray();
        Assert.NotNull(GraphValidator.Validate(new(chain)).Graph);
        Assert.Contains(GraphValidator.Validate(new(chain.Add(new("Extra", [])))).Diagnostics, d => d.Code == "LimitExceeded");
        var roots = Enumerable.Range(0, 33).Select(i => new ClassDefinition("R" + i, [])).ToImmutableArray();
        Assert.NotNull(GraphValidator.Validate(new(roots.Add(new("D", roots.Take(32).Select(c => c.Name).ToImmutableArray())))).Graph);
        Assert.Contains(GraphValidator.Validate(new(roots.Add(new("D", roots.Select(c => c.Name).ToImmutableArray())))).Diagnostics, d => d.Code == "LimitExceeded");
        var edges = roots.Take(32).Concat(Enumerable.Range(0, 64).Select(i => new ClassDefinition("D" + i, roots.Take(32).Select(c => c.Name).ToImmutableArray()))).ToImmutableArray();
        Assert.NotNull(GraphValidator.Validate(new(edges)).Graph);
        Assert.Contains(GraphValidator.Validate(new(edges.Add(new("Extra", ["R0"])))).Diagnostics, d => d.Code == "LimitExceeded");
        Assert.True(GraphValidator.IsName("_" + new string('a', 79)));
        Assert.False(GraphValidator.IsName(new string('a', 81)));
        Assert.False(GraphValidator.IsName("Å"));
    }
}
