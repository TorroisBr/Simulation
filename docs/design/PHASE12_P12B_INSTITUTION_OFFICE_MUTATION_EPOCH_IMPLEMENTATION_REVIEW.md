# P12-B Institution/Office Mutation Epoch — Independent Implementation Review

**Disposition:** `VALIDATED_CANDIDATE` — exact candidate tip reviewed. Canonical promotion remains separate.

## Exact review target

- P12 canonical and candidate base: `codex/phase12/canonical` at `0daa72addc1f186d23713adf75f6c2f83a5aff9b`.
- Candidate branch: `codex/phase12/P12BInstitutionOfficeEpochImplementation` at `d9aee58065e5bab710bc973617221d45b036e16b`.
- Candidate full-tree SHA: `0e8030f11a18acc424f19437d2bb7ad43bf1529d`.
- Final executable code commit/tree: `13a4ff503d336d34ed008f27571cce82c019dfe4` / `85ad013074b727ffe8727c2d90b079a45e0ca5c0`.
- Current remote canonical was verified as `0daa72addc1f186d23713adf75f6c2f83a5aff9b` immediately before review.
- Technical design review: PASS at `235d15e5c78a198de5f79f53fe36346dc942d95b`.
- Current authoritative architecture baseline inspected: `e16796014d348e3b59da7ed848101c4c03926ba5`; accepted P12 Brief and State were checked at the stated canonical base.

## Review findings

The full base-to-candidate diff matches the reviewed bounded Institution/Office contract. Under the selected `UnityBootstrap-Daily-v1` admission context, it registers exactly the four existing P12-E census providers against the installed `InstitutionStore` and child `OfficeStore`. Admission requires all four witnesses at zero cardinality, exact IDs/schema/owner identity, and valid revisions. The three Office sections correctly share their owner and revision. The selected protocol count is 239, not a claim of complete owner coverage. The P10-A proving profile remains separate.

The sealed operation ID is exactly `p12.institution-office.owner-commit`, matching the reviewed design. Before each covered mutation, runtime admission checks the bound owner thread, validates the affected section baselines and shared mutation-epoch capacity, then enters the registered operation. Successful Institution commits notify the Institution section once. Every successful Office commit notifies records, incumbencies, and tenures together in one epoch because all three witnesses share one revision; this includes same-cardinality vacancy/tenure closure. Domain failures do not notify. Post-commit notification failure faults P12 admission but preserves the already committed API success result.

All supported `SimulationRuntime` commit paths are covered: Institution registration; Office registration; both `TryAssignIncumbent` overloads; explicit vacancy; and institutional vacancy recognition. The convenience assignment overload delegates to the explicit-day implementation. Stale vacancy proposals remain rejected by the existing exact-incumbency check. Runtime clone replay remains before initial census baselining. Non-P12 paths retain their previous mutation behavior; no daily-loop or unrelated political mutation path was added.

The corrected tests now assert the correct sealed operation ID and exclude the former `runtime.*` ID, verify all 239 sections and owner identities, exercise each success and rejection path, prove same-cardinality vacancy invalidation, and assert that the registered operation tracker returns to zero after the operation sequence. The off-owner-thread rejection verifies no owner/revision/epoch write and zero active scopes. Existing non-P12 census/domain tests remain in the suite.

## Exact-tree validation evidence

Validation applies to code commit `13a4ff503d336d34ed008f27571cce82c019dfe4` and tree `85ad013074b727ffe8727c2d90b079a45e0ca5c0`; the later candidate commits only retain evidence/docs. I independently checked the corrected artifact SHA-256 values against the committed XML and raw-log ZIP files, and verified the XML results:

- Focused `InstitutionOfficeCensusTests`: 4/4 PASS; XML `E2FEFAEB96BB4C5C91B6314974C0DC66EFB7BDD53F631ED20C248324EC9FD8BB`; log `053EDF76B1F3C7FEA1B1FE39A87D6EC66AE1C6977AD113B168D4A1B2EB3A31FF`.
- ALL EditMode: 2410/2410 PASS; XML `A08CEC174C18A9931D6A3D22BACFBFD97566AD1DDA023426D41AA2C467CA5CEC`; log `DA751444F5B86FC9AF6E23B6C9BEE6065A76FE3A62F29D888C8E0508D26CBF85`.
- Official Smoke: 5/5 PASS; XML `58019511F077BBD51FF8F951C03951A8C49C9B20E953F025194409CEDAD8CD59`; log `0B08E550F6E980F90E11141C897DC6D61A39E49F5E518EF59C8F2517B49EDF87`.
- Raw-log archive SHA-256: `3D50D0F802C06FB7F8058B77E9216902BE33FB1CBE4A502AB3B481C6895B5ECC`.
- `git diff --check`: PASS on the final code tip.

The retained validation record includes source-file hashes. For the mixed-line-ending test files, I confirmed the hashes after the documented CRLF checkout normalization as well as their exact Git content. Unity tests were not rerun during this review.

## Scope and integration limits

This candidate covers only the bounded Institution/Office P12-B mutation-invalidation slice. It does not establish complete owner or shared-epoch coverage, global quiescence, capture eligibility, export/hydration, P12-B completion, P12-A or P13 readiness, or Phase 12 closure. No promotion was performed. The normal refreshed canonical-promotion preflight remains required.
