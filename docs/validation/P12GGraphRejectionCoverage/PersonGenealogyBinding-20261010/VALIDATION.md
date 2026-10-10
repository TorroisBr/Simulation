# P12-G Person/NPC graph validation record

## Candidate identity

- Canonical base: `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Exact code commit: `e9a6ba6ca7c3b471b809dc103cae4eb765071e3c`.
- Candidate Git tree: `1d137fd088c8b954bfb819dc88a2abb5df7ff4e0`.
- Candidate `Assets` tree: `ccc147b8c7b73c33a460ccc5a381572c42d6f58f`.
- Changed code paths: `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs` and `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`.
- Unity Editor: `6000.3.9f1`.

## Bounded change

After the private restored candidate is composed, the coordinator validates the assembled Person, NPC, and Genealogy graph before taking the candidate owner vector. The check rejects duplicate/missing stable identities, one-way or aliased Person/NPC materialization links, retained private links on an unbound NPC, and Genealogy endpoints absent from the staged PersonStore.

`DailyV1RestoreRejectsBrokenPersonNpcBindingAtomically` corrupts the candidate's private Person reference after composition. It verifies `BindingValidationFailed` before publication, retention and health of the original session/token/owner projection, deterministic continuation against a control, and successful retry.

`RestoredDailyOwnerBaselineAcceptsPopulatedPoliticalKnowledgeAndDecisionOwners` creates valid non-empty Person/Genealogy, PoliticalKnowledge, and PoliticalDecision owners before composing a runtime with a restored-continuation context. It verifies exact owner-section cardinality/revisions and retained knowledge/decision facts in the unadmitted restored baseline. This is a baseline/admission witness; it does not claim source-session-to-target PoliticalKnowledge/Decision roundtrip parity or add a live gameplay write path.

The prior exact-code review of `fdc9ac26` returned `NEEDS_CHANGES` because its new restoration fixture left PoliticalKnowledge and PoliticalDecision empty. The current candidate adds the populated restored-baseline witness and the reciprocal Person/NPC rejection test. Independent review of `e9a6ba6` is pending.

## Validation

All results are passing Unity Test Framework XMLs with zero failed, skipped, or inconclusive tests. Raw logs and XMLs are included in [`UnityValidationLogs-20261010.zip`](UnityValidationLogs-20261010.zip), SHA-256 `AF8345AA80DBF846FD78C44A450C53F0ADBEC2AF58442EE7FEEA2A9B80C21EC3`, 4,177,428 bytes.

| Gate | Result | XML | XML SHA-256 | Raw log SHA-256 |
|---|---:|---|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 116/116 | [`Focused-116.xml`](Focused-116.xml) | `8995FEDE7A303E653C0F9BA4DDF548A5C84F473D83413B60D800237B22750751` | `F60740DBB0435C14D866A08C1B0BB7B26C51CAF8E4B7F05DE2AF9D4AC3E5FDC3` |
| ALL EditMode | 2790/2790 | [`EditMode-20261010-161431-8a3da9f2f3e847a28cc1a384a2fb3416.xml`](HarnessResults/EditMode-20261010-161431-8a3da9f2f3e847a28cc1a384a2fb3416.xml) | `6E076EF95B0193624E0129100667B02DCC1A79A1284D7FC362C4771C271C28FD` | `E63B96C78A139E0808209DB5681E55C8942DD32E31BDC04D53E32843BB7A4FB2` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`EditMode-20261010-161513-7348712786cd40dbbd953d4634cc42b9.xml`](HarnessResults/EditMode-20261010-161513-7348712786cd40dbbd953d4634cc42b9.xml) | `91C1EC0749FAF699DC8EB41D59D64712106D8084B1E05406A45B053D24E78473` | `4A49BCB79361B1C05CE4F7989D279A1122268896ACF3387F0E50AAF364B10C16` |
| `git diff --check` | PASS | Code commit `e9a6ba6` | — | — |

## Status and limits

This is a P12-G in-memory graph validation/evidence increment only. It does not complete P12-G, P12-B, or Phase 12; does not establish P12-A readiness, persistence-envelope parsing, export/hydration, capture eligibility, or P13 readiness. The broader P12-G compatibility/rejection matrix, systematic B-F/cross-section corruption coverage, hydrator/validator failure injection, lifecycle disposal cases, and exact P8/P9 lineage variants remain outstanding. Unrelated ProjectSettings edits and untracked `.meta` files remain excluded.
