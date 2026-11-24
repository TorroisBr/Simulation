# CORE-F Selected Receipt Census Integration

Date: 2026-11-24
Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
Pre-change HEAD: `3c3ffa56d47805cd72c57e2ac7d54201f9c075fa`
Core canonical is unchanged: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Scope and result

This checkpoint composes the existing Core Crime and Justice receipt authorities into the selected Daily-v1 census and adds one Core-owned LocalObservation receipt owner per selected NPC. It does not create parallel ledgers or substitute placeholder census providers. The provider registrations witness the exact owners retained by the Core graph, including their stable identities, cardinality, and local revisions.

The prior exact missing-row set was 12: one Crime P18 row, one Justice P18 row, and ten selected-NPC LocalObservation rows. The focused production-composition test now finds no missing rows: all ten distinct LocalObservation registrations bind to each NPC's exact `LocalObservationReceipts` owner, and the complete provider section-ID set equals the test's full selected-profile vector (all 89 census families). The Core registry seals successfully. The receipt owners remain exact-empty for P12; no populated receipt state, persistence schema change, or populated-receipt restoration guarantee is claimed.

The test `DetachedCoreStartSealsCompleteReceiptCensusThenFailsClosedOnMissingOperationInventory` exercises the next lifecycle boundary. Its host deliberately has no sealed runtime-operation inventory. The complete census seals, but `TryStartDotNetOnExistingApplication` returns false and leaves `ActiveSession` unpublished. No daily operation ran. This checkpoint therefore proves full census registration and sealing, not active-session startup, runtime-operation readiness, or a daily advance.

## Validation

### CoreValidation

Official run `e9fab86cc46c4323965dcbbb266d6a34` passed cold and warm: 1,207/1,207 each, with zero failures, errors, timeouts, or skipped tests. The stable 560-file input inventory has digest `5e1f540967bd4d31a0a3c3adcd7968ae6c5294c5469b836bc0c0ff3f2bc17018`. All 11 changed Core package/test inputs match that run's preserved input manifest. The run recorded source HEAD `3c3ffa56d47805cd72c57e2ac7d54201f9c075fa` and tree `bb3ea5c7075a01e34a29302fde378120e77552c9` plus the stable working-input digest.

Original cold and warm TRX remain in protected private evidence and were not edited or included in this candidate. Their SHA-256 values are `465c59f142c7c52e08297ea6549fc0ce9410c0fb3cb9182c1a8a26feab97065f` and `c6108962a6765f509d7683861af08a209a216960af3646e12e55af999b76476a`. Current evidence policy does not authorize normalizing or publishing these raw .NET TRX here; their existence and hashes are recorded without exposing their contents.

### Unity

Official Unity 6000.3.9f1 validation passed against a stable source snapshot. The 17 changed Unity source/assembly files match the successful full EditMode input snapshot exactly. Focused selected Daily-v1 EditMode passed 34/34 (run `7ea26bb34c5a43e2a633c06b8b3f1c2d`); restored terminal actor-choice continuation passed 2/2 (run `880fa3e7a816480c8218c30de909cfa9`); full EditMode passed 3,327/3,327 (run `d82248f50a8f4c7f86bfd1e15e50143a`); official Smoke passed 5/5 (run `a1db84f5c49948598d1233f5019b36b2`). Every focused and Smoke test identity/result is present as Passed in the full EditMode XML.

The approved Unity evidence package contains the complete unfiltered EditMode result only:

- Manifest: `Unity/AllEditMode/coref-selected-receipt-census-editmode-20261124.manifest.json`
- Archive: `Unity/AllEditMode/coref-selected-receipt-census-editmode-20261124.zip`
- Run: `d82248f50a8f4c7f86bfd1e15e50143a`; 3,327 passed, zero failed.
- Original XML SHA-256: `5c91c28e6022c70cf0ea36a9506609d455292bf2aaf0f8a146e5240c1ed99e0a`.
- Normalized XML SHA-256: `ba80294b7dd662bfc3e27855c05dde9c64ca4f791ece1a6baee61e9aba5784bf`.
- Archive SHA-256: `06343b3c412b42c250f378dff40c9bb30bb8a36c8bd69d6c5dd61fb2af7b1b89` (176,533 bytes).

The official packager verified the archive. Its manifest hash matches the archive, and a scan of the packaged XML found no local absolute paths. A packaging attempt for filtered XML was rejected by the existing packager because the filtered report retains a parent `Simulation` suite testcasecount from the full run. The original focused and Smoke reports were left unchanged and private; no XML was repaired or reinterpreted. Their test identities and Passed results were cross-checked against the successful full run.

`git diff --check` passed. E: had 14.12 GiB free after validation (the 8 GiB safety floor was maintained; the 15 GiB warning threshold had been crossed).

## Review and promotion status

Independent exact-tip code review is pending on the candidate commit. This record does not claim review approval or Core canonical promotion. No canonical ref is advanced by this checkpoint. The unrelated ProjectSettings edits and existing local validation note remain outside the candidate change set.
