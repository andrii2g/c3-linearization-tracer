using System.Collections.Immutable;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;

namespace C3Tracer.Cli.Reporting;

public static class ReportBuilder
{
    public static ReportDocument Build(string command, string? target, ValidatedGraph? graph,
        ImmutableArray<Diagnostic> diagnostics, LinearizationResult? result = null) => new(1, command,
        !diagnostics.IsEmpty ? "invalidInput" : result?.Status == LinearizationStatus.Inconsistent ? "inconsistent" : "success",
        target, graph is null ? null : new(graph.Classes), result?.TargetMro, result?.PerClassResults ?? [],
        diagnostics, result?.Failure, result?.Metrics,
        new(result?.TraceTruncated ?? false, result?.DroppedEventCount ?? 0, result?.RetainedEvents ?? []));
}
