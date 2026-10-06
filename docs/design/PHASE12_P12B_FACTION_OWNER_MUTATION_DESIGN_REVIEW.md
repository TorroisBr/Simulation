# P12-B Faction owner admission and mutation epoch design review

**Result:** PASS
**Reviewed design commit:** `f4c1218a8f9969f186fe32076d1470a8bcfbd246`
**Reviewed base:** `54e95a325812baa8db2fe233d3dd566be93f7fa2`
**Review scope:** `docs/design/PHASE12_P12B_FACTION_OWNER_MUTATION_DESIGN.md`

The design is bounded and implementation-ready against the stated canonical
base. The selected Daily-v1 runtime owns the exact `FactionStore` used by the
FR-B factual read surface, starts with zero faction and affiliation rows, and
supports the three listed post-publication commit paths through
`SimulationRuntime` facades. Each successful store commit advances the shared
local revision once.

The two Required sections correctly preserve separate row cardinalities while
binding the same exact owner identity and revision. Notifying both sections on
each successful commit is necessary to refresh both revision witnesses, and
the existing mutation protocol can provide one shared epoch advance. The
proposed preflight, failure, and post-commit fault behavior follows existing
P12 owner adapters and retains existing domain semantics.

The design does not add a gameplay API or policy and does not claim complete
P12-B owner/writer coverage, complete shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, P13
readiness, or Phase 12 closure. The 253-to-255 section change is correctly
limited to this partial owner witness.

No unresolved product or canonical architecture decision was found.
