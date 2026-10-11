# P12-G FactionAffiliation-to-Person Rejection - Independent Review

**Verdict: VALIDATED_CANDIDATE (PASS).**

- Checkpoint: P12-G staged restore and whole-graph validation evidence only.
- Canonical base reviewed: `794ee2b11c412c67a9d86c6d560bccf3ed101237`.
- Candidate evidence tip reviewed: `5b4b6d68afaddb207068660377846ebb82f853ce`.
- Code commit: `352446f9e350d5dd103ade4010921072b429edbb`.
- Code Git tree: `a9b083554601e0958c02e5ba32d4fa5a988dc217`.
- Validated Assets tree: `787f81eafdb25bbd0c33afd194cac793ce825b2b`.
- Independent reviewer: `/root/p12g_inventory_reconciliation`.

## Review

The candidate adds a null-default internal test override that supplies a detached Faction owner snapshot through the existing restore-test path. Production callers pass null, and the existing P12-E Faction validator and staging path are unchanged.

The integrated case captures a valid populated Faction and affiliation, then changes only the affiliation's well-formed `PersonId` to an identity absent from the staged Person section. It preserves the section schema, cardinality, shared local revision, affiliation/Faction identities, and all other row values. Earlier E owners stage; the normal validator rejects before Faction owner staging and publication.

The test checks preservation of the active session/token, health, facts, roots, and owner graph; continuation parity against a control; successful retry of the untouched snapshot; restored Faction and affiliation identity; and parity after the next boundary. The diff is limited to `P12EDailyV1OwnerPackage.cs`, `P12GDailyV1RestoreCoordinator.cs`, `TesteSimulacao.cs`, and `SimulationRuntimeAdmissionTests.cs`. No ProjectSettings or unrelated `.meta` paths are included.

## Validation

Retained exact-tree validation reports zero failures, skips, and inconclusive tests:

- focused case: 1/1;
- `SimulationRuntimeAdmissionTests`: 184/184;
- `P12EFactionOwnerSnapshotTests`: 12/12;
- ALL EditMode: 2858/2858;
- official Smoke: 5/5;
- `git diff --check`: PASS.

The manifest and XML/log evidence are retained in `docs/validation/P12GFactionAffiliationPerson-20261010/VALIDATION.md`. The independent reviewer verified the exact branch/code/tree identities and changed paths, read the test and validation evidence, and recomputed the focused, admission, Faction-snapshot, and Smoke XML hashes plus the focused, Faction-snapshot, and Smoke compressed-log hashes. The reviewer could not recompute the larger ALL EditMode XML or larger compressed/raw logs through the review connector. The coordinating checkout separately recomputed all five XML, compressed-log, and decompressed raw-log SHA-256 values from the retained files; all matched the manifest, and parsed totals matched the reported results.

The review is exact-tip and does not claim complete owner or epoch coverage, runtime-wide quiescence, capture eligibility, export/hydration readiness, P12-A or P13 readiness, or Phase 12 closure. The candidate changes no accepted semantics or profile scope.

## Integration note

Safe additive fast-forward from the stated canonical base. The next distinct bounded graph-evidence case identified from the current P12-G design is a `PoliticalClaimTargetKind.Person` claim whose well-formed target is absent from the staged PersonStore. Existing Office, Institution-recognition, and Property absent-target cases do not cover that branch; the existing PoliticalClaim snapshot override is sufficient. This is test-only evidence and does not establish P12-G readiness.