# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `FixtureTests.cs` | Load expected.json from copied fixtures; assert exact successful orders and typed failure details. |
| `ValidationTests.cs` | Whole-document structure, limits, missing bases, duplicate definitions and inheritance cycles. |
| `JsonInputTests.cs` | Handwritten invalid JSON shape, duplicate properties and strict parsing behavior. |
| `LinearizationTests.cs` | Target overrides at boundary, base-order dependence and inherited subsequences. |
| `TraceTests.cs` | Candidate ordering, precise blocker positions, immutable snapshots and truncation invariance. |
| `ConflictTests.cs` | Stuck candidates, closed cycle provenance and ancestor failure semantics. |
| `MetricsTests.cs` | Exact diamond counters and cap-independent work counters. |
| `GeneratedHierarchyTests.cs` | 200 seeded small DAGs; invariants on success, real blockers on failure. |
| `CliTests.cs` | Exit codes, stream capture, temporary outputs, overwrite guards and path errors. |
| `SvgTests.cs` | XML, nodes, edges, base labels, escaped messages and stable bytes. |
| `TestPaths.cs` | Fixture access using AppContext.BaseDirectory and safely disposed temporary directories. |
