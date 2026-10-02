# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `ReportDocument.cs` | Versioned JSON DTOs with explicit property names and failure null semantics. |
| `ReportBuilder.cs` | Convert validated graph/results to one stable model without recomputing C3. |
| `OutputWriter.cs` | Path preflight, overwrite guard, same-directory temporary files and cleanup; clear partial-publication errors. |
