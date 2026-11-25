# CORE-F TravelParty mutation-epoch reservation review

**Verdict:** `VALIDATED_CANDIDATE` — exact-tip independent review passed.

- Candidate branch: `codex/core/CORE-F-try-start-transaction`.
- Reviewed code commit: `fe3a23ef95456519a2b58425bc642caa84ce9b1f`.
- Reviewed code tree: `53ac06f4ca0a94cc41f0f0624405d9582d7bfb0c`.
- Parent: `a35ac4a2e75439b1ed3d631b25cbdb1782b35ab0`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Independent reviewer: `/root/review_tparty_core_scope`, read-only.

## Finding resolved

The prior review found that Core's new TravelParty operation admission no
longer rejected an outstanding shared mutation-epoch reservation, unlike the
previous `ContinuationCensusProtocol.TryEnterOperation` path. The correction
checks `DailyV1MutationEpochState.HasActiveReservation` inside the serialized
`operationSyncRoot` admission gate, before protocol validation or operation
scope registration. The current owner-thread admission rule keeps reservation
and operation entry serialized.

`TravelPartyAdvanceRejectsWhileMutationEpochReservationIsActive` holds a
reservation from the exact execution state's shared mutation-epoch state inside
an enclosing DailyAdvance. It verifies the operation rejects before its
callback, the enclosing operation count is unchanged, the P12 protocol
fault-closes once, and the reservation remains held until explicitly released.
It also verifies the operation count returns to zero and capture eligibility
returns to idle after cleanup.

The reviewer found no remaining code findings. Exact owner-section and
unchanged-section checks, committed-mutation notifications, nested runtime
scope accounting, capture blocking, and fail-closed cleanup remain intact. The
change does not alter owner identities, P12 census membership, persistence
schema, or daily behavior.

## Validation evidence

The validation files are retained in the private owner-only E: evidence store;
this record publishes run identities and hashes only.

| Gate | Run | Result | Evidence identity |
| --- | --- | ---: | --- |
| CoreValidation cold | `261a570d1da24e909ebf46cb5e0f2525` | 1003/1003 PASS | Core manifest SHA-256 `86b4bec96c904f25197fb8dc51b174ba99527e5888ccfc09a79bdac199aefdd3`; cold TRX SHA-256 `5526c1a812f8a2a0a086d78441bb16a89e232a1d16c9288d8344c0007dd36ba1` |
| CoreValidation warm | `261a570d1da24e909ebf46cb5e0f2525` | 1003/1003 PASS | warm TRX SHA-256 `2d93a09b639742bc19276ffd742a88f4bc9414890cb81adf379b715b13b33b5c` |
| Unity EditMode | `6d3a4b4343ba4e2e96f2196a4d271a8f` | 3296/3296 PASS | stable-input manifest SHA-256 `438281d3c07b6f12b3711ec8a317ea8b988f9d25a3c58cb9c7da5d640d7faf1c`; XML SHA-256 `1312d1815496c6dc523aa3a8281bb681f2facd5440f0d9506e67e4c5591778f2`; log SHA-256 `4fa4c433ff6632072765c85fff0aeb19fcd5702220839cb01426720ab51db2fc` |
| Official Smoke | `bdff55b97fd045afb06d9a9cbf07c64e` | 5/5 PASS | stable-input manifest SHA-256 `5269bcec33cd90ab10170b74e67af25fc5d54b03665cbcea90ff4d6aabb79088`; XML SHA-256 `7f99122b90df92eebfddb51f13159f4903a51be5b745bf14c2c9b830eb68a0ef`; log SHA-256 `49ca34d27b573d8aabc3fc447204e86343cbfa9ee91415d46540349faa69a624` |

The named new regression passed in both Core cold and warm TRX files. Core
source/test hashes recorded in the CoreValidation input manifest match the
reviewed files: Core source `8aa110a8360f9365b57576b5846abd401b530e72394e5009d588d2ec73bd2aaf`,
Core test `c9824cb0cd77f26cc1783c6844b4c28f349a4553d0efd3156b2e57ae0e79d10f`.
Both Unity manifests record the same Core source hash and stable inputs. The
validation was run on the changed working-tree source before it was committed;
the commit materialized those exact source/test bytes without further edits.
`git diff --check` passed on the committed code change.

## Scope and disposition

This review closes only the TravelParty P12 mutation-epoch reservation finding
for the cited candidate. It does not complete the CORE-F application/host
package, approve Core-canonical promotion, or claim detached .NET continuation.
Core canonical remains unchanged.
