# P12-B ScheduledDirective owner-invalidation candidate

**Status:** `VALIDATED_CANDIDATE`; independent exact-tip implementation review is pending.

- **P12 canonical base:** `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`.
- **Consumed architecture/DAG tip:** `da34d50bd7831ac3eefab31e925492ede8dded5c` (`codex/architecture/world-identity-projection`). The capability-level update leaves this accepted P12-B owner work active and requires targeted review of profile admission; the selected profile boundary remains unchanged.
- **Reviewed design:** `d9228b1ce8c63a0480e46b00648564516d383601`; durable exact-tip review record `3669d3fadfe4a04add44fa145f00855d62cf69d7`, verdict `PASS` by two independent design reviewers.
- **Implementation commit:** `b8dced9666438d736c8bd2b417390d52988f3c78`.
- **Implementation tree:** `248abaacad1538a40a4b0a7af4e1898749993128`.
- **Candidate branch:** `codex/phase12/P12BScheduledDirectiveInvalidationImplementation`.

## Bounded behavior delivered

The selected `UnityBootstrap-Daily-v1` runtime registers the existing `p12f.scheduled-directives` schema-v1 owner only when its `ScheduledDirectiveSystem` is installed. Its provider and local revision represent the exact installed store. Standalone runtimes may omit the optional system and omit this section. `SimulationBootstrapComposition` still requires the published provider to match the runtime's exact registered store, so a selected publication cannot claim a missing or different owner.

Genesis rows remain part of the initial census baseline. After binding, successful `ScheduledDirectiveStore.Add` and stored-row terminal transitions preflight owner thread, exact live owner/revision, the selected baseline and epoch capacity before changing owner-visible state. A denied selected-P12 preflight faults and propagates before an incoming directive is bound or a row changes. Successful commits advance local revision once, then notify the existing P12 dispatcher; an active TravelParty, Merchant or solo-travel operation accounts for the section at its outer boundary. `PrepareDay` conflict/unresolved-actor skips and `MarkSucceeded`/`MarkFailed`/`MarkSkipped` use the existing domain semantics. `TryTakeDirective` remains transient.

## Exact-tree validation

All artifacts were generated on the implementation tree above, using `Tools/UnityValidation/Invoke-UnityValidation.ps1`. XML and log artifacts are retained under the ignored worktree path `Library/ValidationEvidence/P12B-ScheduledDirective/`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `ScheduledDirectiveCensusTests` | 16/16 | `54C9BED95B77D6C0F5AFE4A58DDA6BAE69B9C5614C44E335EEB2C689236170D1` | `9E8A8377331E8F992857DBBF0B23E9757EDE75020D22706B408C31B05603B1D5` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `513AC42A853FE050CAE094D77858BBCE0B9A2E226A1FD3F60F7B9FD7626B7BA4` | `895F19C4D66E6FD18507D0B1FC6A57E31479285CF39A51D85DDFEA2184CCC7A5` |
| `SimulationBootstrapCompositionTests` | 21/21 | `BC6EF0933B9A85E095EBEB61C644379C508364F1470111E83E1720E12A5E31A2` | `54D9416F747D5459CCBB080402837B7F0296F200BC463A944EC456F8102C9049` |
| `SimulationRuntimeOrchestrationTests` | 12/12 | `809F980B75DE70995AEFA23F98C22B116961019FA20B6AF2FF3F9999E8070DD5` | `5B569108546236465EBFA72D3574F1A4ACAF8A24FA342F8003A5D1ED74CF56C8` |
| ALL EditMode | 2241/2241 | `0338B4CE0A8FBAC83117F5A376AD621C63694D489B3D7C29CE6F7536F30D32DB` | `ED00070213374C38BB35013F2866F6A9E48824D713F11BB28DED1A62ED8B4DA7` |
| Official EditMode Smoke | 5/5 | `72FD8E0E276C5DAB66C0D75FD5AEFA8C324AB938D0C0C0FC0C1F35B5809C0380` | `8550825FAB552C44CE1FE9DAC853EBAC1AADA89977E6BA13004F56BE272075BE` |

`git diff --check` passed for the candidate changes. Validation also passed with the pre-existing unrelated user changes present; those files are excluded from this candidate.

## Limitations and exclusions

This is only selected-profile ScheduledDirective owner registration/invalidation. It does not make a directive action and its effects atomic, roll back earlier domain effects after a later terminal-write rejection, or establish complete owner/operation/shared-epoch coverage, global quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B completion, P13 fork readiness, or Phase 12 closure. P12-B remains incomplete and P12-A remains `WAIT_DEPENDENCY`.