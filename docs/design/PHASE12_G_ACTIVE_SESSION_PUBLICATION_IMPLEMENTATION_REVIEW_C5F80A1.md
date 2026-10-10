# P12-G active-session publication — independent implementation review

**Verdict:** `VALIDATED_CANDIDATE` — exact-tip review passed for the bounded active-session holder and serialized restored-session exchange seam.

**P12 canonical base/current tip:** `8f5ec2cb9211a6520f31e65e83450d5bd9bfb039`

**Code candidate:** `c5f80a1aaeedbba00e713829a2479544ad50f340`

**Code Git tree:** `e158a38add785c2fbc80f3529f0c6869fa898730`

**Code Assets tree:** `a253d7425c9d482da136d0fca8acd17b2445870b`

**Validation evidence tip:** `1c4ff4dba21aab9ee498f0c9615df17cfbdd15a3`

**Reviewer:** `actor_choice_impl_review`, independent of the implementation author.

## Review result

The complete base-to-code diff is limited to `SimulationRuntime.cs`, `TesteSimulacao.cs`, four focused test files, and retained validation evidence. The amended code candidate adds the successful exchange test after the prior review; the validation tip adds the exact-tree manifest and artifacts without changing the code tree. The candidate `Assets` tree matches the manifest. `git diff --check` passes from the exact canonical base to code candidate.

The active-session object contains the composition and the runtime-dependent reporting/lookup owners. Its constructor enforces exact reference ownership for the runtime's identity registry, spatial network, explorable-site store, justice/logger, and admission context; the City-by-location index is copied. Published accessors and reporting/lookup operations capture or acquire a single active-session reference, and bootstrap aliases are released after initial publication. Simulation operations hold the active-operation count for their full synchronous use of the selected session. `TryPublishRestoredSession` locks that count and checks the expected active reference, Daily-v1 owner-thread contexts, exact profile/config compatibility, the healthy source boundary, a valid source completed-boundary token, and a valid candidate `RestoredContinuation` token before the single `Interlocked.Exchange`. No owner mutation or fallible initialization follows the exchange. Rejected exchange leaves the source pointer and source runtime unchanged.

The added `RestoredSessionExchangeWaitsForIdleWindowAndSwitchesActiveAliases` test closes the prior review gap. It constructs an active source and a separately built candidate with the same `WorldId`, admits the candidate at the source token's day and completed sequence through the existing restored-admission protocol, confirms exchange is rejected while the source operation count is held, then releases the operation and confirms successful exchange. It asserts the active-session field, composition/runtime/spatial/chronicle/report aliases, day, spatial lookup, and source-token validity after exchange. Reflection is used to arrange the private operation lease and staged candidate because the restore coordinator is explicitly outside this checkpoint; the test does not claim full graph staging or hydration.

No remaining implementation defect was found within the accepted active-session publication boundary. No new gameplay or P12-A/P13 scope is introduced.

## Exact-tree validation evidence

The XML files at validation tip `1c4ff4d` were independently parsed and their SHA-256 values recomputed. Each reports `Passed` with zero failed, skipped, or inconclusive tests. The archived log hash also matches the manifest:

- `SimulationRuntimeAdmissionTests`: 76/76; XML `B28FFA4FF015349419EBEC850AA6595D077CA2DF20969200F2A0773A2F395D2C`; archived log `328FF76A146BBB3C04FBA3B171CCBD0BF3C4A1304E03B7427AE3DDB148950413`.
- `SimulationBootstrapCompositionTests`: 26/26; XML `E288041244DAD8A5715AC5A87721685FB43C752A6AF0993875B2A70E95A41EA7`; archived log `9E05DD7336C7046597F49C0D072635A3E6DA5AE9FD94756A8967D6ED55521B1C`.
- `P10BGeneratedRuinGenesisTests`: 10/10; XML `48BE66FEAB6CB1829319E394746E6E71D32662680C873DAAE43BEB7C4DA3F33C`; archived log `16EF7E7E73B8ED6461AB1B569E36ED84212C69D30741F173F52199F3FA7115E4`.
- `P12CrimeSocialAppraisalInvalidationTests`: 12/12; XML `29F77678AB6CCA8CD27FBA1253486201F0F773734ACB72242DEDF5B147E0F607`; archived log `BEA93FA88315826359C010A7C905522153F6799674EE3A02170B20244A734A5E`.
- ALL EditMode: 2739/2739; XML `763349FC32F58E3E5581B9742AF81D2C5F32A2DFAFF66AC4C0AD2A67D97B6325`; archived log `B22B88AFF44600F27E9B5AA2189AA9CEC0CB38AFCABBE88F47157D4369394943`.
- Official Smoke: 5/5; XML `F7210B0C19E11DE9A57D797C034940FEBF18CF31D67AD620130DB2BD17D9C5E2`; archived log `C813094793AFFF9500B8EA71F8C2BFC9069EBF5374A52275CE7B35C629A5590D`.
- Archived log bundle SHA-256: `D042CED2F871D233824A6EFE197A4A2E8A7093F6EA0CFE2A88F419643FCFC5F5`.
- Exact base-to-candidate `git diff --check`: PASS.

No Unity tests were rerun during this independent review; these are the retained exact-tree results.

## Scope and remaining limits

This review validates the active-session holder, consumer redirection, bootstrap-alias release, and idle owner-thread exchange seam only. The candidate does not implement the restore coordinator, complete graph staging/validation or owner hydration, whole-graph failure atomicity, no-replay proof, continuation parity, or complete owner coverage. It does not make P12-G or P12-A ready, establish P13 readiness, promote code to canonical, or close Phase 12. P12-B and P12-A remain incomplete/`WAIT_DEPENDENCY`; Phase 12 remains open.
