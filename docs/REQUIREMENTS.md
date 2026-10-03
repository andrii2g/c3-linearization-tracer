# Requirements and explicit decisions

## Input contract
Top-level JSON object: `target` (optional string) and `classes` (required nonempty array). A class object has exactly `name` (required string) and `bases` (required array of strings, which may be empty). Reject unknown properties, repeated properties, nulls, wrong types, comments and trailing commas. Property matching is case-sensitive. A supplied target must have the same name syntax as a class. For trace, the effective target is CLI --target if supplied, otherwise JSON target; missing effective target is an error. A non-existing JSON target is ignored only when a valid CLI override replaces it. Validate does not require a target, but checks a supplied target exists.

Names: 1–80 ASCII characters, first `[A-Za-z_]`, remainder `[A-Za-z0-9_.-]`. No trimming, case folding or Unicode normalization. `A` and `a` are different classes. Require unique names and unique direct bases for each class. No implicit root and no special treatment of the names O or object. Explicit multiple roots are allowed. Every base must be declared, including forward references.

## Validation stages
1. Argument syntax and input file read limit.
2. JSON syntax and shape; duplicate/unknown properties and required fields.
3. Name/array constraints and graph references; collect structural diagnostics in stable JSON-pointer order.
4. If structural errors exist, stop before traversal.
5. Validate the whole graph for inheritance cycles using stable ordinal start-node order and declared base traversal order. Report first cycle as a closed path.
6. Resolve the effective target and its closure. In trace mode, C3 only evaluates that closure. Disconnected structurally valid C3 inconsistencies do not fail an unrelated target.

`validate` means structural validation and acyclicity only. It does not claim C3 consistency. A successful message must say “Graph valid; C3 not evaluated.”

## Limits
Reject input larger than 1 MiB, more than 256 classes, more than 32 bases per class, more than 2,048 total edges or JSON nesting deeper than 16. Limits are checked before expensive work. Trace default cap: 20,000 retained events; --max-events allows 0–100,000. Reaching this cap only truncates retained events; it does not change computation, metrics or final failure diagnostics. Do not silently drop graph nodes from SVG.

These are teaching-tool limits, not claims about the general complexity of C3. Use iterative traversal for cycle detection, closure and dependency evaluation. CLI I/O cancellation is outside the MVP.

## Semantics
Deterministic first-eligible-head merge. At each candidate occurrence scan all active sequence tails in sequence order. Repeated head occurrences are tested separately if earlier occurrences were rejected. Stop candidate enumeration immediately upon accepting the first eligible head. Remove selected class from every current head equal to it. Never delete tail entries as a shortcut.

Successful order begins with the class, contains its complete reachable closure exactly once, preserves ordered direct bases, and preserves each base's entire MRO as a subsequence. Monotonicity here means descendants cannot reverse an established inherited ordering. A diamond alone is not an error; C3 is a policy for resolving multiple paths, not a claim to infer programmer intent.

## Determinism
Ordinal class identifiers and tie breaks; direct-base order remains untouched. No timestamps, generated GUIDs, environment paths or durations in semantic JSON/SVG. LF newlines, UTF-8 without BOM and invariant numeric formatting. Input declaration permutation must not change a successful semantic report or SVG. Syntax diagnostics may change JSON-pointer locations with input declaration order.

## Non-goals
No real class generation, reflection-driven C# method resolution, Python runtime requirement, alternative MRO algorithms, animation, Graphviz, browser UI, performance comparisons, database, telemetry service or dependency injection framework.
