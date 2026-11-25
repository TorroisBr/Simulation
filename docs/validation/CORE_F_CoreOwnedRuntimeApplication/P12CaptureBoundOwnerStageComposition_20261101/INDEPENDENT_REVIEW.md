# Independent review — P12 capture-bound Core owner-stage composition

## Verdict

**PASS** for the bounded capture-bound restore composition change. This is a
candidate checkpoint only; it is not a Core canonical promotion or full
CORE-F acceptance.

## Exact identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit reviewed: `18aca8b085102b9e7dee9d5f8e5f25a815b9f146`
- Candidate tree reviewed: `19ff191564cb7de8254101c236b04c74811caa0c`
- Candidate base: `3eb0b515e08c74c9a2af3ce504de6bfec7e04e99`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Findings

No actionable findings.

The independent reviewer confirmed that P12-D retains the Core D stage made
from its exact selected owners, identity index, PersonStore, and Genealogy
store; P12-F retains its exact staged Core owners; and the application restore
path requires all D/E/F stages and uses
`TryCreateRestoredActiveRuntimeSessionFromOwnerStages` for both detached-image
and capture-bound restores. There is no fallback to Unity owner reselection
when an application is present. Existing capture-attempt checks, whole-target
validation, and later publication remain in place. The exact admitted
definitions/effective-configuration reference and the P12-F PersonStore
binding checks remain enforced.

The reviewer checked the full EditMode package and private run manifest
identities recorded in [VALIDATION.md](VALIDATION.md), including the manifest
and archive hashes, 3294/3294 result accounting, and all three changed
production-file hashes. The package verifier returned `VALID` for its one
normalized report entry. The reviewer did not edit files or rerun tests.

## Validation boundary

The filtered test runs passed and remain preserved in protected local
retention. Their separate public packages were refused by the evidence
packager because the filtered NUnit XML retains parent suite counts for the
full test tree. The full unfiltered package is valid and contains the complete
test tree. No original result was rewritten.

This checkpoint does not complete `DailyV1Application.TryStart`, move the
whole P12-G transaction into Core, establish the detached .NET host, prove
separate-process T/T+1/T+2 parity, or close CORE-F.
