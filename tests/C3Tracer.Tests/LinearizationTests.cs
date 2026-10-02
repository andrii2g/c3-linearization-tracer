using System.Collections.Immutable;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;
using Xunit;

namespace C3Tracer.Tests;

public class LinearizationTests
{
    [Theory]
    [InlineData("single-root.json", "O", "O")]
    [InlineData("diamond.json", "D", "D,A,B,O")]
    [InlineData("diamond-reversed.json", "D", "D,B,A,O")]
    [InlineData("disconnected-inconsistency.json", "Safe", "Safe,O")]
    public void ExactOrders(string fixture, string target, string expected)
    {
        var result = C3Linearizer.Linearize(TestPaths.Graph(fixture), target, new());
        Assert.Equal(LinearizationStatus.Success, result.Status);
        Assert.Equal(expected.Split(','), result.TargetMro);
    }

    [Fact]
    public void MaximumChainIsEvaluatedIteratively()
    {
        var classes = Enumerable.Range(0, 256).Select(i => new ClassDefinition("N" + i, i == 0 ? [] : ["N" + (i - 1)])).ToImmutableArray();
        var graph = Assert.IsType<ValidatedGraph>(GraphValidator.Validate(new(classes)).Graph);
        var result = C3Linearizer.Linearize(graph, "N255", new(0));
        Assert.Equal(256, result.TargetMro?.Length);
        Assert.Equal(256, result.Metrics.ClassesCompleted);
    }
}
