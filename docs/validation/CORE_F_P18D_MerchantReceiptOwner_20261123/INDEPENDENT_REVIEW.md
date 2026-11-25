# CORE-F P18-D Merchant Trade State Receipt Owner — Independent Review

**Verdict:** `VALIDATED_CANDIDATE` / PASS, no actionable findings.

- **Candidate commit:** `9d914720923fd949b780d111a2d5f1f084eac735`
- **Candidate tree:** `fdd7788638638183f5bc211b8a793bba8d04b50d`
- **Reviewed base:** `7c4338d1d7588df4ad3795a31a162d661afd9dd1`
- **Core canonical:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (unchanged)
- **Review mode:** independent, read-only exact-tip implementation, test, manifest, and package review.

## Scope reviewed

The Core transaction owns selected NPC Merchant trade-state receipts and uses the existing prepared Core plan/travel installs. Receipt replay and fingerprint behavior remain attached to the authoritative owner. P12 binds the exact Core receipt owner but continues to reject populated receipt state; this checkpoint does not introduce receipt persistence.

The prior finding that the Unity `NpcTravelPlanRuntime` projection could remain stale after a committed Core travel-plan install is resolved. The commit bridge now synchronizes the existing Unity projection from the committed Core state and publishes the existing P12 notification once. The integration test verifies exact target Location/City object identity, reason, utility, cost, source decision identity, and compatibility-wrapper revision. No duplicate receipt owner, mutation, or epoch notification was found.

## Evidence reviewed

- CoreValidation cold and warm: `1198/1198` each. The independent reviewer verified the two original TRX hashes and byte counts against the private manifest, distinct cold/warm run IDs, and all 1,198 passing result records with zero non-pass outcomes. The 557 Core inputs match the current files. Original TRX and logs remain protected; no public .NET TRX package is claimed because the run has no approved source-context record.
- Official Unity EditMode: `3326/3326` PASS, stable input; all 1,988 snapshotted file hashes matched the current worktree.
- Official EditMode Smoke filter: `5/5` PASS; the five test identities also occur once as passed in the complete EditMode report.
- Focused selected Merchant integration test: `1/1` PASS.
- Full EditMode package: `coref-p18d-merchant-receipt-editmode-20261123.zip`, SHA-256 `910e03074e71e5921b593e3df3db026f7193c862a995ee5814694b6fb9c4912f`; official verifier passed. The normalized XML has no workstation path or `<output>` element.
- All 24 committed blobs match the reviewed worktree; no protected `ProjectSettings`, unrelated validation record, private evidence, or `.codex-work` path is in the commit.
- `git diff --check`: PASS.

Filtered focused and Smoke XML originals were not edited. The official packager rejects those filtered reports because parent-suite `testcasecount` attributes retain unfiltered counts; they remain private and are not counted as packaged evidence. The full EditMode package includes the focused case and all five Smoke cases.

## Disposition

This is a bounded candidate checkpoint only, not Core canonical promotion or CORE-F completion. The exact missing census set is reduced from 23 to 13 rows: Crime P18 (1), Justice P18 (1), keyed-sale Economy (1), and NPC LocalObservation (10). Complete 89-family coverage, census sealing, active session readiness, Daily-v1 startup, detached restoration, and process-separated continuation remain unproven.