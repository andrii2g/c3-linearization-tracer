using System.Collections.Immutable;
using C3Tracer.Core.Diagnostics;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Core.Linearization;

public enum LinearizationStatus { Success, Inconsistent }
public sealed record TraceOptions(int MaxEvents = 20000);
public sealed record ClassResult(string ClassName, ImmutableArray<string> Order);
public sealed record LinearizationResult(string RequestedTarget, LinearizationStatus Status,
    ImmutableArray<string>? TargetMro, ImmutableArray<ClassResult> PerClassResults, MergeFailure? Failure,
    LinearizationMetrics Metrics, ImmutableArray<TraceEvent> RetainedEvents, bool TraceTruncated, long DroppedEventCount);
