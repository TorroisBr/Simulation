# CORE-F P12-E PoliticalDecision Core Census — Independent Implementation Review

**Verdict:** VALIDATED_CANDIDATE  
**Review type:** Exact-tip independent implementation review; no promotion decision.  
**Candidate branch:** codex/core/CORE-F-restore-stage-transaction  
**Candidate tip:** 61e8923b902eb96a8095ebf197c9ffb54622920b  
**Candidate tree:** b142f921a2bb7fcd90c149ae1d326666146c829a  
**Implementation correction commit:** 8a72a4acab85c51fc1d77ff63c91bffd467aab9a  
**Correction tree:** 25de2f2d06a293c20fe31e515175d2a322742196  
**Base:** 4621b39a0449a045b5bffeb54bf491ee8b5fa9a4  
**Core canonical at review:** a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

The remote candidate ref was verified at the exact tip above, and the base is an ancestor. The candidate is a docs/evidence tip after the reviewed code correction. The full diff against base was inspected.

## Scope and review

The selected P12-E PoliticalDecision census row is composed from the exact PoliticalDecisionStore in the selected owner graph, which must be bound to the same PersonStore. The witness preserves the existing section ID, schema, Required role, store identity, cardinality and local revision. The graph-aware removal path compares all owner facts before removing the row; host-remainder composition leaves stale/foreign rows for the strict exact-row gate to reject. The restoration path reconstitutes the row against the private candidate session graph before final census composition.

The prior review finding against candidate 24b3ff4e5998528cd60134bc3966e7c88a62810a is closed. The no-graph TryRemoveCoreOwnedSelectedRuntimeSections overload now fails closed whenever the PoliticalDecision row is present, explaining that its removal requires the exact owner graph. The added regression verifies that path. No other actionable findings were found.

Tests preserve existing assertions and add coverage for exact owner identity, stale revision rejection, census remainder/recomposition, and the no-graph bypass. The candidate does not change Justice/P18 receipt identity, P12 row identity/schema/wire format, mutation semantics, or the General Architect's host-neutral identity decision. The separately reviewed architecture record cbacf61759a6403c316148c9a3a15b4c5fa2b073 remains unaffected. No P12-A readiness, complete census coverage, detached-restoration completion, or CORE-F completion is claimed.

## Exact source identity

The validated executable/test snapshot matches the committed source, accounting for this Windows checkout's core.autocrlf=true conversion. The correction's source/test hashes are:

- DailyV1Application.cs: 45d290da7320c5a37637f7aa9c5847c6528fc5d04aa5b33beb820d04aadbed71
- DailyV1RuntimeAdvanceStage.cs: 79f2de098ce4b81d179ccef62bc3184ad471a9351be031322ab3e73ce858b325
- DailyV1ApplicationSessionGateTests.cs: ecda9e67ec55d80cdb3be7bfedceafba3ac4dfa52afa044dd2d589b5eba0535c
- DailyV1SelectedRuntimeOwnerGraphTests.cs: ccf339f0218aa5c482d63dd0570bc27356a22428e9f59a7180bf383788b8478f

The official CoreValidation and Unity snapshots match these bytes (including the correction to DailyV1RuntimeAdvanceStage.cs) and remained stable before/after their runs.

## Validation evidence

- Official CoreValidation run 71f0335865b44755bd46a45b667c495d: PASS, 1,081/1,081 cold and 1,081/1,081 warm. Manifest SHA-256 f1440610850917b5c7eb321b296915fb0649b5b032435067266c2a04b10bf784; cold/warm TRX hashes dbb75e43c2e2fb6bdbfb893f88b354978c7e455c4257ed79211afd2c38b9b33d / fd766f62f13d52537a75c3d7179028c90718120f79e9fca74792eeaf70b707d2.
- Official full Unity EditMode run ad95059fe9764cc09e781fb6049f5e06: PASS, 3,303/3,303, stable 1,978-file input snapshot ecfb902dc46b5d26492ba3e2edea5e468c0758e2eeb06be5919d299250866735. Manifest SHA-256 f6136722fc88acc1c0dcf64517c604109ee353a326362ede7d145975a885a5bf; original XML hash d31e82a378cafdb2010d311df160641c6520eaf2833fba7726fdf345da8ba77a.
- Official filtered Smoke run 25b7fc6007d04d28987cb8c4a469329c: PASS, 5/5, on the same stable input snapshot. Manifest/XML hashes d2f443470a3601959f7347825beff7593218db8ab5dd6b445d9f90c31675ed0c / 3e6b775be9e67518b4311ca9639822413de98ddcc010a11173517c6be3b44ea3.
- The evidence packager rejected a separate normalized bundle for the filtered Smoke XML because the suite's declared testcase count differs from its descendants. The original report was left unchanged. This rejection is explicitly disclosed in the validation record. I parsed the full EditMode archive: all 3,303 cases are Passed and each of the five exact Smoke test identities appears once as Passed. The normalized archive contains no absolute workstation/evidence paths. Archive SHA-256 4b0336e6c3ca5d2444e9e1e41a1be507499ed5c6b1122b55cfacd6edbf408ba7; packaged manifest SHA-256 0d5ddc7937bca97d0a455fdfc4ff3af15f90c7e5ae3d3ef89805f3b1c3b6fdda.
- I independently rehashed the Core reports/logs and all referenced original Unity run manifests/XML files against their private manifests; no mismatches. git diff --check passes.

## Integration boundary

This review records an independently validated candidate only. It does not approve canonical promotion or formal closure. The Core canonical branch remains at the SHA above; the candidate should continue through the separate promotion preflight only after the authorized integration workflow calls for it.
