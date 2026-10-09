# P12-E PoliticalClaimStore Owner Snapshot Design Review — Current Base

**Outcome: `PASS` — no blocking findings.** This is a fresh exact-tip review of the current-base PoliticalClaim/recognition owner contract, including its `BASE_DRIFT_ONLY` revalidation.

## Exact content reviewed

- Candidate: `codex/phase12/P12EPoliticalClaimOwnerSnapshotDesignCurrentBaseA768` at `94ce311d6c035bd89d7bf7754cab3cac3e373e84`.
- Current P12 canonical and candidate common ancestor: `a768f2d9eca161f5cff059a782737412f43b2861`. The candidate is a descendant of that exact base through its docs-only replay commit `465d0b2957b2d2dc621a4c0b3d79cccf971f5b89`.
- Candidate tree: `fe9b8a6370229f68775a80007a6678d56eb46401`; candidate and base `Assets` trees both equal `4ee9e00f3aaec28225fdebd2a38d8db78b2061e6`.
- Architecture authority: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`; refreshed remote architecture ref still resolves to that SHA.
- Current P12 canonical and candidate remote refs were checked immediately before review. The candidate diff from current P12 canonical adds only the PoliticalClaim owner design document; `git diff --check` passes.
- The original exact-content review `da4c02d` reviewed prior design commit `c286bc7` against old base `0709eec`. It is retained as historical evidence for that exact older candidate only. This review independently rechecks the current-base candidate `94ce311`; the old review is not used as current-base or implementation review.

Reviewed the full current design and its revalidation, the accepted P12-E umbrella contract, current P12 Brief/State, the exact `0709eec..a768f2d` Property/Estate promotion, current PoliticalClaim owner/census/runtime sources, and the promoted Property/Estate snapshot and implementation handoff.

## Current-base revalidation

The `0709eec..a768f2d` canonical delta adds the bounded Property/Estate snapshot implementation, its tests and validation evidence, the independent review record, and the P12 State promotion update. It does not modify `PoliticalClaimStore`, `PoliticalClaimStoreCensusProviders`, `PoliticalClaimContracts`, `PoliticalClaimTransitions`, or `SimulationRuntime`; the supported claim writers, revision behavior, and two P12-B census sections are unchanged.

The promoted `PropertyEstateOwnerSnapshot` stages the exact `PropertyOwnershipStore` authority with `PropertyId`-keyed ownership rows and typed Person references. The staged store retains its existing `TryGet(PropertyId, ...)` lookup, which is also the current runtime’s authority for validating a Property claim target. The refreshed design now requires claim staging to resolve Property targets against that exact staged owner and reject absent/mismatched roots. This is compatible with the accepted P12-E Property/Estate dependency and does not synthesize or defer target truth.

The candidate classifies the drift as `BASE_DRIFT_ONLY`. Source inspection confirms that classification: the base delta touches no claim owner, census, or live writer path, and the proposal adds no changed claim semantics. P12-E remains open; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

## Findings

No blocking findings.

- **Stable census contracts:** Both required schema-v1 sections retain their exact existing IDs, same-owner identity, separate claim/recognition cardinalities, and shared local revision. Empty sections remain explicit. No census ID, P12-B operation, or mutation-epoch contract changes.
- **Complete owner values:** The design retains every claim field and typed target kind/ID, canonical evidence values, and every recognition field with complete ordered history. It preserves pair identity through the existing length-prefixed `BuildRecognitionId`, including the at-most-one row per `(ClaimId, InstitutionId)` cardinality. It correctly rejects malformed history before any constructor behavior could append/repair the terminal entry. Recognition and claim evidence do not change target truth.
- **Writer/revision boundary:** The current source still shows only the three specified successful runtime mutation facades. Proposal methods are read/prepare; bootstrap clone registration operates on a private clone. Other current claim-store references are census registration, lookup, or mutation-guard binding. Successful writes advance one shared revision once; updates/resolution preserve row counts; failed, stale, or overflow paths do not commit. No new live writer appeared in the Property/Estate base delta. The design requires repeating this audit on the implementation base.
- **Export/staging correctness:** Deterministic claim and recognition ordering matches the store getters; canonical evidence order and recognition history order remain intact. Capture is bound to the existing P12-B completed boundary and validates same-owner section stamps plus before/after revision. Private staging validates claimant, target, recognition Institution, exact pair identity, timeline/status/history, cardinality, schema, owner, and revision before returning a candidate. It restores exact local revision without replaying operations and rejects failures atomically.
- **Dependency/exclusion boundary:** Person and Institution/Office roots remain required, and Property targets now resolve against the promoted exact Property ownership root. PoliticalDecision, Faction, Support, Knowledge, P12-B changes, P12-G publication, P12-A readiness, P13 history, new gameplay, and Phase closure remain excluded. The design does not claim P12-E-wide coverage or capture eligibility.
- **Implementation evidence obligations:** The focused suite matrix covers empty and populated state, all target kinds, pair cardinality/replacement/history, exact revision, deterministic order, missing/dangling/wrong-kind roots, malformed schema/IDs/dates/history, no-partial staging, source immutability, overflow, and unchanged P12-B semantics. It also requires the affected owner regressions, ALL EditMode, official Smoke, `git diff --check`, exact-tree artifacts, and independent exact-tip code review.

## Readiness boundary

This PASS validates only the owner-specific design at `94ce311` on current P12 base `a768f2d`. It does not authorize implementation or promotion and does not establish P12-E completion, P12-A readiness, P13 readiness, profile-wide export/hydration, or Phase 12 closure. Implementation still needs its separate safe file-ownership handoff, exact-base writer re-audit, required validation, and independent implementation review.
