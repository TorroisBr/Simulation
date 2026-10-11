# P12-G post-boundary pending ActorChoice admission validation

**Status:** final exact-tree validation PASS for the test-only candidate.

- P12 canonical base: `70d9ac29e1ec14ffe8792e9313f42bd4821c72a9`
- Test implementation commit: `1164966c783e41e3cc4ea446d0e0e819a828fb55`
- Implementation Git tree: `b26538ec09b91cf9bf06d2d2daa7dd68937f8ef5`
- Validated `Assets` tree: `af1cd303919956cadda6ceed0547e6be661a71c3`
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- Unity Editor: `6000.3.9f1`

## Evidence boundary

The fixture registers and binds a Person to the authored Daily-v1 Pedro before the first successful daily boundary. This is a test fixture using the existing runtime APIs; it does not claim that SampleScene currently offers a Person-backed actor-choice consumer. The fixture then submits a supported SellGoods choice through the existing `LocalPlayer/Request` handler after that boundary.

The completed boundary contains the exact registered P11 `p12f.actor-choice-inputs` row with cardinality 0 and revision 0. The normal request changes that same P11 owner to cardinality 1 and advances its local revision once. The test confirms the old completed-boundary token is stale while the runtime remains healthy. It does not treat the unregistered P18 temporal-input witness as a Daily-v1 owner section.

The restore attempt rejects as `InvalidSourceSession` before any private restore stage. During that attempt, the test observes zero `AdvanceDayAfterClockAdvance` callback entries. It verifies that the current session and source runtime remain active, the pending ActorChoice identity/payload/disposition, P11 owner identity/cardinality/revision, store count and next sequence remain unchanged by the failed attempt, and current facts and continuation roots remain unchanged. Both source and uninterrupted control then consume the queued input through normal daily execution and produce equal complete included-owner projections. A valid restore at the new completed boundary and the following boundary also match the control.

This is a source-reachable stale-boundary admission rejection. It does **not** capture or restore a Pending ActorChoice snapshot, exercise F-hydrator rejection, change production behavior, or establish runtime-wide no-gameplay/quiescence, general no-replay, complete P12-G evidence, P12-A readiness, P13 readiness, or Phase 12 closure. No serialization, export, hydration, profile, or gameplay scope was added.

The prior candidate validation on Assets tree `edef6c0e4e8433465a5e49e10dc646ec6cf995fe` is superseded because it observed the P18 temporal provider instead of the registered P11 section. Its candidate commit and artifacts remain in Git history; they are not evidence for this final Assets tree.

## Validation

All final runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` against the same final validated Assets tree. Every report had zero failures, skips, and inconclusive tests.

| Run | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused new case | 1/1 | `6eb0e443f419353c11fa5ccedabc57faec37bf7f30b22949662f5c02d461c6e6` | `475c21b1701d70b675d8502af9699b7e5b5a922dc924a8c3e3a3d91595e00092` | `5a545a9115700bf8cb5e61be34506e54f0d1169f1d63cd1c71cd0ec4e90893c8` |
| `SimulationRuntimeAdmissionTests` | 189/189 | `78a06fb0b8fd7a4a558aa8d3db2d5608cb141fa3d54f19c4655b48c40b62d637` | `7a4b6cfa3bb1e2e2152f714d2ada895f4f33f8b7857b80cde84633149636bcd2` | `d689cb2f1e3218e78d901177c65afba242528412932c16ba9d121f1827430c34` |
| ALL EditMode | 2863/2863 | `7b5f3ffe29890558e62cd8a01147354a07f916bb074d2929dd86db875ea55382` | `937823bff67f0b1453bdc7650510b932a0fec18415419f635f01f35af48ccdd8` | `9bdddde4c4f30933cf174caf6f39f7fef926890ff00194c7a40ddbb4ef8d694c` |
| Official Smoke | 5/5 | `84f2ff7654e9d3072b8039d74cfd2c649d61db4a9458084cb846d18449be099e` | `667ef77bef506e96849d56811497e4925869a8b8a23bcfc575f77ada1d3edc9a` | `f321998e4b666ea0511254f0a3245ae94b131712c17b403c520c4873d78fc307` |

`git diff --check 70d9ac29e1ec14ffe8792e9313f42bd4821c72a9..1164966c783e41e3cc4ea446d0e0e819a828fb55` passed for the implementation.
