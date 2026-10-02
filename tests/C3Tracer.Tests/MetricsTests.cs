using C3Tracer.Core.Linearization;
using Xunit;

namespace C3Tracer.Tests;

public class MetricsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(20000)]
    public void DiamondCountersAreExactAndCapIndependent(int cap)
    {
        var r = C3Linearizer.Linearize(TestPaths.Graph("diamond.json"), "D", new(cap));
        var m = r.Metrics;
        Assert.Equal(new long[] { 4, 4, 5, 6, 1, 5, 5, 10 }, new[] { m.ClassesStarted, m.ClassesCompleted, m.MergeSteps, m.CandidateChecks,
            m.RejectedCandidates, m.AcceptedCandidates, m.TailElementComparisons, m.HeadRemovals });
        // Root: 2; A and B: 7 each; D: 15 (including one rejection).
        Assert.Equal(31, m.EventsAttempted);
        Assert.Equal(Math.Min(cap, 31), m.EventsRetained);
        Assert.Equal(31 - Math.Min(cap, 31), m.EventsDropped);
        Assert.Equal(m.EventsDropped > 0, r.TraceTruncated);
        Assert.Equal(new[] { "D", "A", "B", "O" }, r.TargetMro);
    }
}
