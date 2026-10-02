# Independent review: WI-A composition with P12 money transfer

**Disposition: `PASS` for the exact combined code candidate below.** This is a targeted composition/source review. It is not canonical promotion, P12-B completion, or new validation authorization.

## Exact inputs

| Item | Exact identity |
|---|---|
| P12 canonical base | `a66c215d1e8e9db34ea559a8bf2a0803fbdbf4ab` |
| Candidate commit | `34ada9279ac332e7f7b1aeb64fb851afdba91fdb` |
| Candidate tree | `28eaa931889acb37929b874aecfb830045044ccf` |
| WI-A canonical | `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5` |
| WI-A implementation | `3b39e0d89858dce517ad72cbb76da621eb954bad` |
| WI-A exact-tip review record | `93b6f0e8cedcb63437cbf5fa5de3461853c81462` |
| Prior P12/WI-A hotspot review | `313095ecec87b11928708607821c6b9ad9b5e275` |
| Architecture baseline | `451340c56e9b676bf6ea43412bcb856b9ccde3de` |

The candidate is a direct child of P12 canonical `a66c215`. Its executable diff from that base is the WI-A composition/publication integration across eight Assets files. The WI-A-specific composition, P18 identity handoff, and bootstrap test files are byte-identical to the previously reviewed WI-A implementation. The only differences between the candidate and WI-A code tip are the three P12 money-transfer files (`EconomyTransactionService.cs`, `SimulationRuntime.cs`, and `SimulationRuntimeAdmissionTests.cs`); those changes are the already promoted P12 transfer implementation that exists in the candidate base. `EconomyTransactionService.cs` is unchanged from the P12 base to this candidate. The P12 transfer operation registration and commit scopes therefore remain intact. `git diff --check` passes from exact P12 base to candidate.

## Review findings

- WI-A allocates one unpublished `WorldId` before genesis, passes that same object into `SimulationRuntime` and `SimulationBootstrapComposition`, and the composition enforces the exact runtime identity handoff. The previously reviewed typed P18 profile preserves that same identity requirement; the legacy string profile remains compatible for identity-less standalone fixtures.
- `TesteSimulacao` keeps public composition access behind the volatile publication gate. Genesis callbacks, including the final publish-stage callback, cannot observe the draft. On success it checks the composition-entry and selected Unity Start threads, installs the composition while the gate remains closed, disposes the P12 bootstrap-publication scope, assesses the selected census, then clears the unpublished identity and opens publication. Failure clears the draft/identity/public composition, leaves publication closed, faults active P12 admission, and latches bootstrap against retry.
- P12 `runtime.economy.money-transfer` remains registered in the selected operation inventory. Its entry path still resolves exact rostered source/destination MoneyAccount sections, checks unchanged baselines before mutation, and enters one named operation around debit, destination credit, any successful source compensation, and result selection. Existing owner hooks remain the commit notification authority. The WI-A bootstrap work neither replaces nor bypasses these hooks.
- The combined tests retain exact `WorldId` identity assertions and P12 transfer tests for active-operation admission across commits, zero-value no-op behavior, compensation, stale/replaced owners, and wrong-thread rejection. The prior `313095...` revalidation predates the P12 transfer integration; this review independently covers the newer base and does not rely on that record as evidence for transfer compatibility.

No composition conflict or P12 transfer regression was found in the exact code diff. The earlier P12/WI-A revalidation note was inspected in the candidate worktree but was untracked at review time, so this record intentionally cites no commit SHA for it.

## Retained exact-tip artifact

The candidate worktree retains the official Smoke result pair under `Library/ValidationResults/P12WIAMoneyTransferRevalidation`. The XML reports Passed, 5 total, 5 passed, and 0 failed; its hash and matching log hash agree with the inspected revalidation note:

- XML `EditMode-20261002-153118-f8d43dad394b4c25b93c9ac395ffda6a.xml`, SHA-256 `418F0E18C9302F4C16734A673BCAD3349B2E7317C106953998A0B5E2CCFA9DC9`.
- Log `EditMode-20261002-153118-f8d43dad394b4c25b93c9ac395ffda6a.log`, SHA-256 `6F04D774C270FFD39FE2A8DFCF7267F9625846EC3DB576DE58A08020FEF19108`.

The focused Unity XML/log pairs and ALL EditMode pair from the combined worktree were not retained. This review did not rerun Unity or claim those suites passed on the combined tree. The source/test review is read-only and applies to the exact candidate SHA/tree above.

## Limits

This revalidation does not make the partial P12 mutation epoch global, establish full owner/operation coverage or runtime-wide quiescence, grant capture eligibility, provide export/hydration, complete P12-B, make P12-A ready, unblock P13, or close Phase 12. FR-B still requires its own coherent Faction/Person read-cut proof. The candidate is not promoted; canonical promotion remains a separate human gate.
