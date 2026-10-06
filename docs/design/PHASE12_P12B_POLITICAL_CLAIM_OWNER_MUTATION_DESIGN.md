# P12-B PoliticalClaimStore owner census and mutation-epoch design

**Work package:** P12-B owner/operation/epoch reconciliation; no new numbered checkpoint ID.
**Canonical base:** `4d015062c28061148eb9926a6d23799681531afe`.
**Architecture revalidation:** latest Architecture General docs available at `e16796014d348e3b59da7ed848101c4c03926ba5`; the §2/§92A development-artifact and new-owner rules do not change the already accepted P9-B-only Daily-v1 profile or add a domain owner in this slice.
**Accepted scope authority:** `docs/design/PHASE12_CAPABILITY_CHECKPOINT_DECOMPOSITION.md` P12-B owner/admission capability authorization and P12-E's included claims/recognition owner set; current blocker matrix in `docs/design/PHASE12_B_BLOCKER_RESOLUTION.md`.
**Status:** design candidate; implementation readiness waits on independent design review.

## Current boundary

`SimulationRuntime` always owns a cloned `PoliticalClaimStore`. The selected Daily-v1 composition passes no source `politicalClaimStore`, so the installed runtime owner begins empty. Runtime cloning validates copied claim and recognition endpoints/times and does not retain the input Store object. The store exposes claim `Count`, `Revision`, and immutable sorted snapshots for claim and recognition records. Claim and recognition rows share that one owner revision. Recognition has no direct count accessor today; add a read-only `RecognitionCount` property backed by the existing recognition dictionary so the census avoids allocating a snapshot just to count rows.

The three existing public commit facades are `TryRegisterPoliticalClaim`, `TryApplyPoliticalClaimRecognition`, and `TryApplyPoliticalClaimResolution`. Each currently increments `PoliticalWorldRevision` after its successful store commit and has no P12 section registration or mutation-epoch notification. Store writes otherwise occur during the private constructor clone; proposals, lookups and validation are not commits. The selected-profile source scan found no other production callsite for these facades or internal Store write methods. Do not add a caller, gameplay rule, policy, claim kind, or persistence behavior.

## Bounded owner contract

Register two schema-v1 `Required` sections against the exact installed runtime `PoliticalClaimStore`:

| Section | Cardinality | Revision | Initial Daily-v1 value |
|---|---|---|---:|
| `p12e.political-claim.records` | `PoliticalClaimStore.Count` | `PoliticalClaimStore.Revision` | 0 |
| `p12e.political-claim.recognitions` | `PoliticalClaimStore.RecognitionCount` | `PoliticalClaimStore.Revision` | 0 |

Both providers carry the same stable owner identity and revision source. Their counts remain independent. This is dynamic row census after publication: a later supported claim or recognition commit changes the relevant count and the common revision. Claim resolution changes neither count, but it still changes the shared revision and must refresh both section witnesses. Recognition updates may replace an existing relation without changing its count; both revision witnesses still refresh.

Add one operation ID, `p12.political-claim.owner-commit`, admitting exactly these two section IDs. Use the existing runtime-admission and mutation-operation protocol; do not add a second epoch, a generic transaction manager, or a new lock.

## Existing commit paths and ordering

For each façade, retain all current argument, identity, target, timeline, and transition validation before the store commit. Immediately before the existing store/System apply call, the selected P12 profile must preflight the owner thread, exact installed owner identity, both unchanged section baselines, and capacity for one shared-epoch advance. Runtimes without a P12 admission context continue to use the existing domain path without P12 publication.

1. `TryRegisterPoliticalClaim`: preflight after current claimant/target/day/resolution checks and immediately before `politicalClaimStore.TryRegister`.
2. `TryApplyPoliticalClaimRecognition`: preflight after the existing transition/current-day checks and immediately before `PoliticalClaimSystem.TryApplyRecognition`.
3. `TryApplyPoliticalClaimResolution`: preflight after the existing transition/current-day checks and immediately before `PoliticalClaimSystem.TryApplyResolution`.

After each successful store commit, advance the existing `PoliticalWorldRevision`, then notify both claim sections once in the enclosing operation. Existing store-local revision overflow and all domain failure codes remain authoritative. Failed/stale/rejected operations produce no census change and no P12 epoch advance. If notification fails after a commit, preserve the committed claim truth and fault the runtime according to the existing P12 adapter convention; never roll back a successful domain commit solely to hide a failed notification.

## Admission and integration

Register these providers during `SimulationRuntime.InitializeNpcRosterCensusProtocol`, using the cloned installed runtime Store, before the existing initial census assessment/publication boundary. Require exact schema, section IDs, owner identity, `Required` role, and cardinality/revision agreement. The selected bootstrapped profile has an empty starting Store; the sections are not `ExplicitlyEmpty` because the three existing runtime APIs support later state. A missing provider, wrong owner, wrong schema, or mismatched baseline fails closed before the runtime is published.

This is instrumentation of an owner already composed by the selected runtime. It does not add a new owner or change composition, so the new-owner admission exception is not being used. If a future composition supplies populated claims at genesis or adds another political owner, it must be reviewed against the profile contract and the current P12 admission evidence; this design does not silently admit unsupported content.

The shared hotspot is `SimulationRuntime.cs`; serialize implementation with other changes to that file and to the sealed P12 section inventory. Store work is limited to the allocation-free recognition count accessor; bootstrap changes are limited to Daily-v1's exact inventory expectation and owner assertion. Do not edit scenes or general-purpose config assets.

## Validation plan

Focused tests must establish:

- each provider's exact stable section ID, schema, `Required` role, owner reference, independent count, common revision, and live refresh;
- the selected Daily-v1 exact inventory grows from 255 to 257, both new sections bind the `SimulationRuntime`'s installed clone, and the normal bootstrapped counts are zero;
- each of the three successful facade commits updates existing domain results, `PoliticalWorldRevision`, both section revisions, and exactly one P12 epoch;
- new recognition and replacement recognition distinguish count changes from revision-only changes; claim resolution updates revisions while preserving both counts;
- invalid/duplicate claims, stale recognition/resolution transitions, local revision overflow, stale baselines, off-owner-thread calls, and exhausted epoch capacity do not mutate claim truth or advance the P12 epoch;
- proposal/read paths and non-P12 runtimes retain existing behavior.

Then run focused PoliticalClaim/P12 census suites, bootstrap-composition and runtime-admission regressions, ALL EditMode, official Smoke, and `git diff --check`. Retain exact test outputs and hashes against the executable source tree before submitting an implementation candidate. Independent code review must inspect the full diff against the refreshed canonical base and confirm no extra runtime producer or scope was added.

## Reconstruction-sensitive facts and exclusions

Claim identity and chronology are stable `PoliticalClaimId`, claimant `PersonId`, target kind/ID, claim type/status, creation/resolution absolute days, and immutable recognition identity `(ClaimId, InstitutionId)` with its history/state and logical day. The existing domain records remain their owners. This P12-B slice adds only count/revision census and invalidation; it does not export or hydrate these records, create claims, decide recognition, establish political authority, or implement a new gameplay flow.

PoliticalSupportStore is a separate next owner and must not be folded into this operation. Political Knowledge and PoliticalDecision state are excluded from this design. The promotion would not prove complete P12-E owner coverage, complete P12-B census or shared-epoch coverage, owner-thread/quiescence for the whole runtime, capture eligibility, export/hydration, P12-A/P13 readiness, P12-B completion, or Phase 12 closure. P12-B stays `INCOMPLETE`; P12-A stays `WAIT_DEPENDENCY`; P13 stays blocked.