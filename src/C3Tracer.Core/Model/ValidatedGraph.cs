using System.Collections.Immutable;

namespace C3Tracer.Core.Model;

public sealed class ValidatedGraph
{
    public ImmutableArray<ClassDefinition> Classes { get; }
    public ImmutableDictionary<string, ClassDefinition> ByName { get; }

    internal ValidatedGraph(IEnumerable<ClassDefinition> classes)
    {
        Classes = classes.OrderBy(c => c.Name, StringComparer.Ordinal).ToImmutableArray();
        ByName = Classes.ToImmutableDictionary(c => c.Name, StringComparer.Ordinal);
    }
}

public sealed record ValidationResult(ValidatedGraph? Graph, ImmutableArray<Diagnostic> Diagnostics);
