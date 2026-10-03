# Domain contracts and report format

## C# API shape
Use sealed records for values and immutable arrays (or defensively copied read-only arrays). Do not return read-only wrappers around mutable backing lists that the algorithm still modifies. Prefer enum statuses internally and explicit camelCase wire values.

```csharp
public sealed record ClassDefinition(string Name, ImmutableArray<string> Bases);
public sealed record Hierarchy(ImmutableArray<ClassDefinition> Classes);
public sealed record TraceOptions(int MaxEvents);
public sealed record Blocker(string SequenceId, int AbsoluteIndex, int RemainingIndex);
public sealed record SequenceSnapshot(string Id, string Kind,
    string? BaseName, ImmutableArray<string> Items, int Cursor);
public sealed record PrecedenceEdge(string Before, string After,
    string SequenceId, int BeforeIndex, int AfterIndex);
```

Implement GraphValidator.Validate(Hierarchy) returning either ValidatedGraph or ordered diagnostics. ValidatedGraph owns a unique ordinal dictionary plus canonical class order and declared base order. C3Linearizer.Linearize(ValidatedGraph, string target, TraceOptions) returns LinearizationResult. Missing targets must be checked before entering this API. Avoid null-forgiving operators used to bypass outcome checking.

LinearizationResult: requestedTarget; status success/inconsistent; targetMro (only success); perClassResults in completion order; failure (only inconsistent); metrics; retainedEvents; traceTruncated; droppedEventCount. Failure: failingClass, partialPrefix, sequence suffix snapshots, blocked candidate occurrences, witness cycle with edge provenance. For an ancestor failure, requested target is still present, targetMro=null and partialPrefix belongs exclusively to failingClass.

Sequence snapshots retain the immutable original `items` and an absolute `cursor`; the remaining suffix is `items[cursor..]`, including an empty suffix for exhausted sequences. This preserves absolute blocker/witness indices without copying or renumbering tails. Text and SVG display only that suffix. The handwritten conflict example uses suffix-only items plus `originalCursor` for readability; it is not a v1 wire report.

## Event envelope
Each retained event has sequenceNumber (1-based across attempted emissions), kind and className. Additional fields depend on kind:
- ClassStarted: ordered direct bases.
- BaseResultReused: baseName and completed order; emitted once per direct base while constructing a non-root's merge.
- MergeStarted: full initial immutable sequences, including the direct-base sequence.
- StepStarted: mergeStep (1-based), cursors in original sequence order.
- CandidateExamined: mergeStep, sourceSequenceId, candidate, accepted bool and ordered blocker list. accepted is exactly blockers.Count == 0.
- CandidateSelected: mergeStep, candidate, cursorsAfter and partialOrderLength. Occurs after the accepted CandidateExamined.
- ClassCompleted: immutable order.
- ClassFailed: failure object or a stable reference to the complete report failure.

Sequence IDs are local to the class: base:0, base:1, ..., direct. Every event also carries className, making the pair globally unambiguous. Sequence kind is baseLinearization or directBases. Indices are zero-based. Candidate checks include accepted and rejected occurrences, including duplicate head names from different source sequences. BaseResultReused is informational, not an algorithm recomputation.

Trace collector increments attempted event count for every emission. Retain the first MaxEvents only. Dropped count is attempted minus retained. A separate report flag summarizes truncation; do not insert a synthetic event over the limit. Summary results and failure details are never dropped. Event payload construction should be skipped after cap when it is unnecessary for final diagnostics.

## Metrics
All counters are nonnegative 64-bit integers and cover all computed classes in the target closure, up to a failure:
- classesStarted / classesCompleted
- mergeSteps: each attempted step, including a failing step
- candidateChecks / rejectedCandidates / acceptedCandidates
- tailElementComparisons: every ordinal name-equality test inside a tail scan
- headRemovals: every advanced sequence cursor
- eventsAttempted / eventsRetained / eventsDropped

acceptedCandidates excludes each class's initial self prefix, including roots. No wall-clock values in the v1 semantic report. Do not call these metrics speedup, parallel depth or runtime benchmarks.

## JSON report v1
Top-level exact keys:
```json
{
  "schemaVersion": 1,
  "command": "trace",
  "status": "success",
  "target": "D",
  "graph": { "classes": [] },
  "mro": ["D", "A", "B", "O"],
  "classResults": [],
  "diagnostics": [],
  "failure": null,
  "metrics": {},
  "trace": { "truncated": false, "droppedEventCount": 0, "events": [] }
}
```
Graph classes are ordinal by name and include all declared classes; bases retain declaration order. classResults use stable completion order and contain className/order. Event objects use kind-specific explicit fields; do not serialize CLR type names. Status values: success, invalidInput, inconsistent. Graph may be null for invalid input. mro is null on invalid/inconsistent, including ancestor failure. Metrics may be null if no engine ran. Always include diagnostics array (possibly empty).

For validate, command=validate, target is supplied target or null, mro=null, classResults=[], failure=null, metrics=null and trace.events=[]. Success only states structural validity. Diagnostic object: code, message, jsonPointer (nullable), className (nullable), path (nullable closed cycle). Do not make tests depend on entire prose messages except focused text snapshots.

Canonical diagnostic codes: InvalidJson, InvalidShape, DuplicateProperty, UnknownProperty, InvalidName, DuplicateClass, DuplicateBase, MissingBase, MissingTarget, UnknownTarget, InheritanceCycle, LimitExceeded. CLI usage and filesystem failures go to stderr with exit 2 or 1 respectively; reports are not required for those operational failures.
