# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `TraceEvent.cs` | Immutable event variants from CONTRACTS; copy cursors and snapshots before publishing. |
| `TraceCollector.cs` | Fixed retention cap, stable attempted event numbering, dropped count; cap-independent engine behavior. |
