# CORE-F P8-C Position Owner — Independent Exact-tip Review

**Verdict: PASS — `VALIDATED_CANDIDATE` for the bounded P8-C person-position authority move.** This record does not approve Core canonical promotion or claim CORE-F completion.

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Exact candidate: `e2f0a4c2d1e9a769949223a4f020ba027a9bc198`
- Candidate tree: `ecbfff6af53d77afc7ffae11b48683bea0834ad3`
- Exact review base: `c0f5873d940764b0ac002e7c242dab5c459c6bf9`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate remote and local branch were synchronized at the reviewed SHA.

## Scope reviewed

The complete `c0f5873..e2f0a4c` delta moves `PersonSpatialPositionStore` and
`P12P8CCensusProviders` into `Simulation.Core`, preserves the original Unity
`.meta` identities, adds the existing Core passage-traversal option resolver,
and binds the position store to the selected runtime owner graph. The Unity
adapter now obtains transit state through that graph.

The move preserves `PersonId`-keyed identity, owner cardinality, revisions,
supported Hex/Location/Crossing resolution, and the existing position
transitions. Core resolution follows the same `SpatialAuthorityStore` path
used by the Unity projection adapter. SubLocation remains outside this store's
contract. The owner graph rejects a store associated with a different
`PersonStore`, and membership graph replacement carries the binding forward.
No original Assets implementation remains alongside the Core source.
P8-C census and the selected P12 exact-empty boundary are unchanged.

## Validation evidence

- Official CoreValidation cold and warm: **668/668 PASS** each, zero failures
  or inconclusive results. The protected report and log hashes match the
  private run manifest. The manifest records aggregate worktree-input identity
  and included the local ProjectSettings inputs; it does not provide per-file
  code hashes. The changed source was committed without further tracked source
  edits after the run.
- Full Unity EditMode: **3254/3254 PASS**, zero failed, skipped, or
  inconclusive. The normalized result-package archive SHA-256 is
  `5fd2d96324b531c4a28f1a92eb7b66fb7dfd7e8309629437f087aa70a19255e7`.
  The package manifest verifies the archive; its captured hashes for changed
  Unity source files match the reviewed candidate.
- Official Smoke cases: **5/5 PASS** in the full EditMode report. A separate
  filtered Smoke execution also reported 5/5, but its XML failed the approved
  packager's parent-suite count-consistency check, so that filtered XML was not
  packaged or treated as independent package evidence.
- `git diff --check`: **PASS**.

These evidence caveats limit provenance claims for the aggregate Core input
manifest and standalone filtered Smoke artifact; neither demonstrates a code
defect. No protected ProjectSettings edits, private evidence, temporary files,
or build outputs are included in the candidate commit.

## Review result and limitations

No actionable implementation defects were found. This validates only the
bounded position-owner integration and its evidence. Shared authoritative
runtime/session ownership, `DailyV1Application.TryStart`, detached P12 restore,
the fixed .NET host, and separate-process T/T+1/T+2 continuation remain
unimplemented acceptance requirements. The candidate must not be described as
complete CORE-F or promoted to Core canonical on the basis of this review.
