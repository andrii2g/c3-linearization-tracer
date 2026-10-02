# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `CommandLineParser.cs` | Strict grammar and defaults in CLI.md; explicit options object and usage errors. |
| `CliApplication.cs` | RunAsync args/stdout/stderr, command dispatch, outcome mapping and operational error handling. |
| `ExitCodes.cs` | Named constants 0,1,2,3; shared across dispatcher and tests. |
