using System.Collections.Immutable;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;
using Xunit;

namespace C3Tracer.Tests;

public class GeneratedHierarchyTests
{
    [Fact]
    public void TwoHundredSeededDagsRespectSemanticInvariants()
    {
        var random = new Random(73129);
        for (int sample = 0; sample < 200; sample++)
        {
            int size = random.Next(2, 21);
            var classes = Enumerable.Range(0, size).Select(i => new ClassDefinition("N" + i,
                Enumerable.Range(0, i).Where(_ => random.NextDouble() < .3).OrderBy(_ => random.Next())
                    .Take(5).Select(j => "N" + j).ToImmutableArray())).ToImmutableArray();
            string target = classes[^1].Name;
            LinearizationResult? baseline = null;
            foreach (var definitions in new[] { classes, classes.Reverse().ToImmutableArray() })
                foreach (int cap in new[] { 0, 1, 20000 })
                {
                    var graph = Assert.IsType<ValidatedGraph>(GraphValidator.Validate(new(definitions)).Graph);
                    var r = C3Linearizer.Linearize(graph, target, new(cap));
                    if (baseline is not null)
                    {
                        Assert.Equal(baseline.Status, r.Status);
                        Assert.Equal(baseline.TargetMro?.ToArray(), r.TargetMro?.ToArray());
                        Assert.Equal(baseline.Failure?.PartialPrefix.ToArray(), r.Failure?.PartialPrefix.ToArray());
                        Assert.Equal(baseline.Failure?.Witness.Cycle.ToArray(), r.Failure?.Witness.Cycle.ToArray());
                        Assert.Equal(baseline.Metrics with { EventsRetained = r.Metrics.EventsRetained, EventsDropped = r.Metrics.EventsDropped }, r.Metrics);
                    }
                    baseline = r;
                    foreach (var c in r.PerClassResults)
                    {
                        Assert.Equal(c.ClassName, c.Order[0]);
                        Assert.Equal(c.Order.Length, c.Order.Distinct(StringComparer.Ordinal).Count());
                        Assert.Equal(TargetClosure.Postorder(graph, c.ClassName).Order(StringComparer.Ordinal), c.Order.Order(StringComparer.Ordinal));
                        Subsequence(graph.ByName[c.ClassName].Bases, c.Order);
                        foreach (string b in graph.ByName[c.ClassName].Bases)
                            Subsequence(Assert.Single(r.PerClassResults, p => p.ClassName == b).Order, c.Order);
                    }
                    if (r.Failure is not null) ConflictTests.Verify(r.Failure);
                }
        }
    }

    private static void Subsequence(IEnumerable<string> required, ImmutableArray<string> order)
    {
        int last = -1;
        foreach (string name in required)
        {
            int next = order.IndexOf(name);
            Assert.True(next > last, $"{name} must follow position {last}.");
            last = next;
        }
    }
}
