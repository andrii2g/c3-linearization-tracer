using C3Tracer.Core.Diagnostics;
using C3Tracer.Core.Linearization;
using Xunit;

namespace C3Tracer.Tests;

public class ConflictTests
{
    [Theory]
    [InlineData("inconsistent-order.json", "Z", "Z", "Z,A,B")]
    [InlineData("ancestor-failure.json", "Child", "Z", "Z,A,B")]
    [InlineData("redundant-base-invalid.json", "C", "C", "C")]
    public void FailureIsTypedAndHasRealWitness(string fixture, string target, string failing, string prefix)
    {
        foreach (int cap in new[] { 0, 1, 20000 })
        {
            var result = C3Linearizer.Linearize(TestPaths.Graph(fixture), target, new(cap));
            Assert.Equal(LinearizationStatus.Inconsistent, result.Status);
            Assert.Null(result.TargetMro);
            Assert.Equal(target, result.RequestedTarget);
            var failure = Assert.IsType<MergeFailure>(result.Failure);
            Assert.Equal(failing, failure.FailingClass);
            Assert.Equal(prefix.Split(','), failure.PartialPrefix);
            Verify(failure);
            if (failing == "Z")
            {
                Assert.Equal(new[] { "X", "Y", "X" }, failure.Witness.Cycle);
                Assert.Equal(new[] { 1, 1, 2 }, failure.RemainingSequences.Select(s => s.Cursor));
                Assert.Equal(new[] { "X,Y,O", "Y,X,O", "" }, failure.RemainingSequences.Select(s => string.Join(",", s.Items.Skip(s.Cursor))));
                Assert.Equal(new[] { new PrecedenceEdge("X", "Y", "base:0", 1, 2), new PrecedenceEdge("Y", "X", "base:1", 1, 2) }, failure.Witness.Edges);
            }
        }
    }

    internal static void Verify(MergeFailure failure)
    {
        Assert.Equal(failure.Witness.Cycle[0], failure.Witness.Cycle[^1]);
        Assert.Equal(failure.Witness.Cycle.Length - 1, failure.Witness.Edges.Length);
        for (int i = 0; i < failure.Witness.Edges.Length; i++)
        {
            var edge = failure.Witness.Edges[i];
            var s = Assert.Single(failure.RemainingSequences, s => s.Id == edge.SequenceId);
            Assert.InRange(edge.BeforeIndex, s.Cursor, s.Items.Length - 2);
            Assert.Equal(edge.BeforeIndex + 1, edge.AfterIndex);
            Assert.Equal(edge.Before, s.Items[edge.BeforeIndex]);
            Assert.Equal(edge.After, s.Items[edge.AfterIndex]);
            Assert.Equal(failure.Witness.Cycle[i], edge.Before);
            Assert.Equal(failure.Witness.Cycle[i + 1], edge.After);
        }
        Assert.Equal(failure.RemainingSequences.Count(s => s.Cursor < s.Items.Length), failure.BlockedCandidates.Length);
        foreach (var c in failure.BlockedCandidates)
        {
            Assert.NotEmpty(c.Blockers);
            var source = Assert.Single(failure.RemainingSequences, s => s.Id == c.SourceSequenceId);
            Assert.Equal(c.Candidate, source.Items[source.Cursor]);
            var expected = failure.RemainingSequences.SelectMany(s => Enumerable.Range(s.Cursor, s.Items.Length - s.Cursor)
                .Where(j => j > s.Cursor && s.Items[j] == c.Candidate).Select(j => new C3Tracer.Core.Tracing.Blocker(s.Id, j, j - s.Cursor)));
            Assert.Equal(expected, c.Blockers);
        }
    }
}
