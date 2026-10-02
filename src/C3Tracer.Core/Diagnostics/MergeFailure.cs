using System.Collections.Immutable;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Core.Diagnostics;

public sealed record BlockedCandidate(string SourceSequenceId, string Candidate, ImmutableArray<Blocker> Blockers);
public sealed record MergeFailure(string FailingClass, ImmutableArray<string> PartialPrefix,
    ImmutableArray<SequenceSnapshot> RemainingSequences, ImmutableArray<BlockedCandidate> BlockedCandidates,
    ConflictWitness Witness);
