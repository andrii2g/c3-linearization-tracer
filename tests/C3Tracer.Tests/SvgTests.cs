using System.Collections.Immutable;
using System.Globalization;
using System.Xml.Linq;
using C3Tracer.Cli.Rendering;
using C3Tracer.Cli.Reporting;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;
using Xunit;

namespace C3Tracer.Tests;

public class SvgTests
{
    private static readonly XNamespace Ns = "http://www.w3.org/2000/svg";
    private static ReportDocument Report(string file)
    {
        var doc = TestPaths.Document(file);
        var graph = TestPaths.Graph(file);
        string target = Assert.IsType<string>(doc.Target);
        return ReportBuilder.Build("trace", target, graph, [], C3Linearizer.Linearize(graph, target, new()));
    }

    [Theory]
    [InlineData("diamond.json")]
    [InlineData("inconsistent-order.json")]
    [InlineData("ancestor-failure.json")]
    public void StandaloneXmlContainsEveryOrderedEdgeAndClosureNode(string fixture)
    {
        var report = Report(fixture);
        string svg = SvgGraphRenderer.Render(report);
        var xml = XDocument.Parse(svg);
        Assert.Equal("img", xml.Root?.Attribute("role")?.Value);
        var graph = TestPaths.Graph(fixture);
        var target = Assert.IsType<string>(report.Target);
        var closure = TargetClosure.Postorder(graph, target);
        var nodes = xml.Descendants(Ns + "g").Where(e => (string?)e.Attribute("class") == "graph-node").ToArray();
        Assert.Equal(closure.Order(StringComparer.Ordinal), nodes.Select(n => (string?)n.Attribute("data-name")).Order(StringComparer.Ordinal));
        var edges = xml.Descendants(Ns + "path").Where(e => (string?)e.Attribute("class") == "inheritance-edge").ToArray();
        Assert.Equal(closure.Sum(n => graph.ByName[n].Bases.Length), edges.Length);
        foreach (var e in edges)
        {
            string derived = Assert.IsType<string>((string?)e.Attribute("data-derived"));
            int order = int.Parse(Assert.IsType<string>((string?)e.Attribute("data-base-order")), CultureInfo.InvariantCulture);
            Assert.Equal(graph.ByName[derived].Bases[order - 1], (string?)e.Attribute("data-base"));
            Assert.Equal("url(#base-arrow)", (string?)e.Attribute("marker-end"));
        }
        var geometry = GraphLayout.Create(Assert.IsType<ReportGraph>(report.Graph), target);
        Assert.All(geometry.Edges, e => Assert.True(e.Y2 < e.Y1));
        if (report.Mro is { } mro)
            Assert.Equal(mro, xml.Descendants(Ns + "g").Where(e => (string?)e.Attribute("class") == "mro-item").Select(e => (string?)e.Attribute("data-name")));
        else
        {
            Assert.Contains("Incomplete partial prefix", svg);
            Assert.Contains("PRECEDENCE WITNESS", svg);
            Assert.Contains("No C3 linearization", svg);
        }
        Assert.DoesNotContain(xml.Descendants(), e => e.Name.LocalName is "script" or "foreignObject" or "image");
        Assert.DoesNotContain(xml.Descendants().Attributes(), a => a.Name.LocalName is "href" or "onload");
    }

    [Fact]
    public void PermutationAndCultureProduceIdenticalJsonAndSvg()
    {
        var report = Report("diamond.json");
        var classes = TestPaths.Document("diamond.json").Hierarchy.Classes.Reverse().ToImmutableArray();
        var graph = Assert.IsType<ValidatedGraph>(GraphValidator.Validate(new(classes)).Graph);
        var other = ReportBuilder.Build("trace", "D", graph, [], C3Linearizer.Linearize(graph, "D", new()));
        var before = CultureInfo.CurrentCulture;
        string svg = SvgGraphRenderer.Render(report), json = JsonReportWriter.Render(report);
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal(svg, SvgGraphRenderer.Render(other));
            Assert.Equal(json, JsonReportWriter.Render(other));
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [Fact]
    public void DiagnosticTextIsEscapedAndLongLabelsAreNotClipped()
    {
        var report = ReportBuilder.Build("trace", null, null, [new("InvalidName", "A<&\"")]);
        string svg = SvgGraphRenderer.Render(report);
        Assert.Contains("A&lt;&amp;", svg);
        Assert.Contains("A<&\"", XDocument.Parse(svg).Descendants(Ns + "text").Select(e => e.Value).Last());
        string name = new('A', 80);
        var graph = Assert.IsType<ValidatedGraph>(GraphValidator.Validate(TestPaths.Hierarchy((name, []))).Graph);
        var layout = GraphLayout.Create(new(graph.Classes), name);
        Assert.True(layout.Width >= 18 + 9 * 80 + 64);
    }
}
