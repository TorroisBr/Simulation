# CORE-F P12-D NPC Residence Authority Fix — Independent Review

**Verdict:** `VALIDATED_CANDIDATE` for the bounded NPC residence-authority fix only. This is not approval of the complete CORE-F application/host package and does not promote Core canonical.

## Exact identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Reviewed code commit: `6a72c2c1e12ff98e7272a70f3a404c27b057cf15`
- Reviewed code tree: `52f1246e073ca93046c4fbd2207846d04513fd4a`
- Candidate base: `ec27067a075c9e37a813feb9a9d99d4ff2b4820b`
- Candidate evidence tip at review: `42e50f4c98428e449a5c247d3e914b56f569ddc4`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Exact-tip independent review: `PASS`

The review covered code commit `6a72c2c`; the candidate evidence commit `42e50f4` adds only validation documentation, manifest and normalized XML archive. This review record is being committed separately after the candidate evidence tip. Its author is the Master Orchestrator, transcribing the verdict returned by the independent reviewer; the reviewer did not author or sign this document.

## Review scope and findings

The independent reviewer examined the complete bounded diff from the stated base, source/test identities, retained validation evidence and candidate ancestry. The prior review had identified two actionable defects; both are resolved in the reviewed tree:

1. Selected NPC residence idempotency and resident-death clearing now consult the selected Core residence authority through `ResidenceSettlementRuntimeId`, not the stale Unity compatibility backing field.
2. P12-D export now emits the residence revision from the bound Person authority when that Person owns residence state; an unbound NPC continues to use its NPC residence owner revision.

Focused regressions cover same-value residence assignment, selected immigration followed by resident death and associated revisions/epoch, and Person-backed NPC export where Person and compatibility-owner revisions differ. The independent review found no remaining actionable defects and no weakened assertions.

The reviewer could not write Git metadata or inspect status in its linked checkout. To avoid implying otherwise, the verdict was based on the immutable exact commit diff, remote branch tip, ancestry, file hashes and reviewable validation evidence. This durable record preserves that response; it is not represented as reviewer-authored.

## Validation evidence

The evidence is tied to code commit `6a72c2c1e12ff98e7272a70f3a404c27b057cf15` and tree `52f1246e073ca93046c4fbd2207846d04513fd4a`.

- Official CoreValidation: `891/891` cold and `891/891` warm.
- CoreValidation run: `79eeeb1fd55145348a30604b212a797f`.
- Core input digest: `61a58f9af55100635974786ddeef0d912def4786dd8dd1a37ab05c9c4c99bd45`.
- Unity: `6000.3.9f1`; stable exact-source input digest `d6696c3c7375746bbc0ca71937566ee90c4bbb6973d3d1c1cd3d247bdc15511a`.
- Focused `P12PopulationLifecycleInvalidationTests`: `22/22 PASS`, run `0789fea9f7d440049270470fa57f7407`.
- ALL EditMode: `3274/3274 PASS`, run `2c0decda4bb2472b92fc43ee4a637a05`.
- Official Smoke filter: `5/5 PASS`, run `72966559d1c449cba394f55a4dfc6e0e`.
- `git diff --check`: `PASS`.
- Normalized XML package verification against the protected original run: `PASS`; archive SHA-256 `6d1483c89c1c940e3ab37f57622f32f130f78dcfd493cc595d165c8a7e96d9c1`; public manifest SHA-256 `c23e1909cbe395c85e19ba52b9095ac4ea769a8b4eb0c43c8f1114edec63a7a7`.
- Public package privacy scan: `PASS`; original reports remain in owner-only retention.

The initial Unity import changed generated ProjectSettings and that run was excluded. The reported Unity runs followed the stable import and retained the exact code commit. A separate PlayMode probe selected zero tests and is not counted as a Smoke result.

## Integration constraints and limits

This candidate resolves only the listed P12-D residence authority defects. It does not change a P12 schema or profile, complete P12-D, complete CORE-F, or establish the shared application/host contract. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The independent review has no remaining test gap for this bounded fix. Full CORE-F validation, detached restoration/host integration, separate-process continuation and their applicable reviews remain outstanding. Any subsequent edits to the reviewed production or test sources require proportional revalidation and a fresh exact-tip review.
