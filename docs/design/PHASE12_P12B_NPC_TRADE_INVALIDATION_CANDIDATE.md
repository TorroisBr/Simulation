# P12-B NPC trade owner-commit candidate

**Status:** Validated implementation candidate; independent exact-tip code review and canonical promotion remain pending.

## Candidate identity

- Canonical base: `codex/phase12/canonical` at `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea`.
- Candidate branch: `codex/phase12/P12BPostMoneyAccountBlockerRefresh`.
- Code commit: `9c75311ee4920f6b45552361cb20e6152252a1ec`.
- Code tree: `8745ee9bf03aca9cdb17308f32cbf3abafea3332`.
- Design contract and independent design review: `PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_DESIGN.md` and `PHASE12_P12B_POST_MONEY_ACCOUNT_INVALIDATION_REVIEW.md`.

## Bounded implementation

The selected UnityBootstrap-Daily-v1 runtime registers the stable
`runtime.economy.npc-trade` operation before sealing its operation inventory.
`TesteSimulacao` binds the shared `EconomyTransactionService` to that runtime
after protocol initialization and before bootstrap publication.

Before the first possible owner commit, the bridge verifies that each
participant is the exact NPC object registered under its RuntimeId and that
its installed MoneyAccount and Inventory are the exact owners published by
the matching roster census providers. It checks section identity, schema,
cardinality for the single-account section, and current local revisions, then
rechecks the four registered sections against their accepted census baselines.
An unsupported or stale boundary faults P12 admission closed before any
continuation notification can claim coverage.

The operation scope remains active across all existing
`TryExecuteNpcTrade` commit and compensation branches. Each successful local
MoneyAccount debit/credit or Inventory remove/add is followed by a notification
for that changed section only when its local revision advances. Successful
account compensation commits are notified individually. No-op and failed
owner calls do not advance the epoch. The scope is disposed in `finally`.
If continuation bookkeeping faults, the existing domain result semantics are
preserved and P12 assessment stays faulted closed.

## Validation

All artifacts below were produced against the code tree above and retained in
`Library/ValidationResults/P12NpcTrade/`. Each row lists the exact XML and log
SHA-256 values.

| Gate | Result | Artifact basename (XML and log) | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 12/12 | `EditMode-20261001-195143-3927ba745e124242b4add8463841539e` | `DCAFC690EF47E34A4096CD721A7FB8086C340CA39FD0895795B141B3167772F5` | `CFF5C849A7C163CDA9FAE52AC0941721A12AA7A5578E0EC354F32B2EBB68F197` |
| `NpcMoneyAccountCensusTests` | 10/10 | `EditMode-20261001-195156-189f527dafcc437c8ab5c75181c98d32` | `5F0862B20B2609D16E427B2E16FCC269DCF87A30050ED769D2A320F3C05CB0F6` | `7E7A1F50480816AF67685B3BA8C5406FAE5CD650F383D5D99A0F8AA1B1A732A5` |
| `NpcInventoryCensusTests` | 7/7 | `EditMode-20261001-195213-c506a6cfe9814b23b22d3c75067cad39` | `A701F40E019A267F46588D885EAF52D30A50CAAC922F5D61C3A677F665B4BBC9` | `70C8DD284CDBE389C203836FD1F3E62A46377598275CE80E5318CA58D7FF6597` |
| `ContinuationCensusProtocolTests` | 22/22 | `EditMode-20261001-195229-33f48553cf0240b099ba3e1d0adedfa8` | `69A23332D5425DCC2D7BCD3CF28A434026C513504CA833B11AEF9FA32FE2C1E5` | `49C6E309C0BD3902074E940DB9C447114D42C8FA02B1578954F7033A46CCD938` |
| `EconomyTransactionTests` | 45/45 | `EditMode-20261001-195244-ada757a37acb459ba1c627de112d87ba` | `78C0F933A41F3ABD048957C5813CC2A884EC13A6E8C6879DFA4C9530758265A8` | `C3EAB8CFFB7152B39F78F3DD8B6C2B3F23737CF8A831F332971A77BEC3169C9B` |
| ALL EditMode | 2122/2122 | `EditMode-20261001-195301-01c6e664fd634fa1a2d2d79be16b3f0f` | `C1F97F8F3E0C9A90FC9819F0FBA5DA0A8445826CD8788879FDABEB0DBC5248E2` | `90945A8195C3DFD16F7819310F4DE369E37371F5A5552C202B5F1DF650491622` |
| Official EditMode Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261001-195340-4584850dea764de1acf9fd866b631ce1` | `A8EB130A4C70FCB583C6FF0328149AAADE05E3E1D14BF1A2AA7F7DEA23594A90` | `BE69CC51F21856206BDB806700454EC3578B6DACA2348FFE89FA898F8E7D0C61` |

`git diff --check` passed on the candidate. The census regression suite covers
roster add/remove and same-ID re-registration, owner replacement and alias
rejection, and successful debit/credit revision changes. The adapter tests
cover successful trade commits, both compensation paths, and preexisting
unnotified owner drift.

## Limits

This candidate covers only the four registered per-NPC MoneyAccount and
Inventory sections used by `TryExecuteNpcTrade`. It excludes subsequent
MerchantSystem plan completion, direct owner entrypoints, every other economy
transaction, City-owned accounts, other selected-profile operation families,
and P18 keyed-sale/timeline paths. The active operation count remains
bookkeeping rather than a lock.

It does not complete P12-B, establish a complete selected-profile owner set or
shared-epoch coverage, or implement capture eligibility, export, or hydration.
P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, P13 remains blocked,
and Phase 12 remains open.
