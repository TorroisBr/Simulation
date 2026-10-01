# P12-B NPC trade owner-commit candidate

**Status:** Independently reviewed and promoted to P12 canonical at
`522cf9158d9f650675eccfb6bcec4144dbaa32e2`; P12-B remains incomplete.

## Candidate identity

- Canonical base: `codex/phase12/canonical` at `f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea`.
- Candidate branch: `codex/phase12/P12BPostMoneyAccountBlockerRefresh`.
- Code commit: `5ab42a9880b866f9f8d94b9365b2c5fb51c15ff7`.
- Code tree: `6f8bab112ef0f7c97bfde31aa3dbd637db4f8a33`.
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
continuation notification can claim coverage, and the bound service returns
`TransactionCommitFailed` before any trade owner commits. A standalone service
with no P12 runtime bound retains the existing domain transaction path.

The operation scope remains active across all existing
`TryExecuteNpcTrade` commit and compensation branches. Each successful local
MoneyAccount debit/credit or Inventory remove/add is followed by a notification
for that changed section only when its local revision advances. Successful
account compensation commits are notified individually. No-op and failed
owner calls do not advance the epoch. The scope is disposed in `finally`.
If continuation bookkeeping faults after the first owner commit, the existing
domain result semantics are preserved and P12 assessment stays faulted closed.

## Validation

All artifacts below were produced against the code tree above and retained in
`Library/ValidationResults/P12NpcTrade/`. Each row lists the exact XML and log
SHA-256 values.

| Gate | Result | Artifact basename (XML and log) | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|---|
| `SimulationRuntimeAdmissionTests` | 15/15 | `EditMode-20261001-204828-090c3ceb89824562a917470ce4c0982b` | `1F202EA3D5E18D1E388DBC1A67257CD9A9351E51463F1328B43A0F0163A262DB` | `465475A9FF186DA4CEEF33CF012E862810F67D1C7DB602E29E4216CB642CCA60` |
| `NpcMoneyAccountCensusTests` | 10/10 | `EditMode-20261001-204906-076e41dfe07a4268add33850baca5a93` | `38E719F321BB707A717F571F01FEA7CEDF792FC08C1EA35FD7F076918CBFC525` | `6C6E7C8E191B41AA035DF90DFD8808E41387EDE8FDF1F72BC63EEB62AF5F2B7B` |
| `NpcInventoryCensusTests` | 7/7 | `EditMode-20261001-204924-f755ffdb2bb94d6295dc93d4d1e1f4bc` | `D6B0FFBD63125A03D1331D6FEAF6AF9638610E91F1CB4475E29EA3F6C3E7B2D7` | `58C2D0B8C195A2118589A7B820F3991E01EE11D21B77AC2DB15E8389B801929B` |
| `ContinuationCensusProtocolTests` | 22/22 | `EditMode-20261001-204941-cd915a2f7a784fba894a09dcee2ddf6a` | `BB22BCCF4EEB5EA522E9D72D66BED6A602122658C2D2DF2F86FDB68A7095C951` | `4F9232B9579CB68555613D20437EFD55915498D19FB472BE4B7FC3C2FE7CC5C7` |
| `EconomyTransactionTests` | 45/45 | `EditMode-20261001-204957-e60042ca2a28479fafbd69fd4fc79bcc` | `D641B29675A29779679F6909DFED87D057584C82A25A7234512E73CB913418AE` | `4C1EEC8E5679257CDCE6D5605AC4F8237289174B62AE9F01161A7FC7D7EE15C0` |
| ALL EditMode | 2125/2125 | `EditMode-20261001-205018-ce278055ba38437285b97308d263497d` | `B5F442429E3462159124C5933025A0AC1FC190CD2364DCAD34A7C6E456C1AE1C` | `8411C4FACC3ACCC4CA9313B3714C9982B5AE2BBD7DB86CF6E98D0828210E0CD8` |
| Official EditMode Smoke (`-TestFilter Smoke`) | 5/5 | `EditMode-20261001-205053-da267083897849cd80fa1f15ec00b126` | `8EFBBA8A61A09B3465C709263F494A6E7175CC805E0649FEDB946F9E7F210254` | `D92F62FE87438D10951A3F6F00FB8B05F205BA4F7E30ABB65F408A6565E72EF8` |

`git diff --check` passed on the candidate. The census regression suite covers
roster add/remove and same-ID re-registration, owner replacement and alias
rejection, and successful debit/credit revision changes. The adapter tests
cover successful trade commits, both compensation paths, unregistered
participant and wrong-thread rejection, stale preexisting owner drift
rejection, and postcommit bookkeeping failure without changing the committed
domain result.

An earlier exploratory run `EditMode-20261001-195504-4594892c7e354fa7990894575710fc41`
included an uncommitted test that expected a same-NPC trade to succeed. The
existing domain method intentionally returns `SameAccount` for that input, so
the test was removed before the code candidate commit; this run is excluded
from candidate evidence. The retained final suite above is on the recorded
code tree.

The candidate branch preserves the preceding post-MoneyAccount audit, design,
and independent design-review documents. Its full diff from canonical
therefore includes five implementation/test files and five documentation
files; the implementation delta is the bounded operation described here.

The first code review at candidate tip `5d42cef6a9ceec4eb0af49689e1f67c07d571eaf`
returned NEEDS_CHANGES because a failed precommit adapter admission disabled
tracking but still allowed the trade to write. Commit
`5ab42a9880b866f9f8d94b9365b2c5fb51c15ff7` corrects that gap: failed bound
P12 admission returns `TransactionCommitFailed` before any owner commit, while
postcommit bookkeeping faults continue to preserve the domain result. Independent exact-tip code re-review passed against candidate tip
`49b32c548e4b8c666657c031105401246cff2563`. The durable record is
`docs/design/PHASE12_P12B_NPC_TRADE_INVALIDATION_REVIEW.md`, committed at
`0279c8e40b5c51bdf5cd6dd03247832766a726de`. The reviewed code commit/tree and
validation evidence above are unchanged. Canonical promotion remains pending.

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

## Canonical promotion

With approval, `codex/phase12/canonical` was fast-forwarded from
`f913dd088f71b74cfab7c1bd8a1a79b7ce9a29ea` to candidate tip
`522cf9158d9f650675eccfb6bcec4144dbaa32e2`. The reviewed code candidate is
`49b32c548e4b8c666657c031105401246cff2563`; its exact-tip independent review
passed and is durably recorded at
`0279c8e40b5c51bdf5cd6dd03247832766a726de`.

The promotion advances only the bounded NPC-to-NPC trade invalidation slice.
P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and P13 remains
blocked. It establishes no complete owner inventory, shared-epoch coverage,
capture eligibility, export, or hydration.
