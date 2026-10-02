using System.Collections.Immutable;
using C3Tracer.Core.Model;

namespace C3Tracer.Core.Validation;

public static class InheritanceCycleDetector
{
    public static ImmutableArray<string> Find(ValidatedGraph graph)
    {
        var colors = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<(string Name, int Next)>();
        foreach (var start in graph.Classes)
        {
            if (colors.ContainsKey(start.Name)) continue;
            colors[start.Name] = 1;
            stack.Add((start.Name, 0));
            while (stack.Count > 0)
            {
                var (name, next) = stack[^1];
                var bases = graph.ByName[name].Bases;
                if (next == bases.Length)
                {
                    colors[name] = 2;
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                stack[^1] = (name, next + 1);
                string b = bases[next];
                if (colors.GetValueOrDefault(b) == 1)
                    return stack.Skip(stack.FindIndex(f => f.Name == b)).Select(f => f.Name).Append(b).ToImmutableArray();
                if (colors.ContainsKey(b)) continue;
                colors[b] = 1;
                stack.Add((b, 0));
            }
        }
        return [];
    }
}
