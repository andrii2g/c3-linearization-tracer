# c3-linearization-tracer

CLI that explains C3 method-resolution order using JSON class hierarchies, candidate-selection traces, conflict witnesses, and standalone SVGs.

Runtime dependencies are framework libraries only; Python is an optional independent test oracle. The latest Windows Release verification passed all **98 tests**. See [verification evidence and limits](docs/VERIFICATION.md) for commands, visual checks and platform coverage.

## Product scope
- Read ordered base lists from JSON; class declarations are data, not generated C# types.
- Validate the entire input for structural errors and inheritance cycles.
- Compute C3 for the selected target and its ancestors.
- Explain every tested candidate and every blocking sequence.
- Distinguish invalid graphs from acyclic graphs with inconsistent precedence.
- Emit human-readable text, a versioned JSON report, and a deterministic SVG.

No web UI, dynamic inheritance, method bodies, plugin system, distributed services, database, or benchmark suite is required.

## Build and run
```bash
dotnet restore c3-linearization-tracer.slnx
dotnet build c3-linearization-tracer.slnx -c Release --no-restore
dotnet test c3-linearization-tracer.slnx -c Release --no-build
dotnet run --project src/C3Tracer.Cli -- trace fixtures/diamond.json --json artifacts/diamond.json --svg artifacts/diamond.svg
dotnet run --project src/C3Tracer.Cli -- trace fixtures/inconsistent-order.json --json artifacts/conflict.json --svg artifacts/conflict.svg
```
The final command should exit 3 and still write diagnostic artifacts.

Use `--force` to replace existing outputs when rerunning these examples. Open the SVG files directly in a browser or image viewer; no external renderer is needed by the application. Reviewed application-generated examples: [diamond](docs/examples/diamond.svg) and [conflict](docs/examples/conflict.svg).

```bash
dotnet run --project src/C3Tracer.Cli -- --help
dotnet run --project src/C3Tracer.Cli -- validate fixtures/diamond.json
dotnet run --project src/C3Tracer.Cli -- trace fixtures/diamond.json --target B --no-trace
```

`validate` checks the entire graph for structural errors and inheritance cycles, and says “Graph valid; C3 not evaluated.” `trace` evaluates only the target's closure after validating the entire document. Direct bases are ordered: reversing them can change the MRO or make it inconsistent. A failure's partial prefix is explicitly incomplete; JSON `mro` is null.

Input example:

```json
{"target":"D","classes":[{"name":"O","bases":[]},{"name":"A","bases":["O"]},{"name":"B","bases":["O"]},{"name":"D","bases":["A","B"]}]}
```

This produces `D A B O`. The trace explains why `O` is rejected before `B`: `O` remains in the tail of `[B, O]`.

Exit codes: **0** success, **1** operational/internal error, **2** invalid arguments/input, **3** inconsistent C3. Input diagnostics go to stderr; successful traces and expected C3 conflicts go to stdout. Requested JSON/SVG diagnostic reports are also written for invalid input and C3 failures.

Limits: 1 MiB UTF-8 input, 256 classes, 32 bases per class, 2,048 edges, nesting depth 16. Traces retain at most 20,000 events by default (`--max-events 0` through `100000`); truncation never changes the computed result. Names are case-sensitive ASCII identifiers, 1–80 characters. See [CLI contract](docs/CLI.md) for the strict option grammar and output protections.

## Reading map
| File | Purpose |
|---|---|
| [Requirements](docs/REQUIREMENTS.md) | Scope, validation, limits and semantics |
| [Architecture](docs/ARCHITECTURE.md) | Responsibilities and dependency boundaries |
| [Algorithm](docs/ALGORITHM.md) | Merge algorithm and cycle-witness pseudocode |
| [Contracts](docs/CONTRACTS.md) | Models, event schema, invariants and reporting |
| [CLI](docs/CLI.md) | Commands, defaults, errors and output behavior |
| [SVG](docs/SVG.md) | Layout, accessibility and failure visualization |
| [Development](docs/DEVELOPMENT.md) | Build scripts and maintenance rules |
| [Testing](docs/TESTING.md) | Behavioral coverage and independent verification |
| [Verification](docs/VERIFICATION.md) | Actual results and remaining verification limits |
| [Fixture expectations](fixtures/expected.json) | Expected outcomes independent of presentation |
| [Python oracle](scripts/verify-fixtures.py) | Optional independent check; no production dependency |

