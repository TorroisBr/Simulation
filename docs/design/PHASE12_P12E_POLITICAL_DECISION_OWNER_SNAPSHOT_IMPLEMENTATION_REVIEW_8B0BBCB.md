# P12-E PoliticalDecision owner snapshot implementation review

**Verdict:** `VALIDATED_CANDIDATE`, with an explicit integration limit.
**Independent reviewer:** `p12e_support_design_review_luna` (Luna).
**P12 canonical base:** `c9d2d8f9ff7176d4d36c5e0a007d2f9ddc7210f8`.
**Architecture baseline:** `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
**Design candidate:** `411feadfb0fc57fb030706a1fd9b5311b33879bb`.
**Design review record:** `d617aab13db2af4db0385e7c4d5b7eaa0375fe09`.
**Implementation code commit/tree:** `8b0bbcb15e864fa0ebb4d26dbc6b205038a2f921` / `4496b67ed9f59d9cb95bc4e16bf0b90bd3ae7e2b`.
**Reviewed candidate tip:** `293ad422153a6ef5b11c7e755757107d6cfba5ed`.
**Candidate full tree:** `63cc8bb15bd3a2c8bf12f49647d4456150f43d38`.
**Candidate Assets tree:** `04b8f0248dfcd4f164d7ba317ac34a159983a134`.
**Branch:** `codex/phase12/P12EPoliticalDecisionOwnerSnapshotImplementation`.

## Review coverage and findings

The reviewer inspected the full implementation diff against P12 canonical, the accepted owner-specific design, the P12-E Brief/State, and the current runtime owner-admission path. The review checked exact token and owner-vector identity, Required schema-v1 section identity, owner cardinality and append-only revision, deterministic day/ordinal-ID ordering, typed decision and outcome shape, canonical reference lists, captured-day bounds, D/E staged-root references, exact staged `PersonStore` binding, and preservation of unresolved P12-F/P12-G values.

The implementation adds a detached PoliticalDecision owner snapshot, a private staged-store factory, focused tests, and the necessary `PoliticalDecisionRecord` constructor assignment for its already-accepted `DecisionKind` argument. It does not change `SimulationRuntime`, bootstrap/profile composition, P12-B mutation/census wiring, P12-C sequencing, or P12-G publication.

The independent verdict is `VALIDATED_CANDIDATE` for the exact code tree above. The candidate is suitable for canonical integration only as an isolated P12-E owner prerequisite. The review does not establish populated live capture under the current Daily-v1 contract or complete PoliticalDecision continuation coverage.

## Exact-zero integration limit

The reviewer confirmed that canonical `TryRegisterP12GateOneFixedOwnerSections` registers PoliticalDecision as Required with exact initial cardinality and revision zero. A runtime therefore rejects preloaded non-empty PoliticalDecision history under the current Daily-v1 profile. Direct post-boundary mutation invalidates the completed token; the existing registration path does not provide the P12-B operation/census invalidation needed to make it a supported completed boundary. The candidate records this limit in its validation manifest and does not broaden P12-B.

The passing evidence proves exact empty-owner live capture, detached non-empty row staging, and rejection behavior. It does not prove live non-empty capture under current Daily-v1. A future P12-B/runtime integration must separately authorize and establish that capability before claiming it.

## Validation reviewed

The exact code tree has retained passing evidence:

- focused owner snapshot: 5/5;
- affected regressions: 28/28 across PoliticalDecision foundation, succession, and P12 genealogy/census;
- ALL EditMode: 2694/2694;
- official Smoke: 5/5;
- `git diff --check`: PASS.

The main orchestrator rechecked every artifact hash in `docs/validation/P12EPoliticalDecisionOwnerSnapshot/SHA256SUMS.txt`. Run identities, NUnit totals, and XML/log hashes are in the adjacent `runs.csv`; full validation and limits are in `VALIDATION.md`.

The docs-only commit from `c798d8f` to reviewed tip `293ad42` changes only the validation boundary paragraph. The reviewer independently confirmed that this change accurately states the canonical P12-B exact-zero admission rule and leaves the implementation and Assets tree unchanged.

## Scope boundary

P12-E remains in progress. This review does not claim full P12-E owner coverage, profile-wide export/hydration, global quiescence, capture eligibility, P12-A readiness, P13 readiness, or Phase 12 closure. PoliticalDecision live non-empty capture remains outside this candidate until the separately owned P12-B boundary supports it.
