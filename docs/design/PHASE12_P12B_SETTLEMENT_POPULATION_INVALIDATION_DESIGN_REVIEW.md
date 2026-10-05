# P12-B SettlementPopulation invalidation design review

**Result:** PASS — `READY_FOR_IMPLEMENTATION` under previously accepted P12-B prerequisite-capability authority.

**Exact candidate:** `621cff72324345ec5ae82b20b6400524f18d60d3`  
**Candidate base:** P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`  
**Architecture reviewed:** `ffd75652d89d862b83d634868c560f8540869b89`  
**Relevant runtime references:** P16 canonical `75a27d7ac97e66c2762835ccea7950a945c2f20d`; P17 canonical `b3f26d541fb1a7f7c5c9809877b4c5b937a53aee`.

The independent exact-tip review confirmed that the design and blocker matrix agree: only the bounded SettlementPopulation invalidation sub-slice is implementation-ready. The candidate’s complete diff from P12 canonical is documentation-only. It adds no implementation code, product semantics, or checkpoint ID.

The reviewed implementation boundary covers Person/NPC life-residence witnesses and local revisions, dynamic-family reconciliation, supported lifecycle operation scopes and exact changed-section sets, fail-closed handling for keyed/receipt-bearing Person death and named birth, capacity preflight/reservation, and one post-commit notification after the complete outer operation. It preserves P12-D ordering for named birth and excludes P18 temporal demography, conflict/WorldCommand behavior, export/hydration, capture eligibility, global quiescence, and full P12-B completion.

Required implementation validation remains outstanding: focused lifecycle/provider/admission/operation tests, affected SettlementPopulation/PersonDeath/NpcResidenceMigration regressions, ALL EditMode, official Smoke, and `git diff --check`. P17/P16 runtime overlap must be serialized against the current canonical tips before implementation and revalidated after integration.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked; Phase 12 remains open. This review does not authorize canonical promotion or claim delivery.
