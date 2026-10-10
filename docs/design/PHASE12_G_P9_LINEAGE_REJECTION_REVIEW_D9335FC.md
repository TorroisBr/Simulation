# P12-G selected P9 malformed-fingerprint rejection — exact-tip independent review

**Verdict:** `VALIDATED_CANDIDATE` for this bounded malformed selected-P9 fingerprint rejection case. This does not complete P12-G.

- Canonical base: `18f9410a08b2d4731c6ee8a5f67332feb1d71787`.
- Candidate branch: `codex/phase12/P12GP9LineageRejectionEvidence`.
- Candidate evidence tip at review: `d8b146c0db1d4638d80073e1c40a0ac6ea530592`.
- Reviewed code commit: `d9335fc250da17bd2d225fd0aa04dc661d6b1a00`.
- Reviewed Git tree: `eb068b209ac71cfed9723949d43d448ca23204e4`.
- Reviewed `Assets` tree: `8d38e01570fc1a4e4fbcfab247762d0997622e9c`.
- Independent reviewer: `/root/p12g_p9_lineage_review`.

## Review findings

The candidate adds one test to the existing P12-G restore-admission suite and no production-code changes. The test replaces the selected P9-B profile fingerprint with the malformed literal `invalid-selected-p9-fingerprint`. The coordinator rejects it during P9 manifest snapshot validation after source capture is recognized and before candidate roots are staged. The test checks that the active source session, completed-boundary token, source owner graph, and source health remain unchanged. It advances the original source/control path for deterministic continuation parity, restores the original fingerprint, and verifies a later valid restore succeeds at the next completed boundary.

This proves end-to-end rejection of a malformed selected-P9 fingerprint before root staging. It does not prove rejection of a well-formed but inconsistent fingerprint or every P9 lineage mismatch. The exact-tip review confirmed the diff contains only that test and its retained validation manifest/XML/compressed logs. The reviewed code commit and `Assets` tree are unchanged at the evidence tip. No code defect or scope expansion was found. The test uses the existing test-only private-field access pattern and does not modify production behavior, add an owner, or alter the selected profile.

## Exact validation evidence

The exact reviewed `Assets` tree passed focused `SimulationRuntimeAdmissionTests` 117/117, ALL EditMode 2791/2791, official Smoke 5/5, and `git diff --check`. All XMLs report zero failures, skips, or inconclusive tests. The reviewer verified the retained results and hashes recorded in [`../validation/P12GGraphRejectionCoverage/P9Lineage-20261010/VALIDATION.md`](../validation/P12GGraphRejectionCoverage/P9Lineage-20261010/VALIDATION.md):

- Focused XML SHA-256: `D4BB9F4027AC0C15937D181F2308511F0A4BD9ABFFE87EFBBD54C85BE4E74E8C`; compressed log: `9500157BE014152C5FB77DEDDB531079BF914B2C23BCC7188A8B1A2BC3E4E178`.
- ALL EditMode XML SHA-256: `EFD1C8B55C89BA772A19F5F61984903215B6553100DEE904E27996912B97EFB2`; compressed log: `35F09F815B1607A1F18D519A1F29A821551225F47B2BDF58E1835CA8AFD3E0FF`.
- Official Smoke XML SHA-256: `2BB2E3E8D63FD3ADC00C6A45F70ACB76E6D2D31A9F10FE6AEDD1A3364FC8F090`; compressed log: `BFB244556C9E6CC4EFEED98ABB7EB22098B61B982F896590322E0325FD44F836`.

No tests were rerun during independent review. The exact candidate diff does not include the protected ProjectSettings edits, unrelated untracked `.meta` files, or prior raw XML files.

## Remaining scope and limits

The selected 299-section live inventory, covered dynamic transitions, and supported writer/operation/epoch family audit were already promoted and are closed by the current State entry at the top of `PHASE12_STATE.md`; older historical entries saying otherwise are superseded. This candidate closes only the malformed selected-P9 fingerprint rejection row. The valid-format-but-inconsistent P9 fingerprint path remains distinct. Existing `DailyV1RestoreRejectsCorruptedRootOrOwnerVectorAtomically` cases already cover P8-A target Location cardinality, so that case should not be duplicated.

Remaining P12-G work includes exact current B–F package-interface verification; the same-attempt target-owner census for P8-C/D, the target Crime sentinel, global receipt caches and the complete staged owner vector; remaining graph/compatibility rejection cases; failure injection across parse/admission/root/owner/relation/commitment/global-validation/guard-bind/publication; causal no-replay evidence; and full included-owner multi-boundary continuation parity. The populated P12-E PoliticalDecision ↔ P12-F PoliticalKnowledge round trip is required by §6.2, but current selected Daily-v1 has no supported source-ingress fixture for populated E/F: the runtime write APIs bypass P12 owner-operation refresh, so post-bootstrap writes stale the census and cannot produce a valid source boundary. A manually seeded restored-context baseline is not a normal source for a second G restore. Treat this as a capability/design blocker; do not add E/F writer scope or use P12-A serialization to force it into this checkpoint.

P12-G remains `WAIT_DEPENDENCY`; P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`. No capture eligibility, export/hydration readiness, or Phase completion is implied.