# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `GraphValidator.cs` | Name checks, duplicates, size/edge limits and reference validation before traversal; returns typed result. |
| `InheritanceCycleDetector.cs` | Iterative white/gray/black DFS; declared base order; recover first closed active-stack cycle. |
