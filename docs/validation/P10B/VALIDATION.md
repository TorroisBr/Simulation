# P10-B candidate validation

**Candidate base:** P12 canonical `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
**Candidate branch:** `codex/phase10/P10BGeneratedLocalTopology-P12`.
**Validation date:** 2026-10-04. Unity runs were sequential in the isolated P10-B worktree.

## Gates

| Suite | Result |
|---|---:|
| `P10BGeneratedRuinGenesisTests` (including six publication failpoints) | 8/8 |
| `P10RuinLocalTopologyGenesisTests` | 6/6 |
| `RuinLocalTopologyGenerationTests` | 4/4 |
| `LocalTopology` | 97/97 |
| `SpatialAuthorityTests` | 8/8 |
| `SimulationBootstrapCompositionTests` | 21/21 |
| `SimulationRuntimeAdmissionTests` | 31/31 |
| `ExplorableSiteFoundationTests` | 17/17 |
| `LocalTopologyPublicationAtomicityTests` | 13/13 |
| `RuntimeIdentityCensusTests` | 6/6 |
| `SpatialNetworkCensusTests` | 7/7 |
| `ExplorableSiteCensusTests` | 5/5 |
| All EditMode | 2259/2259 |
| Official Smoke (`-TestFilter Smoke`) | 5/5 |

The first All EditMode attempt reported 2258/2259 because the authored sample now
selects the P10-A site profile while `ExplorableSiteCensusTests` still expected
an empty site owner. The fixture now asserts its actual single-site census
(cardinality 1, revision 1); the focused suite and full rerun pass.

## Retained raw evidence

`P10B-validation-20261004.zip` contains the exact full EditMode and Smoke XML/log
files. Archive SHA-256:

`71E7CD541F30C4941BF3FBD6522E86A2735E1272A4D1E125AB6BE5C3B806898E`

The archive was inspected after creation and contains all four raw files.
Individual raw-file SHA-256 values:

| Result | File | SHA-256 |
|---|---|---|
| All EditMode | `EditMode-20261004-035333-69e2bf5594b345658433a6b2e86dd943.xml` | `81801C118817AAD39EF67B7D46399BF55CCFC645075EB4DB76356EC0D4AE456A` |
| All EditMode | `EditMode-20261004-035333-69e2bf5594b345658433a6b2e86dd943.log` | `C58757BD7072B3329E5050C0A6252F56BB0BF57955B458814D348205A214C2BE` |
| Smoke | `EditMode-20261004-035429-845d4cdbc27f4456929ad9f5c09db4c4.xml` | `FEB6BDAA85966214E525E2AB7F5DCC773FACD434DF88DFB938634A194266686A` |
| Smoke | `EditMode-20261004-035429-845d4cdbc27f4456929ad9f5c09db4c4.log` | `92998EA4FD00402B844AB431961A31D1ED7437FE1615B53236252ABFF9934CF5` |

Focused run summaries are recorded above; the Unity harness wrote those smaller
focused-run artifacts to its temporary results location. The full EditMode and
Smoke raw evidence is retained in the committed archive.

`git diff --check` passed after cleanup. These results validate the candidate;
they do not constitute independent exact-tip review or canonical promotion.
