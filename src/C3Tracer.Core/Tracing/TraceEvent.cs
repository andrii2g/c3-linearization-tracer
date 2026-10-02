using System.Collections.Immutable;
using C3Tracer.Core.Diagnostics;
using C3Tracer.Core.Linearization;

namespace C3Tracer.Core.Tracing;

public sealed record Blocker(string SequenceId, int AbsoluteIndex, int RemainingIndex);
public abstract record TraceEvent(long SequenceNumber, string Kind, string ClassName);
public sealed record ClassStarted(long SequenceNumber, string ClassName, ImmutableArray<string> Bases)
    : TraceEvent(SequenceNumber, nameof(ClassStarted), ClassName);
public sealed record BaseResultReused(long SequenceNumber, string ClassName, string BaseName, ImmutableArray<string> Order)
    : TraceEvent(SequenceNumber, nameof(BaseResultReused), ClassName);
public sealed record MergeStarted(long SequenceNumber, string ClassName, ImmutableArray<SequenceSnapshot> Sequences)
    : TraceEvent(SequenceNumber, nameof(MergeStarted), ClassName);
public sealed record StepStarted(long SequenceNumber, string ClassName, int MergeStep, ImmutableArray<int> Cursors)
    : TraceEvent(SequenceNumber, nameof(StepStarted), ClassName);
public sealed record CandidateExamined(long SequenceNumber, string ClassName, int MergeStep, string SourceSequenceId,
    string Candidate, bool Accepted, ImmutableArray<Blocker> Blockers)
    : TraceEvent(SequenceNumber, nameof(CandidateExamined), ClassName);
public sealed record CandidateSelected(long SequenceNumber, string ClassName, int MergeStep, string Candidate,
    ImmutableArray<int> CursorsAfter, int PartialOrderLength)
    : TraceEvent(SequenceNumber, nameof(CandidateSelected), ClassName);
public sealed record ClassCompleted(long SequenceNumber, string ClassName, ImmutableArray<string> Order)
    : TraceEvent(SequenceNumber, nameof(ClassCompleted), ClassName);
public sealed record ClassFailed(long SequenceNumber, string ClassName, MergeFailure Failure)
    : TraceEvent(SequenceNumber, nameof(ClassFailed), ClassName);
