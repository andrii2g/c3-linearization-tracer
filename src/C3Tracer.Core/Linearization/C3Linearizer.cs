using System.Collections.Immutable;
using C3Tracer.Core.Diagnostics;
using C3Tracer.Core.Model;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Core.Linearization;

public static class C3Linearizer
{
    public static LinearizationResult Linearize(ValidatedGraph graph, string target, TraceOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(options.MaxEvents);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MaxEvents, 100000);
        if (!graph.ByName.ContainsKey(target)) throw new ArgumentException("Target must exist in the validated graph.", nameof(target));
        var trace = new TraceCollector(options.MaxEvents);
        var cache = new Dictionary<string, ImmutableArray<string>>(StringComparer.Ordinal);
        var completed = ImmutableArray.CreateBuilder<ClassResult>();
        long started = 0, steps = 0, checks = 0, rejected = 0, accepted = 0, comparisons = 0, removals = 0;
        MergeFailure? failure = null;
        foreach (string name in TargetClosure.Postorder(graph, target))
        {
            var bases = graph.ByName[name].Bases;
            started++;
            trace.Emit(n => new ClassStarted(n, name, bases));
            var order = ImmutableArray.CreateBuilder<string>();
            order.Add(name);
            if (!bases.IsEmpty)
            {
                var sequences = new List<MergeSequenceCursor>();
                for (int i = 0; i < bases.Length; i++)
                {
                    string b = bases[i];
                    trace.Emit(n => new BaseResultReused(n, name, b, cache[b]));
                    sequences.Add(new("base:" + i, "baseLinearization", b, cache[b]));
                }
                sequences.Add(new("direct", "directBases", null, bases));
                trace.Emit(n => new MergeStarted(n, name, sequences.Select(s => s.Snapshot()).ToImmutableArray()));
                int step = 0;
                while (sequences.Any(s => !s.Exhausted))
                {
                    step++;
                    steps++;
                    trace.Emit(n => new StepStarted(n, name, step, sequences.Select(s => s.Cursor).ToImmutableArray()));
                    string? selected = null;
                    var blocked = ImmutableArray.CreateBuilder<BlockedCandidate>();
                    foreach (var source in sequences)
                    {
                        if (source.Exhausted) continue;
                        string candidate = source.Head;
                        var blockers = ImmutableArray.CreateBuilder<Blocker>();
                        foreach (var s in sequences)
                            for (int j = s.Cursor + 1; j < s.Items.Length; j++)
                            {
                                comparisons++;
                                if (StringComparer.Ordinal.Equals(s.Items[j], candidate))
                                    blockers.Add(new(s.Id, j, j - s.Cursor));
                            }
                        checks++;
                        var snapshot = blockers.ToImmutable();
                        trace.Emit(n => new CandidateExamined(n, name, step, source.Id, candidate, snapshot.IsEmpty, snapshot));
                        if (snapshot.IsEmpty) { selected = candidate; break; }
                        rejected++;
                        blocked.Add(new(source.Id, candidate, snapshot));
                    }
                    if (selected is null)
                    {
                        var remaining = sequences.Select(s => s.Snapshot()).ToImmutableArray();
                        failure = new(name, order.ToImmutable(), remaining, blocked.ToImmutable(), ConflictWitnessBuilder.Build(remaining));
                        trace.Emit(n => new ClassFailed(n, name, failure));
                        break;
                    }
                    order.Add(selected);
                    foreach (var s in sequences)
                        if (!s.Exhausted && StringComparer.Ordinal.Equals(s.Head, selected)) { s.Advance(); removals++; }
                    accepted++;
                    trace.Emit(n => new CandidateSelected(n, name, step, selected, sequences.Select(s => s.Cursor).ToImmutableArray(), order.Count));
                }
            }
            if (failure is not null) break;
            var result = order.ToImmutable();
            cache[name] = result;
            completed.Add(new(name, result));
            trace.Emit(n => new ClassCompleted(n, name, result));
        }
        var events = trace.Snapshot();
        return new(target, failure is null ? LinearizationStatus.Success : LinearizationStatus.Inconsistent,
            failure is null ? cache[target] : null, completed.ToImmutable(), failure,
            new(started, completed.Count, steps, checks, rejected, accepted, comparisons, removals, trace.Attempted, events.Length, trace.Dropped),
            events, trace.Dropped > 0, trace.Dropped);
    }
}
