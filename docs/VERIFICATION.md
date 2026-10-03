# Verification record

Last functional verification: **2026-10-02**, Windows x64, stable .NET SDK **10.0.401**.

The manual C3 engine, strict input validation, CLI, immutable traces, conflict witnesses, and text/JSON/SVG reports are implemented. Runtime dependencies are framework libraries only.

## Results

| Check | Observed result |
|---|---|
| `dotnet restore c3-linearization-tracer.slnx` | Succeeded with pinned test dependencies |
| `dotnet build c3-linearization-tracer.slnx -c Release --no-restore` | Succeeded; 0 warnings, 0 errors |
| `dotnet test c3-linearization-tracer.slnx -c Release --no-build` | 98 passed, 0 failed, 0 skipped |
| `bash scripts/smoke.sh` using Git Bash on Windows | Passed; diamond exit 0, conflict exit 3 with reports, validate exit 0 |
| `python scripts/verify-fixtures.py` on Windows | All 15 valid/inconsistent fixture outcomes matched Python C3 |
| `python3 scripts/verify-fixtures.py` in Ubuntu WSL | Same 15 outcomes passed |
| README help, target override and demo commands | Matched documented behavior |

The suite covers all 25 independent fixture expectations and 200 seeded DAGs with reversed declaration order and event caps 0, 1 and 20000. It checks exact diamond decisions and counters, ancestor failure semantics, repeated candidate occurrences, every tail blocker, witness provenance, output routing, path protections and deterministic serialization. See [testing](TESTING.md).

Documentation cleanup on the same date removed 19 obsolete instruction, planning and per-folder guide files. Release build and tests were rerun afterward: 0 warnings/errors and 98 passing tests. A repository reference scan found no stale references to removed files; all 27 local Markdown links across 12 documents resolved. `git diff --check` was clean.

## Visual verification

The .NET CLI generated [diamond.svg](examples/diamond.svg) and [conflict.svg](examples/conflict.svg). Both were rasterized with Sharp/libvips and inspected in an image viewer. An initial badge/edge-label collision was corrected; both regenerated images were inspected again with no clipping, incorrect arrowheads or overlapping panels observed. Header wrapping and XML escaping are also covered by tests.

The application does not depend on Sharp or a browser. The in-app browser tool failed to initialize, so this was image-viewer verification rather than live-browser verification.

## Verification limits

- Linux .NET execution was attempted, but Ubuntu WSL had only SDK 8.0.416. Linux tests remain unverified locally because this project requires .NET 10. The [CI matrix](../.github/workflows/ci.yml) includes Linux and Windows; no GitHub CI run was verified in this session.
- Windows directory-junction alias protection was exercised. File-symlink creation lacked OS privilege; the Linux file/directory-symlink test branch remains unverified locally.
- Docker was not exercised.
- Windows script execution initially encountered an unsigned-script policy. A process-only `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/test.ps1` invocation passed the then-current 97 tests and demos; no persistent policy was changed. Direct commands above subsequently verified the final 98-test suite.

Layout allows wide graphs and crossing edges; it does not minimize crossings. Output publication stages files but is not a multi-file transaction, and path checking is not a hostile-filesystem sandbox. These are documented design limits.
