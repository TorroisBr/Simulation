# Selected Daily-v1 Immigration and Emigration — Validation

## Implemented boundary

The selected Daily-v1 immigration and emigration methods now prepare and commit through a Core-owned operation over the exact selected runtime-owner graph. The operation binds the exact NPC owner, City population owner, and exactly one residence authority: the PersonStore-backed Person or the NPC-only residence owner.

Core validates actor life state, owner identities, selected definitions, pre-operation residence, City population/revision, the represented-resident floor, and revision capacity before admission. At commit it revalidates those facts and the resident floor. The City owner validates its transition and P12 writer admission before invoking the residence commit callback; after the residence write succeeds, only the prepared City value/revision write and its existing mutation notification remain.

The Unity runtime retains the public lifecycle API and P12 scope/reservation boundary. The operations keep their existing distinct IDs, `runtime.population.immigration` and `runtime.population.emigration`, and their exact census rows: one City aggregate row and one residence row. Success advances one City revision, one residence revision, and one reserved P12 epoch. The Person-backed route does not add a duplicate NPC-residence census row.

The Core path is selected only for the Daily-v1 capture profile when the roster has no unselected runtime member and both target owners exist in the exact Core graph. Legacy or mixed owner compositions continue through the existing Unity lifecycle implementation. Failure to build the admitted P12 operation scope now faults closed rather than reaching an unscoped lifecycle write.

## Review and correction

The first focused P12 class run exposed one compatibility regression: a selected NPC paired with a legacy City owner was incorrectly sent to the Core path. The Core selection gate now requires both selected owners; the existing P12 lifecycle suite passes after that correction. No test assertions were weakened.

A final preflight review also caught that emigration advances the residence revision as well as immigration. Core now rejects a saturated residence revision for either operation before admission or mutation. CoreValidation and the Unity regression suites below were rerun after this correction.

## Source identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Implementation base: `ecb26c45e3957e70973ea91bb2531605af91d288` (docs-only exact-tip review record successor of reviewed residence-migration checkpoint)
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Full Unity input digest: `99c9ef9e86a5b72a892dca5238366f7b0c2438988de6cf271104fd564158fbca`
- CoreValidation input digest: `fdde54d84a407354db9980753e66fcfc3a20d339affb60c280051c9c5140be4b`
- Official P12, full EditMode, and Smoke runs reported stable before/after snapshots. The following implementation and test hashes match the final official full EditMode snapshot.

| File | SHA-256 |
| --- | --- |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedPopulationChangeOperation.cs` | `94af7c96c0dc923a8f0219df3784386e3f98604deefb71e6d506fcfc941db4c5` |
| `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedPopulationChangeOperation.cs.meta` | `1dacc74e5ff14e6ea09e0543e0463050b46287250b0ff337a1ff5308d3b2190e` |
| `Packages/com.simulation.core/Runtime/Population/SettlementPopulationRuntime.cs` | `033b6a484438201646b4caacceed4be134aabf6aed683bc29cfbeb08945eec26` |
| `Packages/com.simulation.core/Runtime/Population/PopulationOwnerBridge.cs` | `b6eab49c49b32aa5bbd660bc8a00f06b5bc51375f781edbaeaacdce4745824dc` |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `5e00408c160c4e44b51b260d7b43e698567dd4eb3dc95bae83a72585f7be821e` |
| `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs` | `08d4c172bf735090b844657db0d98831a5019e5dcca5e61e8defa3a7e48ba2b2` |

## Validation

| Validation | Run ID | Result | Input identity |
| --- | --- | --- | --- |
| Official CoreValidation (`-SkipLab`) | `a7659ababbc04c30b1428ffefedc7662` | 1,092/1,092 cold and 1,092/1,092 warm PASS | `fdde54d84a407354db9980753e66fcfc3a20d339affb60c280051c9c5140be4b` |
| P12 population lifecycle EditMode | `7fa3fd8a6abd4edeb2de19dec7bcccf6` | 29/29 PASS | `3b7e9d30950963d779a2d3b75a22de6862227f7360d5c132f039e6569de00b85` |
| Full Unity EditMode | `a760a2ed4a3a4dc0b85f2e2395302dfc` | 3,312/3,312 PASS; zero failed/skipped/inconclusive | `99c9ef9e86a5b72a892dca5238366f7b0c2438988de6cf271104fd564158fbca` |
| Official Smoke filter (`Smoke`) | `4be60d781fa54a4cb002b2e747fcd452` | 5/5 PASS; zero failed/skipped/inconclusive | `99c9ef9e86a5b72a892dca5238366f7b0c2438988de6cf271104fd564158fbca` |
| `git diff --check` | candidate source | PASS | implementation/test/docs changes |

CoreValidation raw cold/warm TRX and logs remain in the private evidence store. Its `simulation-core-validation/v1` manifest is not accepted by the current public DotNetTrx normalization adapter. No public normalized .NET package is claimed. Private manifest SHA-256: `b99ab6d8adb76ef3cedb33d3917dd895450036b85170ab96119e118b8c70e773`; cold TRX SHA-256: `6f540816b71904a725b5247d4cb1ae528f737a4a57b02cb9210e33e71db4b821`; warm TRX SHA-256: `59d3a46f5252693ae1b71bb9a7593d1d47a3eca9d1da248e80273eb73fec5368`.

The approved Unity packager accepted and verified the final full EditMode report for the normalized metadata snapshot:

- Archive: `UnityExact/5d0e9385e91d48b8ab4a35e0ca6435ba.zip`
- Archive SHA-256: `d09475440e33d7e88488d92cd02024a156d9c5c0ef03fab55fc00690a58c2c08`
- Archive size: 175,581 bytes
- Package manifest SHA-256: `faf1a4d392bb035808d6cc0d219e580f0548e7f9fb38580378be582c67462196`
- Official package verification: PASS; one run, one XML archive entry.
- Full EditMode original XML SHA-256: `940cd54f22dbc99c38fc96d9a3e9a082af64b36bc0af1483f94d953a605d10c3`
- Full EditMode log SHA-256: `1ddb591e164d1d9627255e1117e31cc037ac7b8ccf273f5246b856c8ae62245f`

The filtered Smoke run passed 5/5 on the same exact input snapshot. Its standalone XML remains in private evidence: the official packager rejected the filtered report because the parent `Simulation` suite's declared testcase count did not match its descendants. The full EditMode package above contains and validates all Smoke tests on this exact snapshot.

The earlier `UnityFinal/` package was generated before the trailing-blank-line-only normalization of the new `.meta` file; it is retained unchanged as superseded evidence:

- Archive SHA-256: `aab1684b9bb2c29f8ef2936b8138bed8c10d586d50f8773d2e588c7a763d9fa2`
- Package manifest SHA-256: `3ea24699ec637ac0b636352e8ad3a9f606088bb58e1c90c10628f474195d8466`

The still earlier full EditMode package under `Unity/` predates the final residence-revision preflight correction and is also retained unchanged:

- Archive SHA-256: `10d70c726a4f07963e483fd9af86bfc071c856a537b3c0e845acf853a4f851bd`
- Package manifest SHA-256: `90cf427551438f993ba7af4fa8230218bcaa76fbe053c9cdb31b87cd5806f466`

Only the `UnityExact/` package is the final exact-source package for this candidate. Raw XML/logs remain in the private evidence store; no local workstation paths are included in the published package.
## Scope boundary

This is a validated intermediate CORE-F checkpoint. It does not promote Core canonical or complete the shared runtime/application/restore/host contract. The pre-existing ProjectSettings edit, unrelated local validation material, metadata, and private evidence remain preserved and unstaged.
