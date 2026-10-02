using System.Collections.Immutable;

namespace C3Tracer.Core.Model;

public sealed record Diagnostic(string Code, string Message, string? JsonPointer = null,
    string? ClassName = null, ImmutableArray<string>? Path = null);
