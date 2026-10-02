using System.Globalization;

namespace C3Tracer.Cli.Commands;

public sealed record CommandOptions(string Command, string? Input = null, string? Target = null,
    string? Json = null, string? Svg = null, int MaxEvents = 20000, bool NoTrace = false, bool Force = false);
public sealed record ArgumentResult(CommandOptions? Options, string? Error);

public static class CommandLineParser
{
    public const string Usage = "c3-tracer trace <input.json> [--target NAME] [--json PATH] [--svg PATH] [--max-events N | --no-trace] [--force]\n"
        + "c3-tracer validate <input.json> [--json PATH] [--force]\n"
        + "c3-tracer --help | --version";

    public static ArgumentResult Parse(string[] args)
    {
        if (args.Length == 1 && args[0] is "--help" or "--version") return new(new(args[0]), null);
        if (args.Length == 0 || args[0] is not ("trace" or "validate")) return Error("Expected trace or validate command.");
        string? input = null, target = null, json = null, svg = null;
        int cap = 20000;
        bool noTrace = false, force = false;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 1; i < args.Length; i++)
        {
            string a = args[i];
            if (!a.StartsWith('-'))
            {
                if (input is not null || a.Length == 0) return Error("Exactly one input path is required.");
                input = a;
                continue;
            }
            if (a is not ("--target" or "--json" or "--svg" or "--max-events" or "--no-trace" or "--force")) return Error($"Unknown option: {a}");
            if (!seen.Add(a)) return Error($"Duplicate option: {a}");
            if (args[0] == "validate" && a is "--target" or "--svg" or "--max-events" or "--no-trace") return Error($"{a} is not supported by validate.");
            if (a == "--force") { force = true; continue; }
            if (a == "--no-trace") { noTrace = true; continue; }
            if (++i >= args.Length || string.IsNullOrEmpty(args[i]) || args[i].StartsWith('-')) return Error($"Missing value for {a}.");
            string value = args[i];
            switch (a)
            {
                case "--target": target = value; break;
                case "--json": json = value; break;
                case "--svg": svg = value; break;
                case "--max-events":
                    if (!value.All(char.IsAsciiDigit) || !int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out cap) || cap > 100000)
                        return Error("--max-events must be a decimal integer from 0 through 100000.");
                    break;
            }
        }
        if (input is null) return Error("Exactly one input path is required.");
        if (noTrace && seen.Contains("--max-events")) return Error("--no-trace cannot be combined with --max-events.");
        if (force && json is null && svg is null) return Error("--force requires an output path.");
        return new(new(args[0], input, target, json, svg, noTrace ? 0 : cap, noTrace, force), null);
    }

    private static ArgumentResult Error(string message) => new(null, message);
}
