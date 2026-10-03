# Testing

Run from the repository root with a stable .NET 10 SDK:

```bash
dotnet restore c3-linearization-tracer.slnx
dotnet build c3-linearization-tracer.slnx -c Release --no-restore
dotnet test c3-linearization-tracer.slnx -c Release --no-build
bash scripts/smoke.sh
```

`bash scripts/test.sh` combines these steps on Linux/WSL or Git Bash. On Windows, `pwsh -File scripts/test.ps1` runs restore, build, tests and process-level trace demos. Direct `dotnet` commands are also available when local script policy disallows unsigned scripts.

## Behavioral coverage

| Test source | Coverage |
|---|---|
| `FixtureTests.cs` | All 25 outcomes in [expected.json](../fixtures/expected.json): exact MROs, failing classes/prefixes, and diagnostic codes |
| `ValidationTests.cs`, `JsonInputTests.cs` | Whole-document validation, strict JSON/UTF-8, duplicate properties, references, cycles, forward references, case sensitivity, and limits |
| `LinearizationTests.cs` | Ordered bases, disconnected inconsistency, roots, diamonds, and a 256-class chain |
| `TraceTests.cs`, `MetricsTests.cs` | Immutable cursors, exact diamond decisions/counters, repeated heads, all blockers, and retention-cap independence |
| `ConflictTests.cs` | Remaining suffixes, canonical directed witness and real source indices, ancestor failure with null target MRO |
| `GeneratedHierarchyTests.cs` | 200 seeded DAGs with 2–20 classes, declaration permutations, and caps 0/1/20000 |
| `CliTests.cs` | All four exit codes, stream routing, arguments, target overrides, failure artifacts, overwrite/path guards, text snapshots, and culture-independent output |
| `SvgTests.cs` | XML parsing, ordered edges, closure nodes, MRO strip, upward arrows, escaping, deterministic output and long names |

Sources are in [tests/C3Tracer.Tests](../tests/C3Tracer.Tests). Generated successes must contain the complete closure exactly once and preserve direct-base and inherited-MRO subsequences. Generated failures must have real blockers and a closed witness with valid provenance; an acyclic hierarchy is not assumed to be C3-consistent.

The diamond's semantic counters are: classesStarted=4, classesCompleted=4, mergeSteps=5, candidateChecks=6, rejectedCandidates=1, acceptedCandidates=5, tailElementComparisons=5, headRemovals=10. Its full trace has 31 events. Retention caps change retained/dropped counts, never semantic work or outcomes.

Windows path-link tests use unprivileged directory junctions. Linux tests use file and directory symlinks. Neither branch silently passes a failed link-creation operation.

## Independent expectations and oracle

Fixtures and handwritten [diamond trace](examples/diamond-target-trace.txt) and [conflict expectation](examples/conflict-expectation.json) are independent semantic contracts. Do not regenerate expected results from the implementation under test.

Optionally run:

```bash
python3 scripts/verify-fixtures.py
```

On Windows, `python scripts/verify-fixtures.py` is equivalent. This constructs Python classes only for external verification, removes Python's implicit `object`, and checks valid/inconsistent fixture outcomes. It does not verify .NET parsing, traces, partial prefixes, CLI behavior or SVG output.

## Visual checks and CI

After renderer changes, generate diamond and conflict SVGs with the CLI and inspect them in a browser or image viewer for clipping, arrow direction, label collisions and panel overlap. XML tests alone are not visual verification. Keep reviewed generated examples separate from handwritten expectations.

The [CI workflow](../.github/workflows/ci.yml) builds and tests on Ubuntu and Windows and runs process smoke checks. [Verification record](VERIFICATION.md) states what was actually run and which environments remain unverified.
