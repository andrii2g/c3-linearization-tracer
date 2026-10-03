# Development and WSL

Run `bash scripts/test.sh` on Linux/WSL or `pwsh scripts/test.ps1` on Windows. Each environment needs its own stable .NET 10 SDK. The scripts build Release before invoking the compiled CLI. From WSL with Docker available, run `docker build --target test -t c3-tracer-tests .`; a successful build requires actual CLI smoke behavior. No ports, mounts, privileged mode or long-running container are needed.

The Docker SDK image is a convenience tag, not a reproducible digest pin. The repository pins a minimum SDK with global.json and stable test dependency versions; record the actual `dotnet --info` used in verification. Commit NuGet lock files only with a validated complete locked-restore workflow. These settings do not claim bit-for-bit dependency reproducibility.

Generate SVG/report outputs with the .NET application under `artifacts/` (ignored). Reviewed diamond and conflict SVGs are included in `docs/examples/`; regenerate and visually inspect them after renderer changes. Handwritten semantic expectations in the same directory are independently authored, not generated reports. Keep semantic JSON free of timing data.

## Maintenance rules

- Implement C3 manually in Core. Classes remain data; production code does not use Python, runtime class generation, reflection-based resolution or third-party graph algorithms.
- Preserve declared direct-base order. Use ordinal sorting only for unordered class enumeration and diagnostic tie breaks.
- Keep Core independent of filesystem, console, JSON/XML, clocks and environment state. CLI owns input, output and presentation.
- Return typed outcomes for invalid input and inconsistent C3. An incomplete prefix is never a complete MRO; failure reports use `mro=null`.
- Keep inheritance-cycle and precedence-cycle diagnostics separate. Use iterative traversals for bounded-depth safety.
- Publish immutable snapshots and keep trace retention independent of algorithm outcomes and work counters.
- Maintain independent fixture expectations and meaningful behavioral tests. Do not disable failing tests or claim checks that were not run.
- Keep runtime dependencies limited to framework libraries and test dependencies pinned. Keep the application a local CLI.

See [testing](TESTING.md) for commands and coverage, and [verification](VERIFICATION.md) for observed results and platform limits. License selection remains with the repository owner.
