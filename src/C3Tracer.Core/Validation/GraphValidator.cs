using System.Collections.Immutable;
using C3Tracer.Core.Model;

namespace C3Tracer.Core.Validation;

public static class GraphValidator
{
    public const int MaxClasses = 256, MaxBases = 32, MaxEdges = 2048;

    public static bool IsName(string? name) => name is { Length: >= 1 and <= 80 }
        && (char.IsAsciiLetter(name[0]) || name[0] == '_')
        && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '.' or '-');

    public static ValidationResult Validate(Hierarchy hierarchy)
    {
        var errors = new List<Diagnostic>();
        if (hierarchy.Classes.IsDefaultOrEmpty)
            return new(null, [new("InvalidShape", "classes must be a nonempty array.", "/classes")]);
        if (hierarchy.Classes.Length > MaxClasses)
            return new(null, [new("LimitExceeded", "At most 256 classes are allowed.", "/classes")]);
        var names = new HashSet<string>(StringComparer.Ordinal);
        long edges = 0;
        for (int i = 0; i < hierarchy.Classes.Length; i++)
        {
            var c = hierarchy.Classes[i];
            string p = $"/classes/{i}";
            if (!IsName(c.Name)) errors.Add(new("InvalidName", "Invalid class name.", p + "/name", c.Name));
            if (!names.Add(c.Name)) errors.Add(new("DuplicateClass", "Class is declared more than once.", p + "/name", c.Name));
            if (c.Bases.IsDefault)
            {
                errors.Add(new("InvalidShape", "bases must be an array.", p + "/bases", c.Name));
                continue;
            }
            edges += c.Bases.Length;
            if (c.Bases.Length > MaxBases) errors.Add(new("LimitExceeded", "At most 32 direct bases are allowed.", p + "/bases", c.Name));
            var bases = new HashSet<string>(StringComparer.Ordinal);
            for (int j = 0; j < c.Bases.Length; j++)
            {
                string b = c.Bases[j];
                if (!IsName(b)) errors.Add(new("InvalidName", "Invalid base name.", p + $"/bases/{j}", c.Name));
                if (!bases.Add(b)) errors.Add(new("DuplicateBase", "Direct base is repeated.", p + $"/bases/{j}", c.Name));
            }
        }
        if (edges > MaxEdges) errors.Add(new("LimitExceeded", "At most 2048 inheritance edges are allowed.", "/classes"));
        for (int i = 0; i < hierarchy.Classes.Length; i++)
        {
            var c = hierarchy.Classes[i];
            if (c.Bases.IsDefault) continue;
            for (int j = 0; j < c.Bases.Length; j++)
                if (!names.Contains(c.Bases[j])) errors.Add(new("MissingBase", $"Base {c.Bases[j]} is not declared.", $"/classes/{i}/bases/{j}", c.Name));
        }
        if (errors.Count > 0) return new(null, Order(errors));
        var graph = new ValidatedGraph(hierarchy.Classes);
        var cycle = InheritanceCycleDetector.Find(graph);
        return cycle.IsEmpty ? new(graph, []) : new(null,
            [new("InheritanceCycle", "Inheritance cycle: " + string.Join(" -> ", cycle), null, cycle[0], cycle)]);
    }

    public static ImmutableArray<Diagnostic> Order(IEnumerable<Diagnostic> errors) => errors
        .OrderBy(e => e.JsonPointer, StringComparer.Ordinal).ThenBy(e => e.Code, StringComparer.Ordinal).ToImmutableArray();
}
