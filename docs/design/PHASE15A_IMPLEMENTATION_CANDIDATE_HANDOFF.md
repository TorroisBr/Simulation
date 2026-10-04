# P15-A implementation candidate handoff

**Checkpoint:** P15-A, one inert runtime structure at an existing canonical Location.
**Canonical base:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
**Code commit:** `c42359f3911da612fc6ad81b0733b6bd13508641`.
**Code tree:** `447e4ea4d10df5e6c984ed825f463b15518e5be1`.
**Branch:** `codex/phase15/P15ARuntimeStructureCurrentBaseIntegration`.

## Delivered boundary

The existing StructureStore owner is composed only through an explicit
`P15A-ProvingStructure` SimulationRuntime composition. Its one inert
`P15AProvingStructureComposition` fixture requires an existing valid P8
Location/anchor, completes initial publication, and permits one structure
creation after the first simulated boundary. The stable ID, fixed proving
definition/revision, LocationId, creation boundary/order and owner revision
remain in StructureStore. It introduces no material debit, settlement,
City, population, production, generic construction, duration, mod, or
save/replay behavior.

`UnityBootstrap-Daily-v1` remains unchanged and excludes the new owner. The
runtime constructor rejects any StructureStore or P15-A composition when a
P12 daily admission context is present. The test verifies the ordinary daily
runtime and TesteSimulacao surface do not admit this owner, and that a
populated unsupported proving store is rejected before daily publication.
The separate proving profile composes the owner and exercises its first
post-publication creation.

## Validation on the exact code tree

| Suite | Result | XML | SHA-256 | Log | SHA-256 |
|---|---:|---|---|---|---|
| RuntimeStructureTruthTests | 8/8 | `Library/ValidationResults/P15A/EditMode-20261004-004307-c5a5ebb275a4460ea5b74ba52fcc0036.xml` | `2277FA05805FA427792F4EB7E474CA14DEC6878BF23CA4487A205A92C8B7561A` | `Library/ValidationResults/P15A/EditMode-20261004-004307-c5a5ebb275a4460ea5b74ba52fcc0036.log` | `44342EB88EB269251B40BBDB2D54B8B123E08559396ADB1F05468B45689C62EF` |
| SimulationBootstrapCompositionTests | 21/21 | `Library/ValidationResults/P15A/EditMode-20261004-004325-da4716d908694129be9b6d6560815a25.xml` | `C2A5AFD17E367DB4453E2C20AC32CE3C40C81BEA356042396BE0B4E014DC0A5B` | `Library/ValidationResults/P15A/EditMode-20261004-004325-da4716d908694129be9b6d6560815a25.log` | `F06804BC837D536ED0EEDC311F184FA2BEFE66DE3E369059B3F81583E8A89CCA` |
| SimulationRuntimeOrchestrationTests | 12/12 | `Library/ValidationResults/P15A/EditMode-20261004-004342-69ab14e0240b4557975300106daa72f7.xml` | `ACD5EB33F4C62EE88315B4A380503541ABD4311D3DAEBF7EF26FE4E8FE518226` | `Library/ValidationResults/P15A/EditMode-20261004-004342-69ab14e0240b4557975300106daa72f7.log` | `B6DBD4BD5ED4924BB5EA01D68A15736B0541F838F598FA6B924C905F33D836F8` |
| ALL EditMode | 2249/2249 | `Library/ValidationResults/P15A/EditMode-20261004-004144-92c60ca7c110433eb50263a206edb664.xml` | `2F720318B0DB90D61F6F1743A30AC1100D2244143A454F6676A18C631CDF33F4` | `Library/ValidationResults/P15A/EditMode-20261004-004144-92c60ca7c110433eb50263a206edb664.log` | `DC672B7E702163CA6EF0C93526220CA229A22D3E86DDEE62CAD94A3F5E40F444` |
| Official EditMode Smoke | 5/5 | `Library/ValidationResults/P15A/EditMode-20261004-004401-678d57a6e58a4c8d9ecaf46d2dba927c.xml` | `FEDDF5FA5B8ACCE9A9B47F5D1C9980EC4B5A86BB0C6D77A807740DA6DA8D0213` | `Library/ValidationResults/P15A/EditMode-20261004-004401-678d57a6e58a4c8d9ecaf46d2dba927c.log` | `5BAACD8AA71949E3D273A9AF23DFECA14E3DD2C949A8612FEF36A3C90E9FA2EE` |

The official Smoke is the repository's `EditMode -TestFilter Smoke` suite;
it includes Editor tests that enter PlayMode internally. A separate direct
PlayMode filter selected zero tests and is not counted as validation evidence.
`git diff --check a6572ab..c42359f` passed.

## Remaining gates and limits

Exact-tip independent implementation review and final canonical preflight
remain required. This candidate does not close Phase 15, alter P12's accepted
daily profile, or claim P12 continuation/capture readiness. No unrelated
ProjectSettings edits or pre-existing ArmedForce `.meta` files are part of
the candidate.
