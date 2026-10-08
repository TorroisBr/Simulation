# P12-C identity and sequence snapshots — implementation review

**Verdict:** PASS — exact-tip validated candidate.

## Review boundary

- **P12 canonical base:** `f23fe5a1c70dce8cb32a4ca6aa088820b3ad7279`
- **Architecture:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- **Candidate branch:** `codex/phase12/P12CIdentitySequenceSnapshotCurrentBase`
- **Reviewed docs tip:** `e12e7bf8c27e9ebbcf2a196685da0f5c7d1f06d9`
- **Code tip:** `d9ce4502b6b1601660f2c44629d6e0f34c72036d`
- **Code tree:** `5222c38f4c4e56313efa1a7caa7542e830d644ba`
- **Assets tree:** `4e41a3dac9162f632f14daa9f9a978308e959371`
- **Independent reviewer:** `/root/p20c_review`

The reviewer inspected the complete base-to-code diff, the exact-source manifest, all retained test XMLs and the archived Unity logs. The reviewed implementation adds immutable snapshot DTOs and strict private staged reconstruction for the fourteen existing `RuntimeIdAllocator` counters and `SimulationRecordSequence`. It preserves current census identities/revisions, P12 mutation hooks, runtime rebinding, P11 ActorChoice records, P18-D occurrence receipts, and the selected Daily-v1 boundary. No out-of-scope owner, record payload, or broader save capability was added.

## Validation evidence

The exact-tree validation is retained in [`P12CIdentitySequenceSnapshotCurrentBase/VALIDATION.md`](../validation/P12CIdentitySequenceSnapshotCurrentBase/VALIDATION.md). The corrected source table hashes raw Git blob bytes from code tip `d9ce450`. The reviewer confirmed all five values match those blobs and each validation worktree file normalizes through `git hash-object --path` to the same candidate blob. The reviewer also verified the nine XML hashes, the `RawLogs.zip` hash, and the archived diagnostic XML hash.

- IdentitySequenceSnapshotTests: 4/4
- RuntimeIdAllocatorCensusTests: 3/3
- SimulationRecordSequenceP12InvalidationTests: 17/17
- ActorChoiceCensusTests: 9/9
- ActorChoiceRuntimeTests: 11/11
- P18DConsumerIntegrationTests: 10/10
- Official Smoke: 5/5
- SimulationRuntimeLongRunTests: 7/7
- ALL EditMode: 2466/2466
- `git diff --check`: PASS

The review first returned NEEDS_CHANGES solely because three source hashes were recorded as worktree-byte hashes while the reviewer compared committed blobs. The validation manifest was corrected to use raw committed-blob SHA-256 values and document the `core.autocrlf=true` mapping. This correction changed no executable files; the Assets tree remains the reviewed `4e41a3d` tree. The reviewer then rechecked exact docs tip `e12e7bf` and returned PASS. No other blocking findings remain.

## Scope and limits

This is partial P12-C delivery for allocator/sequence snapshots only. It does not complete P12-C or P12-A, establish profile-wide export/hydration, capture P9-B provenance, P8-A geography, deterministic-random roots, or close Phase 12. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. Promotion still requires the refreshed canonical preflight and State update.