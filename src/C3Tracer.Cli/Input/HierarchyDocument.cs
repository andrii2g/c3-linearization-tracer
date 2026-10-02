using System.Collections.Immutable;
using C3Tracer.Core.Model;

namespace C3Tracer.Cli.Input;

public sealed record HierarchyDocument(string? Target, Hierarchy Hierarchy);
public sealed record InputResult(HierarchyDocument? Document, ImmutableArray<Diagnostic> Diagnostics, string? Target = null);
