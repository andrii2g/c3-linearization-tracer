# Files to implement here

These are planned implementation files, not completed source. Follow the root specifications.

| File | Responsibility |
|---|---|
| `ClassDefinition.cs` | Immutable name and ordered bases; defensive copying at boundaries. |
| `Hierarchy.cs` | Immutable list of class definitions; never rely on its declaration order for traversal. |
| `ValidatedGraph.cs` | Ordinal dictionary, canonical node order and declared ordered adjacency; construct only through successful validation. |
| `Diagnostic.cs` | Stable code, human message, JSON pointer, optional class and closed cycle path. |
