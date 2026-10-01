# P12-B post-MoneyAccount invalidation design review

**Verdict:** VALIDATED_CANDIDATE — bounded design only; no implementation reviewed.

## Exact review identity

- Canonical base and current canonical at review: `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea` (`codex/phase12/canonical`).
- Exact documentation candidate reviewed: `e5c2d2a0fe6d07331570b635f72c8f3a95bdb47f` on `codex/phase12/P12BPostMoneyAccountBlockerRefresh`.
- Candidate diff is documentation-only in `docs/PHASE12_STATE.md`, `docs/design/PHASE12_POST_MONEY_ACCOUNT_BLOCKER_REFRESH.md`, and `docs/design/PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md`.
- Reviewer: independent P12 census/operation audit reviewer `/root/p12_next_gap_audit`; the author did not review their own design.
- `git diff --check` passed. No Unity tests apply to this documentation-only candidate.

## Findings

The selected-profile asset claim is supported by the repository: `SampleScene` selects `Simulation-GeneralTest.asset`; that asset references `City-CampoVerde` and `City-SerraDeFerro`; neither City asset overrides its account modes; and `CityData` defaults to Open liquidity and Free consumption. Runtime construction consequently has no non-NPC `MoneyAccountRuntime` instances in `UnityBootstrap-Daily-v1`. A future account-backed profile requires a refreshed owner inventory.

The reviewed first write slice is bounded to `EconomyTransactionService.TryExecuteNpcTrade`: two rostered NPC account owners and two rostered NPC Inventory owners, with successful account compensations reported individually while one named operation scope remains active. It does not cover MerchantSystem plan completion or other transaction methods and does not claim capture eligibility. The current `MoneyAccountRuntime.InstallPrepared` call sites are P18-D keyed sale and P18 timeline daily economy, both excluded from this P12 profile.

The reviewer marked the design `VALIDATED_CANDIDATE` and recorded one implementation detail: before the first commit, resolve both buyer and seller RuntimeIds to the registered NPC account and Inventory sections. Implement that preflight so the operation cannot partially mutate an unregistered participant before the continuation protocol faults closed. The shared `EconomyTransactionService` can be bound to the runtime's P12 operation/notification bridge after `SimulationRuntime` initializes its protocol and before bootstrap publication, as specified by the design.

## Status and limits

This is not a code review, implementation validation, canonical promotion, or Phase closure. The accepted P12-B–P12-G prerequisite scope already authorizes bounded prerequisite implementation after design review; this record does not broaden it or create a new checkpoint. P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked, and canonical promotion remains a separate human gate.
