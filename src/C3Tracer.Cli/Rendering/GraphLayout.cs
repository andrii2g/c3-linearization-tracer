using System.Collections.Immutable;
using C3Tracer.Cli.Reporting;
using C3Tracer.Core.Model;

namespace C3Tracer.Cli.Rendering;

public sealed record NodeBox(string Id, ClassDefinition Class, double X, double Y, double Width, int Rank);
public sealed record EdgePath(string Derived, string Base, int BaseOrder, double X1, double Y1, double X2, double Y2);
public sealed record GraphGeometry(double Width, double Bottom, ImmutableArray<NodeBox> Nodes, ImmutableArray<EdgePath> Edges);

public static class GraphLayout
{
    public static GraphGeometry Create(ReportGraph graph, string target)
    {
        var index = graph.Classes.ToDictionary(c => c.Name, StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal) { target };
        var stack = new List<(string Name, int Next)> { (target, 0) };
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        while (stack.Count > 0)
        {
            var (name, next) = stack[^1];
            var bases = index[name].Bases;
            if (next == bases.Length)
            {
                ranks[name] = bases.IsEmpty ? 0 : 1 + bases.Max(b => ranks[b]);
                stack.RemoveAt(stack.Count - 1);
            }
            else
            {
                stack[^1] = (name, next + 1);
                if (seen.Add(bases[next])) stack.Add((bases[next], 0));
            }
        }
        double Width(string name) => Math.Max(96, 18 + 9 * name.Length);
        var groups = ranks.GroupBy(p => p.Value).OrderBy(g => g.Key)
            .Select(g => (Rank: g.Key, Names: g.Select(p => p.Key).Order(StringComparer.Ordinal).ToArray())).ToArray();
        double width = Math.Max(640, groups.Max(g => g.Names.Sum(Width) + (g.Names.Length - 1) * 36) + 64);
        var ids = seen.Order(StringComparer.Ordinal).Select((name, i) => (name, id: "n" + i)).ToDictionary(p => p.name, p => p.id, StringComparer.Ordinal);
        var boxes = new List<NodeBox>();
        foreach (var group in groups)
        {
            double x = (width - group.Names.Sum(Width) - (group.Names.Length - 1) * 36) / 2;
            foreach (string name in group.Names)
            {
                boxes.Add(new(ids[name], index[name], x, 96 + group.Rank * 136, Width(name), group.Rank));
                x += Width(name) + 36;
            }
        }
        var byName = boxes.ToDictionary(b => b.Class.Name, StringComparer.Ordinal);
        var edges = ImmutableArray.CreateBuilder<EdgePath>();
        foreach (var box in boxes.OrderBy(b => b.Class.Name, StringComparer.Ordinal))
            for (int i = 0; i < box.Class.Bases.Length; i++)
            {
                var b = byName[box.Class.Bases[i]];
                edges.Add(new(box.Class.Name, b.Class.Name, i + 1,
                    box.X + box.Width * (i + 1) / (box.Class.Bases.Length + 1), box.Y, b.X + b.Width / 2, b.Y + 44));
            }
        return new(width, boxes.Max(b => b.Y + 44) + 36, boxes.ToImmutableArray(), edges.ToImmutable());
    }
}
