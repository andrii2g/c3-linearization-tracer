using System.Globalization;
using System.Text;
using C3Tracer.Cli.Reporting;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Cli.Rendering;

public static class TextTraceRenderer
{
    public static string Render(ReportDocument report, bool showEvents = true)
    {
        var text = new StringBuilder();
        void Line(string value) => text.Append(value).Append('\n');
        if (report.Status == "invalidInput")
        {
            foreach (var d in report.Diagnostics) Line($"{d.Code}: {d.Message}" + (d.JsonPointer is null ? "" : $" ({d.JsonPointer})"));
            return text.ToString();
        }
        if (report.Command == "validate") return "Graph valid; C3 not evaluated.\n";
        Line($"Requested target: {report.Target}");
        var sequences = new Dictionary<string, SequenceSnapshot[]>(StringComparer.Ordinal);
        var cursors = new Dictionary<string, int[]>(StringComparer.Ordinal);
        var partial = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        if (showEvents)
            foreach (var e in report.Trace.Events)
                switch (e)
                {
                    case ClassStarted c:
                        Line($"Class {c.ClassName}; ordered bases [{string.Join(", ", c.Bases)}]");
                        partial[c.ClassName] = [c.ClassName]; break;
                    case BaseResultReused b: Line($"  Reuse {b.BaseName}: [{string.Join(", ", b.Order)}]"); break;
                    case MergeStarted merge:
                        sequences[merge.ClassName] = merge.Sequences.ToArray();
                        foreach (var s in merge.Sequences) Line($"  {s.Id} = [{string.Join(", ", s.Items)}]");
                        break;
                    case StepStarted s:
                        cursors[s.ClassName] = s.Cursors.ToArray();
                        Line(FormattableString.Invariant($"  Step {s.MergeStep}; cursors [{string.Join(",", s.Cursors)}]")); break;
                    case CandidateExamined c:
                        if (c.Accepted) Line($"  {c.Candidate} from {c.SourceSequenceId} accepted: absent from every remaining tail");
                        foreach (var b in c.Blockers)
                        {
                            int index = Array.FindIndex(sequences[c.ClassName], s => s.Id == b.SequenceId);
                            var seq = sequences[c.ClassName][index];
                            int cursor = cursors[c.ClassName][index];
                            Line(FormattableString.Invariant($"  {c.Candidate} rejected: occurs at remaining index {b.RemainingIndex} of {b.SequenceId} [{string.Join(", ", seq.Items.Skip(cursor))}]; {seq.Items[cursor]} must precede {c.Candidate}"));
                        }
                        break;
                    case CandidateSelected c:
                        partial[c.ClassName].Add(c.Candidate);
                        Line($"  Partial prefix [{string.Join(", ", partial[c.ClassName])}]; cursors after [{string.Join(",", c.CursorsAfter)}]"); break;
                    case ClassCompleted c: Line($"Complete {c.ClassName}: [{string.Join(", ", c.Order)}]"); break;
                }
        if (report.Failure is { } failure)
        {
            Line($"No C3 linearization for target {report.Target}; failing class {failure.FailingClass}.");
            Line($"Incomplete partial prefix for {failure.FailingClass}: [{string.Join(", ", failure.PartialPrefix)}]");
            foreach (var s in failure.RemainingSequences) Line($"  Remaining {s.Id}: [{string.Join(", ", s.Items.Skip(s.Cursor))}]");
            Line("Precedence cycle (not inheritance): " + string.Join(" -> ", failure.Witness.Cycle));
            foreach (var edge in failure.Witness.Edges)
                Line(FormattableString.Invariant($"  {edge.Before} -> {edge.After}: {edge.SequenceId}[{edge.BeforeIndex}] -> [{edge.AfterIndex}]"));
        }
        else if (report.Mro is { } mro) Line("Target MRO: " + string.Join(" ", mro));
        if (report.Trace.Truncated)
            Line(FormattableString.Invariant($"Trace truncated: retained {report.Trace.Events.Length}, dropped {report.Trace.DroppedEventCount}; result fully computed."));
        if (report.Metrics is { } m)
        {
            var metrics = new (string Name, long Value)[] {
                ("classesStarted", m.ClassesStarted), ("classesCompleted", m.ClassesCompleted), ("mergeSteps", m.MergeSteps),
                ("candidateChecks", m.CandidateChecks), ("rejectedCandidates", m.RejectedCandidates), ("acceptedCandidates", m.AcceptedCandidates),
                ("tailElementComparisons", m.TailElementComparisons), ("headRemovals", m.HeadRemovals),
                ("eventsAttempted", m.EventsAttempted), ("eventsRetained", m.EventsRetained), ("eventsDropped", m.EventsDropped) };
            Line("Metrics: " + string.Join(", ", metrics.Select(p => p.Name + "=" + p.Value.ToString(CultureInfo.InvariantCulture))));
        }
        return text.ToString();
    }
}
