# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `TargetClosure.cs` | Iterative declared-base postorder over selected closure; stable and duplicate-free. |
| `MergeSequenceCursor.cs` | Immutable sequence plus mutable private cursor, head/tail enumeration and provenance. |
| `C3Linearizer.cs` | Base-first computation, cache, manual first-good-head merge, counters and events; no I/O. |
| `LinearizationResult.cs` | Success/inconsistent outcome, target order or failure, class results, metrics and trace. |
| `LinearizationMetrics.cs` | 64-bit semantic work counters; no clock measurements. |
