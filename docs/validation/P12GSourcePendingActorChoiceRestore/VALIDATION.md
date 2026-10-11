# P12-G post-boundary pending ActorChoice admission validation

**Status:** exact-tree validation PASS for the test-only candidate.

- P12 canonical base: `70d9ac29e1ec14ffe8792e9313f42bd4821c72a9`
- Test implementation commit: `18d2a2dde4963be39c09e3961e288a251e358878`
- Implementation Git tree: `54ead00dfe14bb704e32e9802d68135244d0315e`
- Validated `Assets` tree: `edef6c0e4e8433465a5e49e10dc646ec6cf995fe`
- Changed executable path: `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- Unity Editor: `6000.3.9f1`

## Evidence boundary

The fixture registers and binds a Person to the authored Daily-v1 Pedro before the first successful daily boundary. It then submits a supported SellGoods choice through the existing trusted `LocalPlayer/Request` handler after that boundary. The accepted P11 owner mutation makes the previous completed-boundary token stale while leaving the runtime healthy.

The restore attempt rejects as `InvalidSourceSession` before any private restore stage. During that attempt, the test observes zero `AdvanceDayAfterClockAdvance` callback entries. It verifies that the current session and source runtime remain active, the pending ActorChoice identity/payload/disposition, census owner identity/cardinality/revision, store count and next sequence remain unchanged by the attempt, and the current facts and continuation roots remain unchanged. Both source and uninterrupted control then consume the queued input through normal daily execution and produce equal complete included-owner projections. A valid restore at the new completed boundary and the following boundary also match the control.

This is a source-reachable stale-boundary admission rejection. It does **not** capture or restore a Pending ActorChoice snapshot, exercise F-hydrator rejection, change production behavior, or establish runtime-wide no-gameplay/quiescence, general no-replay, complete P12-G evidence, P12-A readiness, P13 readiness, or Phase 12 closure. No serialization, export, hydration, profile, or gameplay scope was added.

## Validation

All final runs used `Tools/UnityValidation/Invoke-UnityValidation.ps1` against the same validated Assets tree. Every report had zero failures, skips, and inconclusive tests.

| Run | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused new case | 1/1 | `0967c6e1dac87e04097ae3c0b8757e1029412221a48151422a0716abcd986225` | `b9434308fc4def03a0654df75a0c6bb56be6721c2564b0a621c5495c03f48c78` | `2a5524b21cf3df4f074ee1fad6afe6aeb5fea58891159f737c067c7d67800bc7` |
| `SimulationRuntimeAdmissionTests` | 189/189 | `c3a4bfcdbffddd114185e75a37b7648518328546fe474f7686f9c9732832fdab` | `378969f421e2b5c41d01cc0c40b8ed7bf3e02c333c7350d7c0f0bf784facfb29` | `5e2e1ca529982a39dde007cedb2284a114f30210d87b39f046bb03303d78f177` |
| ALL EditMode | 2863/2863 | `92f0ee17d3c1470b46cf71ec6b68eda602172d399e4880e15d35942ba9c89ab6` | `f4a1ebef5c82c4cfba50c236f3485e7002232db4fd360f2b0360c6ed3c4e2c50` | `081d1755992ed5356608686406003d4d1fd31c4f268a3d6ae4ce9a1367c1206d` |
| Official Smoke | 5/5 | `8e035fc493faff3ee819d1c152e5e38d0c0efcf3ca85b390a3d311adfeec829f` | `f97ae2340d3b3290dab4bfab39cc9a31a0af43bf02b6c521fdbbcbdbced561cc` | `62737958fd5642f23c31ecbc2254710d42d186271be6acd2b946581e24825e85` |

`git diff --check 70d9ac29e1ec14ffe8792e9313f42bd4821c72a9..18d2a2dde4963be39c09e3961e288a251e358878` passed for the candidate implementation.
