using System.Collections.Immutable;
using C3Tracer.Cli.Input;
using C3Tracer.Cli.Rendering;
using C3Tracer.Cli.Reporting;
using C3Tracer.Core.Linearization;
using C3Tracer.Core.Model;
using C3Tracer.Core.Validation;

namespace C3Tracer.Cli.Commands;

public static class CliApplication
{
    public static async Task<int> RunAsync(string[] args, TextWriter stdout, TextWriter stderr)
    {
        var parsed = CommandLineParser.Parse(args);
        if (parsed.Options is not { } options)
        {
            await stderr.WriteAsync(parsed.Error + "\n" + CommandLineParser.Usage + "\n");
            return ExitCodes.InvalidInput;
        }
        if (options.Command is "--help" or "--version")
        {
            await stdout.WriteAsync(options.Command == "--help" ? CommandLineParser.Usage + "\n" : "c3-tracer 0.1.0\n");
            return ExitCodes.Success;
        }
        try
        {
            if (options.Input is not { } inputPath) throw new InvalidOperationException("Parsed command has no input.");
            var paths = OutputWriter.Preflight(inputPath, options.Json, options.Svg, options.Force);
            var input = await HierarchyJsonReader.ReadAsync(paths.Input);
            var diagnostics = input.Diagnostics;
            string? target = options.Target ?? input.Target;
            ValidatedGraph? graph = null;
            if (input.Document is { } document)
            {
                var validation = GraphValidator.Validate(document.Hierarchy);
                diagnostics = validation.Diagnostics;
                graph = validation.Graph;
                if (graph is not null)
                {
                    if (target is null && options.Command == "trace") diagnostics = [new("MissingTarget", "Trace requires a target.", "/target")];
                    else if (target is not null && !GraphValidator.IsName(target)) diagnostics = [new("InvalidName", "Invalid target name.", "/target")];
                    else if (target is not null && !graph.ByName.ContainsKey(target)) diagnostics = [new("UnknownTarget", $"Target {target} is not declared.", "/target")];
                }
            }
            LinearizationResult? result = null;
            if (diagnostics.IsEmpty && graph is not null && target is not null && options.Command == "trace")
                result = C3Linearizer.Linearize(graph, target, new(options.MaxEvents));
            var report = ReportBuilder.Build(options.Command, target, graph, diagnostics, result);
            string text = TextTraceRenderer.Render(report, !options.NoTrace);
            await (diagnostics.IsEmpty ? stdout : stderr).WriteAsync(text);
            await OutputWriter.WriteAsync(paths, paths.Json is null ? null : JsonReportWriter.Render(report),
                paths.Svg is null ? null : SvgGraphRenderer.Render(report), options.Force);
            return report.Status switch { "invalidInput" => ExitCodes.InvalidInput, "inconsistent" => ExitCodes.Inconsistent, _ => ExitCodes.Success };
        }
        catch (Exception ex)
        {
            await stderr.WriteAsync($"Error: {ex.Message}\n");
            return ExitCodes.OperationalFailure;
        }
    }
}
