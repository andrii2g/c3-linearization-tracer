using C3Tracer.Core.Linearization;
using C3Tracer.Core.Tracing;
using Xunit;

namespace C3Tracer.Tests;

public class TraceTests
{
    [Fact]
    public void DiamondMatchesHandwrittenTraceAndSnapshotsDoNotChange()
    {
        var result = C3Linearizer.Linearize(TestPaths.Graph("diamond.json"), "D", new());
        var events = result.RetainedEvents.Where(e => e.ClassName == "D").ToArray();
        var candidates = events.OfType<CandidateExamined>().ToArray();
        Assert.Equal(new[] { "A", "O", "B", "O" }, candidates.Select(c => c.Candidate));
        Assert.Equal(new[] { true, false, true, true }, candidates.Select(c => c.Accepted));
        Assert.Equal(new[] { 1, 2, 2, 3 }, candidates.Select(c => c.MergeStep));
        Assert.Equal(new Blocker("base:1", 1, 1), Assert.Single(candidates[1].Blockers));
        Assert.Equal(new[] { "base:0", "base:0", "base:1", "base:0" }, candidates.Select(c => c.SourceSequenceId));
        var steps = events.OfType<StepStarted>().ToArray();
        Assert.Equal(new[] { 0, 0, 0 }, steps[0].Cursors);
        Assert.Equal(new[] { 1, 0, 1 }, steps[1].Cursors);
        Assert.Equal(new[] { 1, 1, 2 }, steps[2].Cursors);
        var selected = events.OfType<CandidateSelected>().ToArray();
        Assert.Equal(new[] { 1, 0, 1 }, selected[0].CursorsAfter);
        Assert.Equal(new[] { 1, 1, 2 }, selected[1].CursorsAfter);
        Assert.Equal(new[] { 2, 2, 2 }, selected[2].CursorsAfter);
        Assert.Equal(new[] { 2, 3, 4 }, selected.Select(s => s.PartialOrderLength));
        Assert.All(Assert.Single(events.OfType<MergeStarted>()).Sequences, s => Assert.Equal(0, s.Cursor));
        Assert.Equal(Enumerable.Range(1, result.RetainedEvents.Length).Select(n => (long)n), result.RetainedEvents.Select(e => e.SequenceNumber));
        Assert.DoesNotContain(result.RetainedEvents, e => e.ClassName == "O" && e is MergeStarted);
    }
}
