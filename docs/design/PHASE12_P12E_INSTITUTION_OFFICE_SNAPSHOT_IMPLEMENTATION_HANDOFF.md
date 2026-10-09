# P12-E Institution/Office Snapshot Implementation Handoff

**Status:** implementation candidate; not reviewed, integrated, canonical, or a Phase closure.

## Authority and exact basis

- Candidate branch: `codex/phase12/P12EInstitutionOfficeOwnerSnapshotImplementation`.
- Implementation base: design review commit `878372724a7979352f9430e83afe89a7091d7028`.
- Reviewed design: `codex/phase12/P12EInstitutionOfficeSnapshotDesign` at `52a1e5942e6c5fd57040566f71fe0e4fda508373`.
- Independent design PASS: `codex/review/phase12/P12EInstitutionOfficeOwnerSnapshotDesignReview52A1E59` at `878372724a7979352f9430e83afe89a7091d7028`.
- P12 canonical base recorded by that review: `f5d99cb7008023d14a0ed16ea2149a7d7c18def1`.
- Architecture authority checked by the design reviewer: current `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.

## Implemented boundary

This candidate adds detached schema-v1 values for exactly the four already-required Institution/Office census sections; capture consumes the existing Daily-v1 completed-boundary token and its exact owner-section vector. It stages one unpublished InstitutionStore and then one OfficeStore bound to that exact InstitutionStore, restoring the captured owner revisions directly through internal factories. It preserves active incumbency independently from ordered tenure history, including repeated equal closed tenure occurrences. It does not run succession, vacancy recognition, death, daily systems, runtime clone replay, or public gameplay mutations while staging.

The capability remains a private P12-E owner adapter. It does not add owners or sections, capture admission, runtime wiring, global graph publication, P12-G validation, shared epoch/quiescence coverage, a persistence envelope, or readiness/closure claims for P12-B, P12-A, P12-G, P13, or Phase 12.

## File and hotspot ownership

This implementation exclusively owns these candidate paths:

- `Assets/_Project/Scripts/Institution/InstitutionStores.cs` — only the internal exact Institution/Office factories were added.
- `Assets/_Project/Scripts/P12EInstitutionOfficeOwnerSnapshot.cs` and its `.meta` — detached adapter and value validation. It resides outside `Scripts/Institution` to preserve the existing Unity-free/domain-only layering contract enforced by `InstitutionFoundationTests`.
- `Assets/_Project/Tests/EditMode/Editor/Institution/P12EInstitutionOfficeOwnerSnapshotTests.cs` and its `.meta` — dedicated tests.

No other writer should edit `InstitutionStores.cs` until this candidate is integrated or explicitly handed off. The adapter and tests do not own `SimulationRuntime`, bootstrap/profile admission, census registration, P12-B token/epoch code, Person/Daily composition, or Property/Estate/Claim/Recognition/Conflict/War/Battle files. Future E graph composition should place this pair after the staged P12-D PersonStore and before Property/Claim consumers; that composition work belongs to its separately assigned integration owner.

## Validation evidence

The code tree validated here is `e1ecdcfc23e81842471a377582c172dd353941c4` (`Assets` tree). Successful runs on that tree:

| Gate | Result | Result artifact SHA-256 | Log artifact SHA-256 |
|---|---:|---|---|
| Focused `P12EInstitutionOfficeOwnerSnapshotTests` | 8/8 | `2C2966D63940F8F3318EF5184A655690C9CC856E71894694D60D0D973462D7EF` | `BE6F5556EDB0B94A5D9B01E72CC1864872E2BF59BCB2080227310456856CC77C` |
| Affected Institution, succession, Person/identity, Property/Claim regressions plus focused owner tests | 86/86 | `7D0AADB1DE9654155E7DA91B83D54E8989682EB1866754538A8192DDF0B35D58` | `4685C4121F21A0C84B06537498AC78CE751A0D92E3CF2842DB77E25BC9784BCA` |
| ALL EditMode | 2636/2636 | `7B37F49F3449346A2DB806CE5340FC6E9B11CA2FCCD5C953BFCCBE224457B466` | `7224573E8182D956B6C6B0E7FBB7910C49A46A12860EF1E7D6C4F83961C66E42` |
| Official EditMode Smoke (`-TestFilter Smoke`) | 5/5 | `D2B019E4CE3938CA1FEF5DABB7B43BD6198E5AF1AA2D9074FB2F4FD6377E758C` | `276B1F3BF67024639B5EB471E85B426F36AB706F249C153801415536AC4669A1` |
| `git diff --cached --check` | PASS | n/a | n/a |

XML result artifacts are under `docs/validation/P12EInstitutionOfficeOwnerSnapshot/{focused,regression,all-editmode,smoke}/`. Raw Unity logs remain in the local ignored output directories and are identified by their SHA-256 above; they are not committed because Unity emits trailing whitespace in machine-generated log lines, which conflicts with the required `git diff --check` gate. The regression selector included `InstitutionOfficeCensusTests`, `InstitutionFoundationTests`, `InstitutionalVacancyRecognitionTests`, `PoliticalSuccessionIntegrationTests`, `PersonOwnerSnapshotTests`, `PersonStoreCensusTests`, `IdentitySequenceSnapshotTests`, `PropertyOwnershipCensusTests`, `PoliticalClaimFoundationTests`, `P12PoliticalClaimCensusTests`, and this focused fixture. The initial regression exposed the existing core-layering assertion; moving the adapter to `Scripts/` preserved that contract, and the final regression result above passed.

The result and log hashes are recorded against the tested Assets tree. Exact candidate commit/tree and the independent exact-tip code-review record must be appended by the candidate/review handoff after commit and review. This document does not assert an independent implementation review has passed.

## Expanded validation re-run

After the initial candidate commit, focused malformed-section and snapshot-value coverage was expanded and the implementation revalidated on the resulting complete candidate working tree. The expanded tests cover multiple Institutions and Offices with order/value preservation, stale-token rejection after owner mutation, assignment versus vacancy captures (including cardinality and revision behavior), a missing token-section vector, missing/multiple open-tenure cases, invalid dates, unsupported schema, negative section revisions, and cardinality mismatch staging failures. These checks preserve repeated equal closed tenure occurrences and their multiplicity/order; clone-time deduplication validation is not reused.

| Gate | Result | Result artifact | Result SHA-256 | Log artifact | Log SHA-256 |
|---|---:|---|---|---|---|
| Focused `P12EInstitutionOfficeOwnerSnapshotTests` | 11/11 | `focused-revalidation/EditMode-20261009-000559-47b083d14a5d445bb53846fcab461ad0.xml` | `84986796F9D2D141B8DD88F50805E2F36A6C57CCB912928E8959847E8096827E` | same basename `.log` (local ignored output) | `C19C7F2CE4327D3477E93C9EB3C7B80DFEE5A864467576B6EDC8D3E43E3FFE62` |
| Selected Institution/Office, succession, Person/identity, Property/Claim regressions plus focused tests | 89/89 | `regression-revalidation/EditMode-20261009-000618-f1e8b56489024d27ac73aa208e011d6e.xml` | `B66D3F8F1E8D8D9C22AFBD0C360653C60468408549C29E88ED54C56800BC5A9C` | same basename `.log` (local ignored output) | `8C44293CBC21856B7097BDB1925B234C5F15DDC93ACB19D39CBAA4914140925D` |
| ALL EditMode | 2639/2639 | `all-editmode-revalidation/EditMode-20261009-000638-c5746e7f50784f93b4af937ff833bf5b.xml` | `0BB681BC3054BF3455E7289057A5FDD731AF14499C42BDE2354780A726E7493B` | same basename `.log` (local ignored output) | `1A835CFF129DD586B080D819EFC760A78AA3815F177AAD7EA58EA434C6DFB04B` |
| Official EditMode Smoke (`-TestFilter Smoke`) | 5/5 | `smoke-revalidation/EditMode-20261009-000736-4883585114474f7cb7125ecf56753fcc.xml` | `6232E284DA4F125993812F28BDE2F3B1A91A9FE5856430CAE8F8D59C5EEF2081` | same basename `.log` (local ignored output) | `728B53FC61B825768E031C7A239EB3F5A865BEE800389050D94F94C5F62191D3` |
| `git diff --check` | PASS | checked after staging the owned product/test/evidence paths | — | — | — |

The newly passing XML artifacts are committed with the candidate. Unity's raw `.log` outputs remain local ignored artifacts because generated log formatting includes trailing whitespace; their hashes above identify the exact local run outputs without introducing them into the whitespace gate. Unrelated ProjectSettings edits, pre-existing untracked `.meta` files, and failed-run artifacts were left untouched. Exact final code/tree and commit IDs are established by the additive candidate commit; this remains implementation evidence only and is not canonical promotion or independent implementation review.
