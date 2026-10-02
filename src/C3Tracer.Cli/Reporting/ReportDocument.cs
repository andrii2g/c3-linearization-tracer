using System.Collections.Immutable;
using C3Tracer.Core.Diagnostics;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;
using C3Tracer.Core.Tracing;

namespace C3Tracer.Cli.Reporting;

public sealed record ReportGraph(ImmutableArray<ClassDefinition> Classes);
public sealed record ReportTrace(bool Truncated, long DroppedEventCount, ImmutableArray<TraceEvent> Events);
public sealed record ReportDocument(int SchemaVersion, string Command, string Status, string? Target,
    ReportGraph? Graph, ImmutableArray<string>? Mro, ImmutableArray<ClassResult> ClassResults,
    ImmutableArray<Diagnostic> Diagnostics, MergeFailure? Failure, LinearizationMetrics? Metrics, ReportTrace Trace);
