# CORE-F selected P8 owner binding — exact-tip independent review

## Verdict

**PASS — `VALIDATED_CANDIDATE` for the bounded selected P8 owner binding.**
The review applies to code candidate
`4c23abb046ef3dceb723821bf4fff1f173039894`, tree
`709a062129f479430cdc8018aa0f0f51de58dd57`, based on
`4b025b3ad18dbf6283cc44ba6d809863b9686d9c`. Core canonical at review was
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Scope reviewed

The full base-to-tip diff composes the selected `PersonSpatialPositionStore`
against the exact Core `PersonStore` and spatial authority, binds that store
and the exact legacy-anchor owner into the selected runtime graph before
publication, and makes the selected Unity path retain those exact instances.
The nonselected compatibility path retains its existing clone behavior.

The prior review finding is resolved. The P14 local-flow fixture retains
`HasLocalDailyMaterialFlow`, keeps its bound P8 owners exact-empty, builds the
P8 rows from the live graph providers, and asserts that the exact P8 witness
matches before checking that P12 save admission rejects the excluded City
state. Existing denial, failure-category, null-operation, and scope-balance
assertions remain intact. No P12 profile widening or CORE-F completion claim is
introduced.

## Validation evidence and review limit

The candidate validation record reports CoreValidation 918/918 cold and warm,
Unity EditMode 3275/3275, and official Smoke 5/5. The corrected Core test hash
is recorded in the revalidation note and matches the source hash verified by
the orchestrator against the official manifest. The independent reviewer
could not open the owner-only manifests to recompute their artifact hashes;
that access limitation is preserved here. Unity results remain applicable
because only the standalone .NET fixture and documentation changed after the
Unity runs, and the Unity production-source hashes are unchanged.

No actionable implementation finding remains. This is not Core canonical
promotion. Shared runtime/session ownership, `DailyV1Application.TryStart`,
detached P12 restoration, the fixed .NET host, and separate-process
T/T+1/T+2 continuation remain outstanding CORE-F work.
