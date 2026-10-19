# CORE-F selected travel owner / P12 boundary — independent exact-tip review

**Verdict: VALIDATED_CANDIDATE — selected travel slice only.** This is not a whole-commit review, canonical promotion, or CORE-F acceptance.

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`
- Production base: `3ec0aa126f95a034f219c4dca3accc82ac3c8dc9`
- Candidate: `62d27581fe1b3b9255dbe810be07b53bb3465551`
- Candidate tree: `34987c0efec955aeb100b64474ecbe7551a7f58f`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` on `codex/core/canonical`, queried from GitHub with `git ls-remote` on 2026-10-19.
- The candidate parent is the stated base. Current canonical is an ancestor of the base; no post-base canonical drift was found.

## Scope and result

I reviewed the exact base-to-candidate travel changes in `DailyV1SelectedRuntimeOwnerGraph`, `DailyV1SelectedTravelTransitionPlan`, `TravelSystem`, the `NpcRuntime` P12 adapter, the runtime call site, and their focused tests. This commit also contains selected City material-flow owner changes; those are outside this record's review scope and are not approved by this verdict.

No actionable travel finding remains.

The Core graph orders eligible owners by ordinal RuntimeId and uses the active-party membership snapshot plus each owner's active party ID to exclude party travel. For each solo owner it prepares one plan, runs host/P12 preflight, commits the Core travel and presence owners, invokes the Unity projection/P12 publisher, and then invokes that owner's Knowledge/event/log result callback before processing the next NPC.

The preflight resolves all changed City owners and the stored destination City to their exact Unity City projections. It checks reciprocal Core-ID/object membership for each candidate. The destination is validation-only unless the prepared plan changes that City's presence; only actual changed Cities are returned for projection and P12 revision accounting. This preserves validation on non-arrival steps without falsely counting a destination as mutated.

The Core plan owns travel and City-presence values/revisions. The Unity methods reconcile object-reference/field projections and do not write the Core owner or advance its revision. P12 invalidation follows the reciprocal projection and precedes the next owner's preflight. Arrival discovery and event/report behavior remain in stable per-owner order after that owner's P12 notification.

A rejected per-owner preflight leaves that owner's Core travel/presence state and revisions untouched; processing may continue for later eligible owners, so the daily set is not an all-or-nothing batch. A projection exception after a Core commit faults the runtime; no cross-owner rollback is claimed. This is a bounded per-owner integration slice, not a new rollback contract.

## Coverage and validation evidence

The shared-destination Unity regression selects two living NPCs from one source City and advances both to the same destination. It checks two arrivals, the exact two destination presence-revision increments, consistent projections, and a passing roster census. A second test deliberately creates an inconsistent destination object projection on a non-arrival step and verifies rejection without changing travel revision, destination presence revision, or the P12 mutation epoch. Core tests cover per-owner publish/result ordering, rejection without the rejected owner's commit, and later eligible-owner progress.

The committed validation record is `docs/validation/CORE_F_RuntimeBoundaryTravel/VALIDATION.md` (blob `4a947f60e9a926922376673a55db2d626a6079f5`). All nine listed source/test SHA-256 values match the committed candidate files.

That record reports Unity 6000.3.9f1 runs including the selected arrival test 1/1 (run `d3934becd8a74ea698951df93c9f9643`), SimulationRuntimeAdmission 208/208 (run `03e3c65b90064cf889968629e320c329`), BootstrapComposition 38/38 (run `2e06ff66fa0a448aa071f3a7acf4c8ba`), P12 solo travel start 11/11 (run `becf3b717a1c40059bd8e83515bb7e8c`), P12 TravelParty advance 10/10 (run `e75cf0f4cc1b4528bb3dc3f4ad472093`), runtime orchestration 12/12 (run `c1e0c1be02884c138f34e0ba1334e562`), generalized spatial travel 19/19 (run `875a00001cb04a109d2b9a5024e34485`), and GroupTravel 32/32 (run `77aa9d939d0b4663a230f2a4822daa14`). It reports CoreValidation 7/7 and `git diff --check` PASS.

The original Unity XML/log bytes reside in the stated private evidence store and were not available for independent re-opening in this review. Accordingly, I verified the committed source identities and the record's run IDs/results, but did not independently verify the private artifact bytes or reproduce the runs. The record itself correctly identifies these as incremental filtered results, not complete packaged evidence or full EditMode/Smoke validation.

## Limits

This review does not cover the co-committed material-flow change, the full CORE-F host/runtime package, full-suite validation, detached restore, .NET host continuation, or canonical promotion. CORE-F remains incomplete. No candidate files were edited during review.
