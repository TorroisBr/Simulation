# P12-D City root snapshot design review

**Review ID:** `P12D-CITY-ROOT-SNAPSHOT-DESIGN-1F47F72-R1`
**Outcome:** `NEEDS_CHANGES`
**Implementation readiness:** `NOT READY FOR IMPLEMENTATION`

## Exact revisions reviewed

- Candidate branch: `codex/phase12/P12DCityRootSnapshotDesign`
- Exact candidate: `1f47f724e6245e69b938cc0aedaf11b10e78c1c2`
- Candidate tree: `3436130ab0dec3df3a79fd80265e5ef7942ec403`
- Candidate proposal blob: `0db56b85aa636cb3d09cc918cbef2e7eaa3b94cf`
- Exact P12 base and canonical at review: `da2a73896bc405ae6f11c536a5fbe8d471b00c21` (tree `ebdb529873d1826320b04224f1a9e79efc5c22db`)
- Architecture branch tip checked remotely: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`
- Candidate parent is the stated P12 base. The candidate diff adds only the proposal document; no executable files changed.

Reviewed the candidate proposal against the current-base P12-D technical design (`6cde463b1ca7f4a5c8033fe3538360ca5d80e189`), its exact design review (`4c1501474a774eb8d7f737a6074b3922dbdfec0e`), P12 State (`35205ed70f3d0bd4c0a3bda52c92d6b570ead395`), the P12 Brief, the current architecture, and these owner sources at the exact P12 base: `CityRuntime.cs` (`a8165678d7a3d8922e1880b39830efb1a6711e26`), `SettlementPopulationRuntime.cs` (`024ffc79b5ece19a2db7d3811ec53cb8b75026e8`), and `CityNpcPresenceCensusProvider.cs` (`de903befd8faa8126aa4633049610077c09266cc`). The P12-D high-level review authorizes only the GenealogyStore slice; this review does not enlarge that authorization.

## Findings

The proposal is well bounded in several important respects. Its City, Market, and SettlementPopulation section IDs, schemas, owner identities, cardinalities, and local revisions match the installed P12-B providers. It preserves `ImportantNpcs` order and `ImportantNpcRevision`, the distinct aggregate and receipt revisions for population, exact market rows and stored prices, and the population receipt set. It rejects the selected-profile-incompatible account-backed City modes and references, P14 material-flow state, and unsupported P18 continuation state. The rollback/replay semantics are distinguished correctly, and the proposed factories avoid using gameplay operations, repricing, genesis, or rollback APIs to reconstruct facts. The test outline covers the key exact-value and rejection behaviors. No product-semantic conflict was found in those bounded choices.

Two design obligations remain unresolved and block implementation readiness:

1. **There is no current proof that the excluded City P18 daily-economy receipt owner is empty.** At the reviewed base, `CityRuntime` keeps `dailyEconomyReceipts` and `dailyEconomyReceiptRevision` private. `TryCommitDailyEconomy` installs a receipt and advances that revision, but there is no owner-local read-only API returning their exact empty-state facts together, nor a P12-B census section for this City receipt owner. The proposal correctly says a proof seam is needed and asks this review to decide it, so this is not yet a complete implementation contract. Specify the narrowest owner-local, non-mutating empty-state proof that reads both facts consistently on the serialized owner thread; keep the P18 receipt values excluded and do not invent a P12-B witness, City aggregate revision, or competing capture lock. Then test populated and advanced/otherwise inconsistent states fail closed.

2. **The City/NPC reference cycle has no resolved one-construction stage order.** `CityRuntime.ImportantNpcs` stores concrete `NpcRuntime` references, while each NPC's `CurrentCity` stores the reciprocal concrete City reference; the installed City presence census validates both directions. The high-level D design's listed order restores City owners before NPC owners, but the City snapshot's exact ordered membership cannot be installed until the corresponding staged NPC references exist. The proposal identifies this cycle and explicitly declines to choose a placeholder/fixup or alternate order. Define a staged construction strategy that constructs each final City and NPC exactly once, preserves ordered membership and reciprocal reference validation, and does not publish or hydrate a partial City twice. State precisely which shared D/NPC component owns that strategy and which focused tests prove it. If the strategy requires changing the reviewed D design's sequencing, update and independently review that design before implementation.

One interface boundary also needs to be kept explicit when the proposal is amended: the City owner capture must consume the one already validated `DailyCaptureEligibilityToken` and shared capture stamp supplied by the D/E/F capture orchestration. It must not independently acquire a token or add another lock. The proposal's file-ownership list excludes `SimulationRuntime`, so specify this as an input/ownership boundary for the later shared adapter rather than assigning runtime admission or whole-profile publication to this City slice.

The current Daily-v1 absence of City accounts is a valid bounded fail-closed precondition for this slice, provided the exact selected-profile contract remains enforced and no NPC account witness is treated as City coverage. This does not change the broader P12-D owner contract or claim support for account-backed Cities.

## Readiness, validation, and next action

The candidate is **not ready for implementation**. The receipt-empty evidence seam and the single-construction City/NPC stage sequence must be made concrete in the proposal and independently reviewed. This is a technical-design gap under the existing P12-D boundary, not a newly discovered product decision. P12-D stays open; this result does not make P12-A ready or imply Phase 12 closure.

No Unity tests were run because this candidate is documentation-only and its contract is not implementation-ready. `git diff --check` passed for this review record. No code or candidate proposal was modified by the reviewer.
