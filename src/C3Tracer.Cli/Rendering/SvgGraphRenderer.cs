using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;
using C3Tracer.Cli.Reporting;

namespace C3Tracer.Cli.Rendering;

public static class SvgGraphRenderer
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";
    private static string N(double n) => n.ToString("0.###", CultureInfo.InvariantCulture);

    public static string Render(ReportDocument report)
    {
        GraphGeometry? layout = report.Status != "invalidInput" && report.Graph is { } graph && report.Target is { } target
            ? GraphLayout.Create(graph, target) : null;
        double width = layout?.Width ?? 800;
        var root = new XElement(Ns + "svg", new XAttribute("role", "img"), new XAttribute("aria-labelledby", "title description"),
            new XAttribute("font-family", "ui-monospace, Consolas, monospace"), new XAttribute("font-size", "14"));
        root.Add(new XElement(Ns + "title", new XAttribute("id", "title"), report.Status == "invalidInput" ? "Invalid hierarchy" : "C3 trace for " + report.Target));
        root.Add(new XElement(Ns + "desc", new XAttribute("id", "description"),
            "Inheritance arrows point from derived classes upward to bases. Numbers on edges preserve direct-base order. Precedence conflicts are reported separately."));
        root.Add(new XElement(Ns + "defs", new XElement(Ns + "marker", new XAttribute("id", "base-arrow"),
            new XAttribute("viewBox", "0 0 10 10"), new XAttribute("refX", "9"), new XAttribute("refY", "5"),
            new XAttribute("markerWidth", "7"), new XAttribute("markerHeight", "7"), new XAttribute("orient", "auto"),
            new XElement(Ns + "path", new XAttribute("d", "M 0 0 L 10 5 L 0 10 z"), new XAttribute("fill", "#52677d")))));
        var background = new XElement(Ns + "rect", new XAttribute("width", "100%"), new XAttribute("height", "100%"), new XAttribute("fill", "#f7fafc"));
        root.Add(background);
        string heading = report.Status == "invalidInput" ? "Invalid input" : $"Target: {report.Target} | {(report.Status == "success" ? "C3 success" : "No C3 linearization")}";
        double headerY = 30;
        foreach (string line in Wrap(heading, (int)((width - 48) / 11)))
        {
            Text(root, 24, headerY, line, "#172e45", "18");
            headerY += 24;
        }
        foreach (string line in Wrap("Inheritance: derived -> base (upward); edge labels = declared base position.", (int)((width - 48) / 9)))
        {
            Text(root, 24, headerY + 3, line, "#52677d");
            headerY += 22;
        }
        double offset = Math.Max(0, headerY + 10 - 96);
        if (layout is not null && offset > 0)
            layout = layout with { Bottom = layout.Bottom + offset,
                Nodes = layout.Nodes.Select(b => b with { Y = b.Y + offset }).ToImmutableArray(),
                Edges = layout.Edges.Select(e => e with { Y1 = e.Y1 + offset, Y2 = e.Y2 + offset }).ToImmutableArray() };
        double y = headerY + 24;
        if (layout is not null)
        {
            foreach (var e in layout.Edges)
            {
                double middle = (e.Y1 + e.Y2) / 2;
                root.Add(new XElement(Ns + "path", new XAttribute("class", "inheritance-edge"),
                    new XAttribute("data-derived", e.Derived), new XAttribute("data-base", e.Base), new XAttribute("data-base-order", e.BaseOrder),
                    new XAttribute("d", $"M {N(e.X1)} {N(e.Y1)} C {N(e.X1)} {N(middle)} {N(e.X2)} {N(middle)} {N(e.X2)} {N(e.Y2)}"),
                    new XAttribute("fill", "none"), new XAttribute("stroke", "#52677d"), new XAttribute("stroke-width", "1.5"), new XAttribute("marker-end", "url(#base-arrow)")));
                Text(root, e.X1 + 5, e.Y1 - 10, e.BaseOrder.ToString(CultureInfo.InvariantCulture), "#52677d");
            }
            foreach (var b in layout.Nodes)
            {
                bool isTarget = b.Class.Name == report.Target, failed = b.Class.Name == report.Failure?.FailingClass;
                int position = report.Mro?.IndexOf(b.Class.Name) ?? -1;
                var group = new XElement(Ns + "g", new XAttribute("id", b.Id), new XAttribute("class", "graph-node"), new XAttribute("data-name", b.Class.Name));
                group.Add(new XElement(Ns + "title", $"{b.Class.Name}; bases [{string.Join(", ", b.Class.Bases)}]" + (position < 0 ? "" : $"; target MRO position {position + 1}")));
                group.Add(new XElement(Ns + "rect", new XAttribute("x", N(b.X)), new XAttribute("y", N(b.Y)), new XAttribute("width", N(b.Width)),
                    new XAttribute("height", "44"), new XAttribute("rx", "7"), new XAttribute("fill", failed ? "#fff0ef" : isTarget ? "#d6eaff" : "#e4f3ef"),
                    new XAttribute("stroke", failed ? "#b42318" : isTarget ? "#245c9c" : "#357363"), new XAttribute("stroke-width", isTarget || failed ? "2.5" : "1.5")));
                Text(group, b.X + b.Width / 2, b.Y + 27, b.Class.Name, "#172e45", anchor: "middle");
                if (position >= 0) Text(group, b.X + b.Width - 6, b.Y + 12, "#" + (position + 1).ToString(CultureInfo.InvariantCulture), "#245c9c", "10", "end");
                if (failed) Text(group, b.X + b.Width / 2, b.Y + 61, "FAILING CLASS", "#b42318", "12", "middle");
                root.Add(group);
            }
            y = layout.Bottom + 20;
        }
        void Wrapped(string message, string color = "#172e45")
        {
            int columns = Math.Max(20, (int)((width - 64) / 9));
            foreach (string line in Wrap(message, columns)) { Text(root, 32, y, line, color); y += 22; }
        }
        if (report.Status == "invalidInput")
        {
            foreach (var d in report.Diagnostics) Wrapped(d.Code + ": " + d.Message, "#b42318");
        }
        else if (report.Failure is { } failure)
        {
            Wrapped("No C3 linearization; failing class: " + failure.FailingClass, "#b42318");
            Wrapped("Incomplete partial prefix: " + string.Join(" -> ", failure.PartialPrefix));
            y += 10;
            foreach (var s in failure.RemainingSequences) Wrapped($"Remaining {s.Id}: [{string.Join(", ", s.Items.Skip(s.Cursor))}]");
            y += 20;
            Wrapped("PRECEDENCE WITNESS (not inheritance arrows)", "#b42318");
            Wrapped("Closed cycle: " + string.Join(" -> ", failure.Witness.Cycle));
            foreach (var e in failure.Witness.Edges) Wrapped(FormattableString.Invariant($"{e.Before} -> {e.After} | {e.SequenceId} | absolute positions {e.BeforeIndex} -> {e.AfterIndex}"));
        }
        else if (report.Mro is { } mro)
        {
            Wrapped("Complete target MRO (left to right, then next row):");
            double x = 32;
            int position = 0;
            foreach (string name in mro)
            {
                double boxWidth = Math.Max(96, 18 + name.Length * 9);
                if (x + boxWidth > width - 32 && x > 32) { x = 32; y += 64; }
                var group = new XElement(Ns + "g", new XAttribute("class", "mro-item"), new XAttribute("data-name", name), new XAttribute("data-position", ++position));
                group.Add(new XElement(Ns + "rect", new XAttribute("x", N(x)), new XAttribute("y", N(y)), new XAttribute("width", N(boxWidth)),
                    new XAttribute("height", "40"), new XAttribute("rx", "5"), new XAttribute("fill", "#d6eaff"), new XAttribute("stroke", "#245c9c")));
                Text(group, x + boxWidth / 2, y + 25, name, "#172e45", anchor: "middle");
                root.Add(group);
                x += boxWidth + 16;
            }
            y += 64;
        }
        double height = y + 32;
        root.SetAttributeValue("viewBox", $"0 0 {N(width)} {N(height)}");
        root.SetAttributeValue("width", N(width));
        root.SetAttributeValue("height", N(height));
        return root.ToString().Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
    }

    private static IEnumerable<string> Wrap(string text, int columns)
    {
        // Also split long tokens so even future unrestricted diagnostic text stays in bounds.
        while (text.Length > columns)
        {
            int cut = text.LastIndexOf(' ', columns, columns);
            if (cut < 1) cut = columns;
            yield return text[..cut];
            text = text[cut..].TrimStart();
        }
        yield return text;
    }

    private static void Text(XElement parent, double x, double y, string text, string color, string size = "14", string anchor = "start") =>
        parent.Add(new XElement(Ns + "text", new XAttribute("x", N(x)), new XAttribute("y", N(y)), new XAttribute("fill", color),
            new XAttribute("font-size", size), new XAttribute("text-anchor", anchor), text));
}
