# CORE-F selected Merchant fail-closed review — d6ca2001

## Exact identity

- Repository: `TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-runtime-boundary-3ec0aa`.
- Reviewed code commit: `d6ca200163adcaf77a6d69bb4609234efd690df3`.
- Reviewed code tree: `eeff27d5acb43b4cc7650a30ac804200fce6705a`.
- Exact parent/base: `1405d7863f868c5851a2bbe17bebfde773916d17`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate branch on origin after publication: `d6ca200163adcaf77a6d69bb4609234efd690df3`.
- Independent exact-tip code review: `PASS`.
- Reviewer: independent read-only review agent, not the implementation author.

This record describes the independent review of the code commit above. It
does not modify that reviewed code tree and does not approve Core canonical
promotion.

## Scope reviewed

The review addressed the selected Daily-v1 Merchant findings on the prior
candidate. A selected BuyGoods/SellGoods action whose Unity projection no
longer matches the exact Core action slot now fails closed before invoking the
generic provider. With P12 admission active, the runtime faults admission
before throwing. The regression verifies that money, account and inventory
revisions, inventory quantities, Market stock and Market revision remain
unchanged.

A planned NPC sale that reports a partial trade commit now returns that result
without starting a second sale against the Market or advancing the Merchant
plan. Existing committed trade mutations are preserved. After the trade scope
has disposed, the P12 admission path is faulted for the partial commit. Tests
cover the retained account/seller-inventory writes, unchanged buyer inventory
and Market, no Market boundary entry, unchanged plan revision, and P12 fault.

The review also checked the adjacent Unity API/test updates required by the
current Core owner interfaces. It found no remaining actionable finding in
this bounded change.

## Validation evidence and limits

Reported validation on the reviewed source/test tree:

- .NET focused `DailyV1SelectedNpcTradeExecutionTests`: `16/16 PASS`.
- .NET `Simulation.Core.Tests` cold: `501/501 PASS`.
- .NET `Simulation.Core.Tests` warm: `501/501 PASS`.
- Unity focused selected Merchant projection mismatch: `1/1 PASS`.
- Unity `SimulationRuntimeAdmissionTests`: `210/210 PASS`.
- Unity `SimulationBootstrapCompositionTests`: `38/38 PASS`.
- `git diff --check`: `PASS`.

The independent reviewer inspected the exact commit diff but did not rerun the
test suites. Full Unity EditMode, official Smoke, LongRun, complete CORE-F
application/host validation, detached P12 restoration, and separate-process
T/T+1/T+2 parity are not established by this checkpoint.

## Verdict and integration limits

The bounded selected Merchant fail-closed and partial-commit corrections pass
independent exact-tip code review. This is candidate progress only. It does
not establish completion of the approved CORE-F package, authorize canonical
promotion, or claim a shared detached Daily-v1 runtime. The candidate still
requires the remaining authoritative Core execution and composition work,
application lifecycle, detached restoration, fixed .NET host, full applicable
validation, and package-level review.
