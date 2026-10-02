namespace C3Tracer.Core.Linearization;

public sealed record LinearizationMetrics(long ClassesStarted, long ClassesCompleted, long MergeSteps,
    long CandidateChecks, long RejectedCandidates, long AcceptedCandidates, long TailElementComparisons,
    long HeadRemovals, long EventsAttempted, long EventsRetained, long EventsDropped);
