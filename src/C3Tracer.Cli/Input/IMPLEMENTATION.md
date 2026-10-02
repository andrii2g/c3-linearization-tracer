# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `HierarchyJsonReader.cs` | Bounded strict UTF-8 JSON parsing, duplicate keys and exact shape; no last-key-wins semantics. |
| `HierarchyDocument.cs` | Input DTO boundary; map null/missing/type errors before constructing Core domain objects. |
