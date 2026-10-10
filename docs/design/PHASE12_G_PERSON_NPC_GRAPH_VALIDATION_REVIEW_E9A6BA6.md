# P12-G Person/NPC graph validation — exact-tip independent review

**Verdict:** `VALIDATED_CANDIDATE` for this bounded in-memory graph-validation increment. This does not complete P12-G.

- Canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Candidate branch: `codex/phase12/P12GGraphRejectionCoverage`.
- Candidate evidence tip at review: `2fc4317868808f9dda21ea7ca63caf29ac83fa4a`.
- Reviewed code commit: `e9a6ba6ca7c3b471b809dc103cae4eb765071e3c`.
- Reviewed Git tree: `1d137fd088c8b954bfb819dc88a2abb5df7ff4e0`.
- Reviewed `Assets` tree: `ccc147b8c7b73c33a460ccc5a381572c42d6f58f`.
- Independent reviewer: `/root/p12g_failure_matrix`.

## Review findings

The final assembled-graph validator checks unique Person and NPC identities, exact reciprocal Person/NPC IDs, object references and indexes, rejects an unbound NPC retaining a private Person reference, and verifies Genealogy endpoints resolve in the staged Person store. It runs on the private candidate before target-owner census/admission and publication. The added corruption test confirms rejection preserves the active session/token, source owner graph and health; subsequent source/control continuation remains equivalent, and a valid retry succeeds.

The populated restored-owner baseline test now supplies nonempty Genealogy, PoliticalKnowledge and PoliticalDecision state and checks preserved facts, exact section cardinalities and revisions. This closes the prior fresh-baseline evidence finding. It does not demonstrate source-session-to-target PoliticalKnowledge/PoliticalDecision round-trip parity, and no such parity is claimed. The nonempty Genealogy relation is separately exercised through integrated restore and continuation parity.

No code defect or scope regression was found for this bounded increment. Exact candidate validation and artifact hashes were rechecked; all reported suites have zero failed, skipped or inconclusive tests. The reviewed code and Assets trees are unchanged at the candidate evidence tip.

## Exact validation evidence

- Focused `SimulationRuntimeAdmissionTests`: 116/116 PASS; XML SHA-256 `8995FEDE7A303E653C0F9BA4DDF548A5C84F473D83413B60D800237B22750751`; raw log SHA-256 `F60740DBB0435C14D866A08C1B0BB7B26C51CAF8E4B7F05DE2AF9D4AC3E5FDC3`.
- ALL EditMode: 2790/2790 PASS; XML SHA-256 `6E076EF95B0193624E0129100667B02DCC1A79A1284D7FC362C4771C271C28FD`; raw log SHA-256 `E63B96C78A139E0808209DB5681E55C8942DD32E31BDC04D53E32843BB7A4FB2`.
- Official Smoke: 5/5 PASS; XML SHA-256 `91C1EC0749FAF699DC8EB41D59D64712106D8084B1E05406A45B053D24E78473`; raw log SHA-256 `4A49BCB79361B1C05CE4F7989D279A1122268896ACF3387F0E50AAF364B10C16`.
- Archived validation logs: SHA-256 `AF8345AA80DBF846FD78C44A450C53F0ADBEC2AF58442EE7FEEA2A9B80C21EC3`.
- `git diff --check`: PASS at code commit `e9a6ba6`.

The checked artifacts are retained in `docs/validation/P12GGraphRejectionCoverage/PersonGenealogyBinding-20261010/VALIDATION.md` and its linked XML/log archive.

## Remaining P12-G obligations and limits

P12-G remains `WAIT_DEPENDENCY`. This increment does not complete the broader section/profile compatibility matrix, systematic B–F and cross-section corruption cases, owner-hydration/relation/commitment/global-validation failure injection, lifecycle disposal cases, exact P8/P9 lineage variants, or full multi-boundary parity across every included owner. The in-memory coordinator does not parse a serialized envelope; serialization remains at the P12-A boundary. No P12-A readiness, P13 readiness, capture eligibility, export/hydration capability, or Phase 12 closure is claimed. P12-B remains incomplete and Phase 12 remains open.

No Unity tests were run by the reviewer. Protected ProjectSettings changes and unrelated untracked `.meta` files were not touched.
