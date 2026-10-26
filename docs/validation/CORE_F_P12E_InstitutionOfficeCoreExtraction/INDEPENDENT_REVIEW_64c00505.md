# CORE-F P12-E Institution/Office Core Extraction — independent review

**Verdict:** VALIDATED_CANDIDATE. No actionable implementation findings.

## Reviewed identities

- Candidate branch: codex/core/CORE-F-shared-daily-v1-app-host.
- Full candidate: 64c005051bccea2cda6197ae03632ba106d8d1c5.
- Reviewed implementation commit: 69b0c9b333dcfde285098b455cadf01f223a301f.
- Implemented source tree: d1af2ac4a99b71cf8cc71b51d8bd9eebe42abec3.
- Candidate base: 25bdf58f1d24bd1f589194a31dd92ff65a3674ac.
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea.
- Candidate final commit adds validation documentation and the sanitized Unity package only; executable source and tests are unchanged from the validated implementation tree.

## Review result

The complete base-to-candidate diff is limited to the P12-E Institution/Office Core extraction, tests, and evidence. The census providers, owner codec, and snapshot are moved into the shared Core package. Their original Unity meta GUIDs are preserved. The new Unity capture adapter has a unique GUID and supplies the exact live runtime owners and the existing P12 capture freshness check.

The Core snapshot remains detached and internal. It consumes the same completed-boundary token and exact owner-section vector; checks required section identity, schema, role, cardinality, local revision, exact owner instance, and the Office-to-Institution object binding; copies only detached values; and revalidates the current token and owner data after capture. The existing runtime validator enforces current-token reference identity and owner-vector/epoch comparison. The staged factory preserves source revisions and returns private stores bound to the exact staged InstitutionStore.

Snapshot validation preserves stable IDs, Office-to-Institution and incumbent/tenure-to-Person relations, exact cardinality and revision rules, one active incumbency per Office, and one matching open tenure for each active incumbency. Closed tenure occurrences remain distinct and ordered. The codec retains its existing schema and is moved without source changes.

The Unity runtime continues to provide the same authoritative owner instances through a narrow host adapter; the snapshot and codec logic now have one Core source owner. No duplicate owner implementation, profile expansion, new public API, or change to P12 semantics was found.

Test edits replace direct calls to the moved snapshot method with the Unity adapter and retain the existing assertions. The three new Core tests cover codec round-trip/staging, missing Office parent rejection, and exact empty sections with shared revision. All three are present and passing in both retained CoreValidation reports.

## Validation and evidence

- CoreValidation: 845/845 cold and warm. The private manifest binds source commit 69b0c9b333dcfde285098b455cadf01f223a301f and tree d1af2ac4a99b71cf8cc71b51d8bd9eebe42abec3; source inventory is stable and captured TRX hashes match the protected originals. The three new Core tests pass in both reports. No public DotNetTrx package is claimed; the original reports remain private, as documented in VALIDATION.md.
- Unity EditMode: 3271/3271, run 7accabcde6a443d99cff57226a006364, exact implementation commit/tree, stable input digest 8c2ef810905f4ff6efbb2a311bc6ac4a2413797348eba4e2b04cc4ec214283a6. The sanitized package archive SHA-256 is 30bf1bce3e4f01ea9ebbd4aac4c943faf12280f04285b9fcd35e482dc20c52d5; manifest SHA-256 is 52853aecb3f4aa2c87fd21600235ba50b051b49f408bbb8bab0652761dc83af4. Official verification with the protected original run manifest passed.
- Official filtered Smoke: 5/5, run 0a2236b8f8a9468c8a01e8d3963027bb, same stable implementation commit/tree. The filtered original remains private and no filtered public package is claimed; all five passing test identities also appear in the verified full EditMode package.
- git diff --check across base to candidate: PASS.
- The public Unity manifest contains no local workstation paths or credential-like values. The archive contains the normalized result only; raw logs and original XML remain protected.

## Scope and integration limits

This is a bounded source-boundary checkpoint only. It does not complete CORE-F, move the P12-E package coordinator or the shared runtime into Core, implement DailyV1Application, or establish detached .NET restoration/host continuation. It does not change P12-A readiness, P12-G status, P13 readiness, or Phase 12 closure. Canonical promotion remains a separate workflow.