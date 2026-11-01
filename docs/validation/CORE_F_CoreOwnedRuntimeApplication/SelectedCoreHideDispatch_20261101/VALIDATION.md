# Selected Core Hide dispatch — validation

Date: 2026-11-01

Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`

Base: `2225ed5d06b3120b600acd09680f561e6a8415ce`

Implementation candidate: `9414824baadc31e59f87c01184054640b2f2003f`

Implementation tree: `abe1455f154f9bd4020db3d26a77c66d0d8706ce`

Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Bounded scope

Selected Daily-v1 Hide dispatch can use the actor's exact admitted Core action slot when the Unity `NpcActionRuntime` execution projection is absent. The path requires the exact selected owner graph and action definition, an enabled targetless Hide payload, the exact bound NPC runtime, and the live admission context owned by the current thread. A present but stale Unity projection fails closed. Core performs the Hide resolution and owner writes inside the existing P12 Crime/Justice operation scope; this does not create a second action authority.

The regression verifies that rejected admission and stale projections leave owner revisions and mutation epoch unchanged. Successful Hide uses one deterministic draw, retains the exact Justice owner, updates the hidden timer and status through the existing NPC owner, and publishes the two expected NPC Crime/Justice owner writes.

This slice does not complete all selected action dispatch, the shared `DailyV1Application`, the detached .NET host, or CORE-F. It does not promote Core canonical.

## Source identity

The official Unity runner captured 1,974 validation inputs with stable before/after digest `2295ffbf1683876f5a1cd46051408ec4a57018cd6fdcbc969f9ca61dbcb3a355`. The run began at base HEAD `2225ed5d06b3120b600acd09680f561e6a8415ce`; the committed candidate contains the exact source files captured by those runs:

| File | SHA-256 |
|---|---|
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `49c0f516b7ebb6864b2fcde471e2d51c17ad701e9d5fe3beecc708ba598c0dd5` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `b42ac3114b62df2cd8c5543e62b933cca2fe1c7b2ce2f62f30ff96bf863497df` |

The candidate commit changes only these two paths relative to its base. The full and focused runs used the same stable input digest; no production or test source changed between those runs and the candidate commit.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Focused regression `SelectedDailyV1HideDispatchBoundaryUsesExactCoreSlotWithoutUnityActionProjection` | **1/1 PASS** | Run `79e39434b63c47b88b01a2c3f942c1e2`; private run-manifest SHA-256 `5ebb41e118ada10115595b9ffeb6fd4022fd5b30ba3060ccb2d2c3638384ae32`; original XML SHA-256 `90be25f816bc55bf79cc2c045bcf0a6fd622c2d56f7dd49fc2eead60e28d95b1`. |
| Full Unity EditMode | **3285/3285 PASS**; zero failures, skips, or inconclusive | Unity `6000.3.9f1`; run `d4d36344007c4a7da730e7d0a44e3649`; private run-manifest SHA-256 `3c139bd558e7dc0cedd4ea173044a4830353d252f8fa8cdffce8a23a59ea3139`; original XML SHA-256 `00f729794c22bac618c7534c47bd7d5ad3359ad19c5a633d629aa301e10cfd9a`. |
| Official filtered Smoke | **5/5 PASS**; zero failures, skips, or inconclusive | EditMode filter `Smoke`; run `2b0c4993ba4e4ffea3c989aa582a6e65`; private run-manifest SHA-256 `1e8ee24d8b83f899745d9905d29f0dd9a8670c087bb867e3ca0411b339bf8edd`; original XML SHA-256 `ce1df4b6168d4dbdf221e58957d428df771f30444e8378f9dfb8dbb9c1d37766`. |
| Privacy-safe result package | **Verified** | [Normalized manifest](UnityEvidence/coref-hide-dispatch-editmode-20261101.manifest.json), SHA-256 `ea405fd516d9ab401a0522c04d2ba68f4f3e12bb53274ab0ac3aad077fcfbe90`; [result-only archive](UnityEvidence/coref-hide-dispatch-editmode-20261101.zip), SHA-256 `d8bde1a52a256da88bec9165ea0b1982a7badef905d0c990f58f29e4438bbb85` (173,787 bytes). The project verifier returned `VALID: runs=1 entries=1` against the protected original run manifest. |
| `git diff --check` | **PASS** | Candidate source diff. |

The full EditMode report includes the five Smoke identities. The raw XML/log reports and private run manifests remain in owner-only local evidence storage; they were not copied into this record. The normalized package contains result XML only and redacts machine-local paths. CoreValidation was not rerun because this change touches only Unity runtime adapter code and its Unity EditMode test; the full Unity suite compiled and exercised both.

Independent exact-tip review: **VALIDATED_CANDIDATE — PASS**, recorded in [REVIEW.md](REVIEW.md).