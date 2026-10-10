# P12-G restored-boundary coordinator candidate validation

## Candidate identity

- Canonical base: `codex/phase12/canonical` at `3c1575530a1d44c3932767b6e8879be2e6672dc8`.
- Code candidate: `d5bb1ff1603f1ec1e1197b5e7368496e859fee48`.
- Code candidate Git tree: `88c43f59e77cf4f63ad67cc7252906163f2914bf`.
- Code candidate `Assets` tree: `cb8d8a116ff59ada9f334b4c29f218799f94eb87`.
- Unity Editor: `6000.3.9f1`.
- The validation runs below used the exact tracked code committed at the code candidate SHA. The candidate `Assets` tree is unchanged after validation.

## Scope exercised

The coordinator now checks reciprocal active TravelParty/NPC bindings against the staged route, locations, travel progress, and destination City projection before restored-boundary admission. The regression corrupts one private target NPC's `ActiveTravelPartyId`, confirms rejection before publication, verifies the old active graph and token remain valid, then advances against an uninterrupted control and retries a valid restore.

Failure injection now has separate D, E, and F staging boundaries in addition to source capture, C roots, candidate composition, target validation, boundary admission, and pre-publication. The parity fixture compares the included C–F owner projection over multiple future boundaries, including a deterministic autonomous action, a pending directive through its due boundary, and an active TravelParty that is retained without replaying its charge or movement.

This is a validated P12-G implementation candidate only. It does not define a serialized envelope, claim P12-G completion, authorize P12-A export/hydration, make P12-A ready, or unblock P13.

## Validation results

All XML files report `Passed` with zero failed, skipped, or inconclusive tests. XML SHA-256 values are recorded below. Unity logs are archived in [`P12GRestoreCoordinator-UnityLogs.zip`](P12GRestoreCoordinator-UnityLogs.zip), SHA-256 `9C73C9E81EA49ABA56538E7363B3120B0A047DF692BFC6E2B40477C51C44EBAB`.

| Gate | Result | XML | XML SHA-256 |
|---|---:|---|---|
| Focused `SimulationRuntimeAdmissionTests` | 94/94 | [`EditMode-20261010-044704-9db03b90388c45a7bfc40b2ba2f119d1.xml`](Raw/Focused-Final/EditMode-20261010-044704-9db03b90388c45a7bfc40b2ba2f119d1.xml) | `1DBAB88E602E65EC1BD775D89F3BB057B6599584121DC1A06F32FACFF44094C0` |
| ALL EditMode | 2767/2767 | [`EditMode-20261010-044721-b31f1877a847414e8b759aebfad23142.xml`](Raw/All-EditMode/EditMode-20261010-044721-b31f1877a847414e8b759aebfad23142.xml) | `F844A8FECEB78FA287C0C22CCDDACA4A2549C2F8902CDB034D4DF887062F84B1` |
| Official Smoke (`-TestFilter Smoke`) | 5/5 | [`EditMode-20261010-044803-36e3077c7c23439388936d99fefa5597.xml`](Raw/Official-Smoke/EditMode-20261010-044803-36e3077c7c23439388936d99fefa5597.xml) | `45230926E8964219A9170B37334D71CE72A9FA2B6817B25D4DB0443AFB762118` |
| `git diff --check` | PASS | `3c1575530a1d44c3932767b6e8879be2e6672dc8..d5bb1ff1603f1ec1e1197b5e7368496e859fee48` | — |

The isolated integrated-corruption test also passed 1/1 at the same code tree; its XML is [`EditMode-20261010-044646-77eeadf883fb493da90f1200bb628b56.xml`](Raw/Focused-CrossOwner-R2/EditMode-20261010-044646-77eeadf883fb493da90f1200bb628b56.xml), SHA-256 `01A1DE1088342F6D4CBCCCA92378C1759485B0CF2D94C79303FAE90CA854744B`.

## Preserved limits

- P12-G remains `WAIT_DEPENDENCY` pending exact-tip independent review and closure of every remaining §6 rejection/atomicity obligation.
- P12-A remains `WAIT_DEPENDENCY`; no capture, export, or hydration readiness is implied.
- P13 remains `BLOCKED`.
- Phase 12 remains `OPEN`.
- The unrelated `ProjectSettings` edits and untracked `.meta` files are excluded from the candidate.
