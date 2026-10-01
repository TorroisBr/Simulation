# P12-B per-City Market stock census — implementation candidate

**Work package:** accepted P12-B owner-witness inventory; no new checkpoint ID.
**Candidate branch:** `codex/phase12/P12BMarketStockCensusImplementation`.
**Canonical base:** `7be88ca7816b14c934729bffec56178ce3eb7e5a`.
**Code-bearing commit:** `3b2a9c2807ac95c6c929fe1104fcb078f364b96f` (tree `1bb0a2b9e271d8070fe1012a0fecf7b37b0763e5`).
**Design:** `PHASE12_P12B_MARKET_STOCK_CENSUS_DESIGN.md`, independently reviewed PASS; review at `PHASE12_P12B_MARKET_STOCK_CENSUS_REVIEW.md`.
**Status:** implementation submitted; awaiting independent exact-tip review.

## Delivered boundary

Adds `CityMarketCensusProvider`, bound to each exact Market owner reached from the composed City's installed `Market` property. Stable schema-v1 section IDs encode City RuntimeId; providers are ordered by ordinal City RuntimeId and reject missing Cities, duplicate City IDs, or aliased Market owners. Each witness reports only `MarketRuntime.Items.Count` and that Market's existing local `Revision`.

`SimulationBootstrapComposition` exposes the fixed provider list. The selected-profile test verifies two installed, distinct Markets at five rows and revision zero each, stable repeated witnesses, a successful stock update that changes the local revision without changing row count, and a new stock row that changes both count and revision.

No `MarketRuntime`, `SimulationRuntime`, `ContinuationCensusProtocol`, owner-operation scope, mutation epoch, counterparty/account, City/NPC composite revision, export, or hydration changes are included. These witnesses remain passive and unsynchronized. This does not prove a complete profile inventory, shared-epoch invalidation, owner-thread/quiescence, capture eligibility, P12-B readiness, P12-A readiness, or P13 readiness. P12-B remains incomplete; P12-A stays `WAIT_DEPENDENCY`; P13 remains blocked.

## Validation on the exact code-bearing commit

All commands used `Tools/UnityValidation/Invoke-UnityValidation.ps1` and wrote results to `Library/ValidationResults/P12BMarketStockCensus/` in the candidate worktree.

| Gate | Result | XML and SHA-256 | Log and SHA-256 |
|---|---:|---|---|
| `EditMode -TestFilter CityMarketCensusTests` | 1/1 passed | `EditMode-20261001-154000-052354957747450c97022da45f71dfe7.xml` — `487788B95B0EC1CB7E3655D058FF13240078B06D192B03ABA73FB99A913ED5E6` | `EditMode-20261001-154000-052354957747450c97022da45f71dfe7.log` — `3BBB6A366CDF211A31125AB4EAE80D51A8A302D8DA9C682BB301FCA49432C9ED` |
| `EditMode -TestFilter SimulationBootstrapCompositionTests` | 14/14 passed | `EditMode-20261001-154018-380a14a2393b4986989fdf19151230b6.xml` — `71E3DF8F988183E0E9315C1A3DF5B53D137B71BA6E16F8FD0EA410E316C2D001` | `EditMode-20261001-154018-380a14a2393b4986989fdf19151230b6.log` — `6F141888EAADB8FDE16E8364D6F2363B1810D90F683CEE90BA5DF4AD33C296E9` |
| `EditMode -All` | 2108/2108 passed | `EditMode-20261001-154035-8404f27f726b4dc3a6d15fe9e66a014c.xml` — `7BB4AF319ECFE8E1BEB909D6188201FD96BB88380F70052D46CA2E8348D5B9C7` | `EditMode-20261001-154035-8404f27f726b4dc3a6d15fe9e66a014c.log` — `9D8EFCA720F95402EB30484C03D567510587F720F2201FAF172E63D4C6E49E75` |
| Official `EditMode -TestFilter Smoke` | 5/5 passed | `EditMode-20261001-154116-96bc1ea738114b82bcb032afee6b1e11.xml` — `6C0F3B2E4D4FCA3872D99805B008F11A5538A1CFAC4CEC97E0BC58FD247431BA` | `EditMode-20261001-154116-96bc1ea738114b82bcb032afee6b1e11.log` — `74379598B8BEB90C63E95DA1F7EC40D85A84CE26F461CB810D9542DF8D7B2A8B` |
| `git diff --check 7be88ca..3b2a9c2` | passed | — | — |

A PlayMode filter probe for `GMConsolePlayModeSmokeTests` reported zero tests; it is not counted as a gate. The repository's complete official Smoke gate is the five-test EditMode `Smoke` filter above.

The candidate worktree contains unrelated ProjectSettings and untracked `.meta` changes outside this committed diff; they have been left unstaged and untouched.
