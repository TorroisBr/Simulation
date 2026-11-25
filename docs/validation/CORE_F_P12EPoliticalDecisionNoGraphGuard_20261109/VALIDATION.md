# CORE-F P12-E PoliticalDecision No-Graph Removal Guard - Validation

Date: 2026-11-09

## Scope

This follow-up closes the exact-tip review finding for candidate `24b3ff4e5998528cd60134bc3966e7c88a62810`. The legacy `TryRemoveCoreOwnedSelectedRuntimeSections` overload that has no owner graph now fails closed if the section vector contains the P12-E PoliticalDecision row. It cannot strip that row without the graph-aware overload's exact identity/cardinality/revision validation. A regression in the existing census-composition suite covers the no-graph route. The graph-aware production path and P12 format are unchanged.

The correction does not alter Justice Records, the Justice/P18 receipt shared identity, P12 schema or identity, or the General Architect's host-neutral identity contract. It does not imply P12-A readiness, complete P12 coverage, detached restoration, or CORE-F completion.

## Exact implementation identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate base: `4621b39a0449a045b5bffeb54bf491ee8b5fa9a4`
- Corrective implementation commit: `8a72a4acab85c51fc1d77ff63c91bffd467aab9a`
- Corrective implementation tree: `25de2f2d06a293c20fe31e515175d2a322742196`
- `DailyV1RuntimeAdvanceStage.cs` SHA-256: `79f2de098ce4b81d179ccef62bc3184ad471a9351be031322ab3e73ce858b325`
- `DailyV1ApplicationSessionGateTests.cs` SHA-256: `ecda9e67ec55d80cdb3be7bfedceafba3ac4dfa52afa044dd2d589b5eba0535c`

Validation ran against the working-tree source snapshot. Both changed file hashes are unchanged in the corrective implementation commit. No source or test edits were made after validation.

## Validation

- Official CoreValidation run `71f0335865b44755bd46a45b667c495d`: PASS, 1,081/1,081 cold and 1,081/1,081 warm; zero failed, errored, timed out, aborted, inconclusive, or unexecuted cases. Stable input digest: `79ef005dee763cb4fcbe0ebe61683f1d6dbae65719affeaa68158f60e490fc34`.
  - Private run-manifest SHA-256: `f1440610850917b5c7eb321b296915fb0649b5b032435067266c2a04b10bf784`
  - Cold TRX SHA-256: `dbb75e43c2e2fb6bdbfb893f88b354978c7e455c4257ed79211afd2c38b9b33d`
  - Warm TRX SHA-256: `fd766f62f13d52537a75c3d7179028c90718120f79e9fca74792eeaf70b707d2`
- Official full Unity EditMode run `ad95059fe9764cc09e781fb6049f5e06`: PASS, 3,303/3,303; zero failed, skipped, or inconclusive. Unity Editor `6000.3.9f1`, harness `1.1.0`. Its 1,978-file input snapshot was stable at `ecfb902dc46b5d26492ba3e2edea5e468c0758e2eeb06be5919d299250866735`.
  - Private run-manifest SHA-256: `f6136722fc88acc1c0dcf64517c604109ee353a326362ede7d145975a885a5bf`
  - Original XML SHA-256: `d31e82a378cafdb2010d311df160641c6520eaf2833fba7726fdf345da8ba77a`
- Official filtered Smoke run `25b7fc6007d04d28987cb8c4a469329c`: PASS, 5/5; zero failed, skipped, or inconclusive. It used the same stable Unity input snapshot. Its original XML SHA-256 is `3e6b775be9e67518b4311ca9639822413de98ddcc010a11173517c6be3b44ea3`, and its private run-manifest SHA-256 is `d2f443470a3601959f7347825beff7593218db8ab5dd6b445d9f90c31675ed0c`.
- The result packager verified the deterministic full EditMode package: [manifest](../CORE_F_P12EPoliticalDecisionCoreCensus_20261109/Evidence/p12e-core-census-guard-editmode.manifest.json) and [result archive](../CORE_F_P12EPoliticalDecisionCoreCensus_20261109/Evidence/p12e-core-census-guard-editmode.zip). Manifest SHA-256: `0d5ddc7937bca97d0a455fdfc4ff3af15f90c7e5ae3d3ef89805f3b1c3b6fdda`; archive SHA-256: `4b0336e6c3ca5d2444e9e1e41a1be507499ed5c6b1122b55cfacd6edbf408ba7` (174,463 bytes). The archive contains all 3,303 passed EditMode cases, including each of the five exact Smoke cases once.
- The packager rejected a separate normalized bundle for the filtered Smoke XML because its `Simulation` suite `testcasecount` differs from its descendants. The original report remains unchanged in protected evidence. No result XML was edited. The five Smoke test identities are present as Passed cases in the verified full EditMode archive, and the official Smoke runner itself reported 5/5 PASS.
- `git diff --check`: PASS after the final corrective source/test edit and staged checkpoint review.

Protected original XML/logs and private run manifests remain in the official evidence store; this record contains no local machine paths or raw report contents.

## Review and integration boundary

Independent review of prior candidate `24b3ff4e5998528cd60134bc3966e7c88a62810` returned `NEEDS_CHANGES` for the no-graph removal path. The bounded correction above addresses that finding; exact-tip independent review of `8a72a4acab85c51fc1d77ff63c91bffd467aab9a` is pending. The General Architect's identity review `cbacf617` remains PASS and is not changed by this slice. No Core canonical promotion has occurred.
