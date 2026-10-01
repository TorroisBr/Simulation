# P12-B per-NPC MoneyAccount census witness — technical design

**Work package:** accepted P12-B owner/cardinality census; no new checkpoint ID or semantic scope.
**Design branch:** `codex/phase12/P12BNpcMoneyAccountCensusDesign`.
**Canonical base:** `43dba1b5d16cba1558c4c39239f3cd0d4c669958` (after the Market census promotion and State refresh).
**Status:** proposed for independent technical review; no implementation authorization is created by this record.

## Evidence and bounded objective

The refreshed P12 owner/operation inventory lists direct `MoneyAccountRuntime.TryCredit` and `TryDebit` writes and identifies MoneyAccount's local revision as owner-local evidence, not a shared epoch. The Market stock-row witness promoted at `533c1e54f362218f222bf567dc1cacb8fdf68600` explicitly excludes MoneyAccount owners. The selected authored profile has ten NPC rows, and each constructed `NpcRuntime` receives its own account.

`NpcRuntime.MoneyAccount` returns the existing serialized field without lazy construction (`NpcRuntime.cs`); the constructor installs a `MoneyAccountRuntime`. `MoneyAccountRuntime` exposes `Revision`, incremented by successful positive credit/debit writes and prepared account installs. Failed operations and zero-amount no-ops do not advance it. These facts support a passive account-owner witness without changing account behavior.

Add a roster-following census family for NPC-owned accounts only. The family proves one exact account owner per currently registered NPC. It does not enumerate City population-economy or Market-counterparty accounts, and it does not claim complete economy-owner coverage.

## Witness contract

- Register one schema-v1 section for each exact NPC in the installed runtime roster. Use the collision-free prefix `p12e.npc-money-account/` followed by the stable `NpcRuntime.RuntimeId`; this is distinct from `p12f.inventory/` and `p12e.city-market-stock-rows/`.
- Bind the witness to the exact object returned by `NpcRuntime.MoneyAccount`; report cardinality `1` for a present owner, including a zero-balance account, and report only `MoneyAccountRuntime.Revision`.
- Do not include balance or other account payload in the witness. Do not replace or materialize an account while reading it.
- Require a valid unique NPC RuntimeId, a non-null account, and a distinct account object per NPC. A missing or aliased owner is an incomplete/invalid census section and must fail closed.
- Order providers ordinally by NPC RuntimeId and return an immutable provider view, consistent with the existing Inventory family.
- If an account reference changes while its NPC remains registered, retain the last published family and fault/reject reconciliation; do not silently adopt a replacement. Removing an NPC and later registering a new NPC with the same RuntimeId is a new membership boundary and may publish its new account owner.

The witness is passive and unsynchronized. Revision changes establish only that this account owner recorded a local mutation; they do not prove a shared mutation epoch, runtime operation boundary, or capture eligibility. The family is registered in `ContinuationCensusProtocol`, but this slice adds no mutation notification for account writers. Therefore a successful account revision advance without an existing supported notification must make the next census assessment or membership reconciliation fail closed and latch the census protocol fault. This is deliberate evidence that account-write invalidation is still missing; it must not disable or alter ordinary gameplay operations, and it does not establish capture eligibility.

## Smallest integration surface

- Add `NpcMoneyAccountCensusProviders.cs` and its Unity metadata.
- Add the family beside the existing per-NPC Inventory family in `ContinuationCensusProtocol` registration, validation, owner-identity tracking, and staged roster reconciliation. Publish the family only after the full membership reconciliation succeeds. Do not add account-write notifications or broaden the shared mutation-epoch surface in this package.
- Expose the reconciled family through `SimulationRuntime` and the existing bootstrap composition boundary used by the census providers.
- Leave `NpcRuntime`, `MoneyAccountRuntime`, transaction semantics, and account writers unchanged unless exact review finds that the stated identity/revision contract is false.

Do not add mutation-epoch wiring, runtime operation scopes, balance export, hydration, City-side account providers, or P12-A/P13 readiness claims in this slice.

## Required focused evidence

1. The selected authored profile has ten ordinally ordered NPC account sections; each binds to that NPC's installed account, has cardinality `1`, and starts at revision `0`.
2. Repeated census reads are stable. A successful positive credit and debit each advance that account's revision while cardinality stays `1`; a zero-amount no-op and a failed credit/debit preserve revision.
3. Adding/removing an NPC updates only its account section within the same atomic membership reconciliation as the already registered families. Removing and later registering a new NPC with the same RuntimeId publishes the new account identity.
4. Null accounts, duplicate RuntimeIds, aliased account objects, or an unexpected in-place account replacement fail closed without partially publishing the account family or disturbing other families.
5. Census reads do not materialize missing accounts. Tests must assert only identity, cardinality, and revision; they must not expose balances through the witness.
6. A positive account write without a supported account-section notification causes the next census assessment to reject/fault rather than accept a stale baseline. A subsequent roster reconciliation also rejects if it encounters that unnotified revision. Zero/no-op and failed writes leave the protocol assessment valid because the owner revision did not advance.

Run focused census and roster-reconciliation tests, the selected-profile bootstrap test, the required P12 EditMode/Smoke regressions, and `git diff --check` on the exact implementation tip. Keep test XML/log hashes with the candidate record.

## Revalidation and limits

The current P12-B blockers remain separate: complete effective-profile owner/cardinality coverage, supported-writer shared-epoch coverage, global owner-thread/quiescence proof, capture eligibility, and export/staged hydration. This design supplies only a missing per-NPC account witness. It does not make P12-B complete, P12-A READY, P13 READY, or any phase closed. The exact candidate code and complete tests require independent review before implementation status can be derived as READY.