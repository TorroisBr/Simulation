# CORE-F demographic-owner architecture adoption

Architecture baseline: `a20978d2b61e07a4b131afbb3971aae346511acb`

Production Core baseline: `0094febf7f97333d0653eaf9688fe1d24d73327f`

This Core candidate imports the reviewed demographic-owner implementation
handoff and its supporting source/API designs from the architecture baseline.
It imports no executable source from the older architecture worktree. The
bounded package is ready for implementation under the active Master mandate;
this record does not claim that the owners or downstream runtime are delivered.

Imported architecture documents are byte-identical to the reviewed source
blobs:

| Document | Architecture blob |
|---|---|
| `docs/architecture/CORE_F_DEMOGRAPHIC_OWNERS_MASTER_HANDOFF.md` | `f90684edde28126f0c1325dea9aa870d0f48c161` |
| `docs/architecture/CORE_F_AGGREGATE_POPULATION_OWNER_DESIGN.md` | `9ee64e01a0ba7c92940791fce51ca220cec3beea` |
| `docs/architecture/CORE_F_GENEALOGY_OWNER_DESIGN.md` | `0e62d662fe891388de8afbf40a2e6c1cbc27c676` |
| `docs/architecture/CORE_F_CLOSURE_EXECUTION_CONTRACT.md` | `8d5bcd8bf00124570f5f48307c903b58a87417a8` |
| `docs/architecture/CORE_F_CLOSURE_PATH_REVIEW.md` | `92cb630c74618b5f315db2b4788fd655403ca856` |
| `docs/architecture/CORE_F_RUNTIME_RESTORE_INTEGRATION_DESIGN.md` | `ebc3ff88dca9dad200bf6e73585832e1c0a1bb2f` |
| `docs/architecture/SIMULATION_CORE_EXTRACTION.md` | `3c1fea815953f6e2bca1ef7557011af5166397f4` |

No source, build, test, Unity project, P12 profile, or canonical branch content
was imported from the architecture baseline.
