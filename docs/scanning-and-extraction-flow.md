# Binary JSON Scanning and Extraction Flow

This document explains how payload scanning and document extraction work in ProjectSift.

## Overview

- The scanner walks raw payload bytes and discovers top-level JSON candidates (`{...}` or `[...]`) inside binary framing.
- Each discovered candidate is extracted independently so one malformed candidate cannot block later candidates.
- The extractor validates JSON using `System.Text.Json` and produces sorted, deduplicated property paths.


### Scanner flow

1. Initialize payload buffer and set `position = 0`.
2. Read current byte.
3. If byte is not `{` or `[`, increment position and continue.
4. Run boundary scan from current position.
5. Handle boundary status:
     - **Complete**
         - Slice candidate bytes from `position..end`.
         - Run extractor.
         - If extracted status is **Successful/Recovered**, jump `position` to boundary end.
         - If extracted status is **Failed**, continue scanning from next byte (resync behavior).
     - **RecoverableIncomplete**
         - Append deterministic closing delimiters.
         - Run extractor and mark result as **Recovered** when extraction was successful.
         - If recovered result is non-failed, terminate scan.
         - If failed, continue scanning (resync behavior).
     - **TooDeep / TooLarge / MismatchedBoundary**
         - Add failed `DocumentResult` with diagnostic.
         - Continue scanning.
     - **AmbiguousIncomplete**
         - Add failed `DocumentResult` with diagnostic.
         - Terminate scan.
6. After loop:
     - If no documents were discovered, add payload-level `NoCandidatesDiscovered` diagnostic.
7. Return `PayloadResult`.

### Extractor flow

1. Validate candidate content:
     - Empty => failed `CandidateEmpty`
     - Over size limit => failed `CandidateTooLarge`
2. Parse with `JsonDocument.Parse(...)` using configured max depth.
     - Parse exception => failed `CandidateInvalidJson`
3. Validate root kind:
     - Not object/array => failed `CandidateUnsupportedRoot`
4. Collect property paths recursively:
     - object property => append segment
     - array => append `[]`
     - nested object/array => recurse
     - enforce max path count/depth limits
5. If collection exceeds limits => failed `CandidatePathLimitExceeded`.
6. Deduplicate, sort ordinally, map to `PropertyPath`, return successful `DocumentResult`.

## Boundary status decision table

| Boundary status | Action | Continue scanning? |
|---|---|---|
| `Complete` | Extract original candidate | Yes (or skip to boundary end on non-failed extraction) |
| `RecoverableIncomplete` | Append deterministic closers, extract, mark recovered | Yes if failed; otherwise stop |
| `TooDeep` | Failed document diagnostic | Yes |
| `TooLarge` | Failed document diagnostic | Yes |
| `MismatchedBoundary` | Failed document diagnostic | Yes |
| `AmbiguousIncomplete` | Failed document diagnostic | No |

## Pseudocode

```text
for each byte position in payload:
    if byte is not '{' or '[':
        continue

    boundary = scanBoundary(position)

    switch boundary.status:
        Complete:
            result = extract(originalCandidate)
            add result
            if result is not Failed:
                position = boundary.end

        RecoverableIncomplete:
            result = extract(repairedCandidate)
            recovered = toRecovered(result)
            add recovered
            if recovered is not Failed:
                stop scanning

        TooDeep/TooLarge/Mismatched:
            add failed result

        AmbiguousIncomplete:
            add failed result
            stop scanning

if no document results:
    add payload-level no-candidate diagnostic
```

## Extractor path rules

- Object properties use dot notation: `customer.id`
- Arrays use `[]`: `items[]`, `items[].sku`
- Reserved property names use bracket-quoted escaping: `['a.b']`, `['x[y]']`
- Paths are deduplicated per document and sorted using ordinal comparison.

## Recovery and determinism

- Recovery is attempted only when the candidate reaches payload end with unambiguous missing closing delimiters.
- If ambiguity exists (for example, still inside a string), recovery is not attempted and a failed result is produced.
- Candidate failures are isolated; scanning continues when deterministic resynchronization allows it.
