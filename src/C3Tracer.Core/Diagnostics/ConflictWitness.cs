using System.Collections.Immutable;

namespace C3Tracer.Core.Diagnostics;

public sealed record PrecedenceEdge(string Before, string After, string SequenceId, int BeforeIndex, int AfterIndex);
public sealed record ConflictWitness(ImmutableArray<string> Cycle, ImmutableArray<PrecedenceEdge> Edges);
