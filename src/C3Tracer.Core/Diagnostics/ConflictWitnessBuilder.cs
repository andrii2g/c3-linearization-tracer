using System.Collections.Immutable;
using C3Tracer.Core.Linearization;

namespace C3Tracer.Core.Diagnostics;

public static class ConflictWitnessBuilder
{
    public static ConflictWitness Build(ImmutableArray<SequenceSnapshot> sequences)
    {
        var sources = new Dictionary<(string, string), List<PrecedenceEdge>>();
        var neighbors = new SortedDictionary<string, SortedSet<string>>(StringComparer.Ordinal);
        foreach (var s in sequences)
            for (int i = s.Cursor; i + 1 < s.Items.Length; i++)
            {
                string a = s.Items[i], b = s.Items[i + 1];
                if (!neighbors.ContainsKey(a)) neighbors[a] = new(StringComparer.Ordinal);
                if (!neighbors.ContainsKey(b)) neighbors[b] = new(StringComparer.Ordinal);
                neighbors[a].Add(b);
                if (!sources.TryGetValue((a, b), out var list)) sources[(a, b)] = list = [];
                list.Add(new(a, b, s.Id, i, i + 1));
            }
        var adjacency = neighbors.ToDictionary(p => p.Key, p => p.Value.ToArray(), StringComparer.Ordinal);
        var colors = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new List<(string Name, int Next)>();
        foreach (string start in neighbors.Keys)
        {
            if (colors.ContainsKey(start)) continue;
            colors[start] = 1;
            stack.Add((start, 0));
            while (stack.Count > 0)
            {
                var (name, next) = stack[^1];
                if (next == adjacency[name].Length)
                {
                    colors[name] = 2;
                    stack.RemoveAt(stack.Count - 1);
                    continue;
                }
                stack[^1] = (name, next + 1);
                string neighbor = adjacency[name][next];
                if (colors.GetValueOrDefault(neighbor) == 1)
                {
                    var cycle = stack.Skip(stack.FindIndex(f => f.Name == neighbor)).Select(f => f.Name).ToArray();
                    int min = 0;
                    for (int i = 1; i < cycle.Length; i++)
                        if (StringComparer.Ordinal.Compare(cycle[i], cycle[min]) < 0) min = i;
                    var rotated = Enumerable.Range(0, cycle.Length).Select(i => cycle[(min + i) % cycle.Length]).ToImmutableArray();
                    rotated = rotated.Add(rotated[0]);
                    // First source follows original merge-sequence order; all sources were retained above.
                    return new(rotated, Enumerable.Range(0, rotated.Length - 1)
                        .Select(i => sources[(rotated[i], rotated[i + 1])][0]).ToImmutableArray());
                }
                if (colors.ContainsKey(neighbor)) continue;
                colors[neighbor] = 1;
                stack.Add((neighbor, 0));
            }
        }
        throw new InvalidOperationException("Stuck merge has no precedence cycle.");
    }
}
