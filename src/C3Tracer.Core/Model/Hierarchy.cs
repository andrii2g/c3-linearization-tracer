using System.Collections.Immutable;

namespace C3Tracer.Core.Model;

public sealed record Hierarchy(ImmutableArray<ClassDefinition> Classes);
