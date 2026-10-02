using System.Collections.Immutable;

namespace C3Tracer.Core.Model;

public sealed record ClassDefinition(string Name, ImmutableArray<string> Bases);
