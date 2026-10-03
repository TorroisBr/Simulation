# P12-B ActorChoice P11 invalidation candidate

**Status:** Independent exact-tip implementation review PASS; bounded
promotion preflight pending. See
[`PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_IMPLEMENTATION_REVIEW.md`](PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_IMPLEMENTATION_REVIEW.md).

**Canonical base:** `f1ec63ea7fa0592b3a280e138a80023e3cacc6b7`.

**Design:** `bd91d8aa42671c1e328bbeacd29c1b48f41e0cae`; independent design
review PASS is recorded in
[`PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_DESIGN_REVIEW.md`](PHASE12_P12B_ACTOR_CHOICE_INVALIDATION_DESIGN_REVIEW.md).

**Implementation code tip:** `0bb87c89662397857c7e55267bbc60f32ce0676a`.

**Exact candidate tree:** `aa570c943299eafe52c9a5a9b05a9487bdfd5add`.

## Scope delivered

The selected daily P12 protocol registers the required schema-v1
`p12f.actor-choice-inputs` section on the exact runtime-owned
`ActorChoiceStore`. It verifies the live store identity, section/schema,
dynamic P11 cardinality, and shared local revision at setup and each commit
boundary. P11 capture and every successful P11 disposition commit preflight
the owner thread, current census baseline, and epoch capacity before mutation;
the committed write then reports through the existing P12 section notification
path. Existing active TravelParty, Merchant, and solo-travel batch collectors
remain the notification route when one is active.

The P18 temporal provider remains a distinct section and is not registered by
the selected daily profile. No temporal writer callback or P18 behavior was
added. Decision recording and action execution remain separate operations;
P11 order, no-fallback/rethrow, and partial-result behavior are unchanged.

Changed executable/test paths:

- `Assets/_Project/Scripts/ActorChoiceStore.cs`
- `Assets/_Project/Scripts/SimulationRuntime.cs`
- `Assets/_Project/Tests/EditMode/Editor/ActorChoiceCensusTests.cs`

No ProjectSettings or `.meta` file is part of the candidate diff.

## Exact-tree validation

All gates ran in a detached clean checkout at the exact code tip above. XML
and log files were copied immediately after each Unity invocation into the
ignored local artifact directory
`E:\GitHub\GeneralSimulation\MainSimulation\.worktrees\p12-actorchoice-validation\Library\P12ActorChoiceValidation`;
their SHA-256 values are listed below. `git diff --check` and the staged
candidate diff check passed.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `ActorChoiceCensusTests` | 9/9 | `0F8B5C7C4BD327E4FAB7F97DEA613C6BCDD78BEED7DC62463A168B2A20A6386A` | `C6263860968DE294866F73B181086157D2DFBF50D26303FD466F417E2EF3C9C0` |
| `ActorChoice` | 49/49 | `36E1589B3F23842AE29AD6D4CB992C45B87EBD2958418F9D148BE4C0501B1176` | `EFD55801B7D72B56F2F3D1F1F17DB2D61BF141A41B4381A6D8ABF8C078D4F62F` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `6F2359797A698152C430F923AE9ED198C0B14F254993B6D1A5A5FD05F34ACB65` | `13E0347CA6FCA24F484F0D83D560E7A93BACDD428244E4BAC9AD505995D1EB34` |
| `SimulationBootstrapCompositionTests` | 21/21 | `ABFC4C3019563C7985F7BA8BC1AD9059727F89A8DDB04B868ADBD28802A9A3CD` | `0594F1530DA6C3AC94A4266164CB8C768E704D4A3C1639381ACAE7D4A6D2CA7C` |
| ALL EditMode | 2231/2231 | `3CA8EC33671C28AACDB3007DA0BA57884BA9A8FDAE69E7CA44BE3B609F500E73` | `08604D0164CE50E21E6C1BA6FFB1D9491F5EC582DC02D6039707410CAB9FBBB0` |
| Official EditMode Smoke | 5/5 | `CC84712165A0B6F90DE6207ED15DA83739744889854990AE2E758ED811DA8F23` | `29AAEFFA49723E756D313527A949335000DDF931FC357BA0DE3816C3ECEBD356` |

The harness reported zero failed or skipped tests for every gate.

## Limits retained

This candidate covers only P11 ActorChoice owner registration and invalidation
for the accepted `UnityBootstrap-Daily-v1` profile. It does not include the
P18 temporal section, ActorChoice export/hydration, capture eligibility,
complete owner or shared-epoch coverage, global operation/quiescence claims,
P12-A readiness, P12-B completion, P13 readiness, or Phase closure. P12-B
remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
