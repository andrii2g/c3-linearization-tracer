using System.Collections.Immutable;
using C3Tracer.Core.Model;

namespace C3Tracer.Core.Linearization;

public static class TargetClosure
{
    public static ImmutableArray<string> Postorder(ValidatedGraph graph, string target)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal) { target };
        var stack = new List<(string Name, int Next)> { (target, 0) };
        var result = ImmutableArray.CreateBuilder<string>();
        while (stack.Count > 0)
        {
            var (name, next) = stack[^1];
            var bases = graph.ByName[name].Bases;
            if (next == bases.Length)
            {
                result.Add(name);
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            stack[^1] = (name, next + 1);
            if (seen.Add(bases[next])) stack.Add((bases[next], 0));
        }
        return result.ToImmutable();
    }
}
