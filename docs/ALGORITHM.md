# C3 algorithm and diagnostics

## Evaluation order
Create an ordinal class index. Validate inheritance cycles on the entire graph first. For the target, use an explicit DFS frame stack carrying (class, nextBaseIndex), following bases in declared order. Emit a class once after its bases have completed. This gives a stable base-first postorder with shared ancestors computed once. Keep a cache of immutable successful orders. If a class fails, stop the evaluation and return its failure with the requested target recorded separately.

The engine may emit BaseResultReused for each direct base when constructing a class merge, because its previously completed result is being reused. Do not promise recursive cache-hit counts: use the fixed event semantics in CONTRACTS.

## Merge pseudocode
```text
linearize(C):
    bases = directBases(C)
    if bases.empty:
        result = [C]
        emit ClassStarted, ClassCompleted
        cache immutable result
        return success

    sequences = [immutable L(B) for B in bases]
                + [immutable copy of bases]
    attach stable provenance to every sequence
    cursors = zeros(sequences.count)
    result = [C]
    emit MergeStarted(initial sequences)
    step = 1

    while any sequence has remaining elements:
        emit StepStarted(step, copied cursors)
        selected = none
        for source in sequences in original order:
            if source exhausted: continue
            candidate = source[cursor(source)]
            blockers = []
            for sequence in sequences in original order:
                for position from cursor(sequence)+1 to last index:
                    increment tailElementComparisons
                    if sequence[position] == candidate:
                        blockers.add(sequence id, absolute position,
                                     remaining-relative position)
            increment candidateChecks
            emit CandidateExamined(step, source id, candidate, blockers)
            if blockers.empty:
                selected = candidate
                break
        if selected is none:
            snapshot remaining sequences and result
            construct precedence witness
            emit ClassFailed
            return typed failure
        result.append(selected)
        for sequence in sequences:
            if not exhausted and sequence[cursor] == selected:
                cursor += 1
                increment headRemovals
        increment acceptedCandidates
        emit CandidateSelected(step, selected, copied cursors, result count)
        step += 1

    cache immutable result
    emit ClassCompleted(result)
    return success
```
The candidate's own tail is scanned as well; valid sequences have no repeated nodes, but using the same rule for all sequences is simpler. Do not count cursor-head equality checks in tailElementComparisons. Empty direct-base lists on roots do not create a merge or candidate checks.

## Why it works
An element in a remaining tail still has a predecessor that must appear first. Selecting it would violate an inherited or local sequence order. Accepting only an unblocked head respects all such orderings, and scanning sequence heads in their defined order fixes a deterministic choice. Prepending C gives the subclass first priority.

## Failure witness
Use only the remaining nonempty sequence suffixes at the failure point. For each adjacent pair u,v add precedence edge u→v with provenance (sequence id, absolute positions). Combine parallel edges but retain all sources. Search the derived directed graph for one cycle using iterative color DFS, ordinal node and neighbor order. A back edge to an active node closes a cycle; rotate the closed cycle to begin with its ordinal-smallest node without reversing edge direction. Pick first provenance by original sequence index for each cycle edge, retain other sources optionally.

A stuck merge implies a cycle in these remaining precedence requirements: every available head has some required predecessor. If no cycle is found, treat that as an engine invariant failure (exit 1), not an ordinary inconsistent result. Do not assert that the witness is minimum length or a minimal unsatisfiable subset.

An inheritance cycle is detected in the original derived→base graph before any C3 computation. The precedence witness graph is different; never draw it as an inheritance edge.

## Work and memory
Let a merge have k sequences, at most T initial sequence entries and u unique emitted classes. A direct scanning implementation examines up to k heads each step, each scanning at most T entries: O(u*k*T) upper bound for that merge. Count actual tail equality operations. Total work is the sum over computed classes; cached MRO storage can be O(V²). Event snapshots can dominate storage; cap retained events and store cursors rather than full repeated lists. Do not label the implementation universally O(V+E).
