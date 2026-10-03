# Architecture

## Projects
C3Tracer.Core is a plain net10.0 library with models, validation, graph algorithms, C3 and trace records. C3Tracer.Cli is a console application referencing Core and owns JSON, arguments, filesystem and rendering. C3Tracer.Tests references both projects and uses xUnit with Microsoft.NET.Test.Sdk. No dependency injection container is required.

Core returns values, never exit codes. CLI maps outcomes to presentation and exit codes. Validation errors are separate from C3 failures. Input shape validation belongs in CLI; graph-level name/reference/base/cycle rules belong in Core so programmatic callers receive the same protection.

## Data flow
1. Parse arguments and check output paths.
2. Read bounded UTF-8 input, reject malformed UTF-8, parse JSON with explicit depth and strict syntax.
3. Walk JsonDocument to reject duplicate/unknown/missing properties and bad value kinds; create DTOs.
4. Map DTOs to copied immutable domain definitions.
5. Validate names, graph limits and references; run iterative whole-graph cycle check.
6. Resolve target, derive stable closure and compute C3 using local state owned by the invocation.
7. Build one immutable report model from graph, result and diagnostics.
8. Render text, optional JSON and SVG from that same model.

Do not cache results across commands or mutate input arrays. No shared static mutable data. Separate counters from trace retention so changing event caps cannot change metrics.

## Components
TraceCollector owns cap, attempted count and retained immutable events. MergeSequenceCursor owns an immutable sequence and current index; it does not emit events. ConflictWitnessBuilder consumes frozen suffixes. GraphLayout consumes graph data and returns boxes/edge paths; SvgGraphRenderer converts layout to XML. OutputWriter handles preflight and temporary files. CliApplication.RunAsync(args, stdout, stderr) returns an int and is callable in integration tests; Program only delegates.

Avoid a generic graph framework, repository pattern, strategy registry or service host. Explicit small components are enough.

## Dependency choices
Runtime: framework libraries only, including System.Text.Json and System.Xml.Linq. Tests: xUnit v2 with pinned stable package versions. Dependency updates should address a concrete compatibility or maintenance need. Python is only an optional independent test oracle. GitHub CI restores .NET dependencies, builds Release and runs tests on Linux/Windows.
