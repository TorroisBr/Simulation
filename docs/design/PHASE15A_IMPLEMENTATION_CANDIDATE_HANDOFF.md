# P15-A implementation candidate handoff

**Checkpoint:** P15-A, one inert runtime structure at an existing canonical Location.
**Canonical base:** `a6572ab3d4330d81edb334ae8b4c84ca5e6b173e`.
**Initial code commit:** `c42359f3911da612fc6ad81b0733b6bd13508641`.
**Initial code tree:** `447e4ea4d10df5e6c984ed825f463b15518e5be1`.
**Current code commit:** `c99541b0292b79c8a89540dafa82348bda99de91`.
**Current code tree:** `94114a1c4804ddc2f7cc84eff1cbeb876bd4900f`.
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

## Review remediation and revalidation on the current code tree

The independent review of the initial candidate identified two required fixes.
The public StructureStore.TryCreateStructure mutation seam is now internal;
the public P15-A runtime/composition entry point supplies the actual current
boundary, completed-publication fact, and the bounded causal order. The
runtime spatial invariant report now includes StructureStore validation at
CurrentDay. Focused tests prove there is no public store mutation surface,
the supported pre-boundary entry point leaves the empty owner unchanged, and
malformed Location/future-boundary records are surfaced by runtime validation.

The current code commit is c99541b0292b79c8a89540dafa82348bda99de91
(tree 94114a1c4804ddc2f7cc84eff1cbeb876bd4900f). All results below were
generated after these fixes against that exact code tree. XML and logs are
retained under Library/ValidationResults/P15A-review-fix/.

| Suite | Result | XML | SHA-256 | Log | SHA-256 |
|---|---:|---|---|---|---|
| RuntimeStructureTruthTests | 10/10 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-010946-2c07663ef26c4abd89a81671941e7a96.xml | B9DE293D0D70BB447556C6B3C391C8250D5D8D3B5550CCC86932B6647C981C83 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-010946-2c07663ef26c4abd89a81671941e7a96.log | 39FEC2F3AE4520CB9CB20BF765648EA0121B4E545FA6375A0D337A67B9C9BEAD |
| SimulationBootstrapCompositionTests | 21/21 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011024-a7e973e65f0e4f179efaee3b8e52c185.xml | 3DB0B2BA202322B86D5649D86E8DE7EC0547EEA11F8F1AB2EF3E1035CEE9B120 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011024-a7e973e65f0e4f179efaee3b8e52c185.log | 54F6507C639DB055C4C804C0F015727F3ED4A268996D502AA00DE4A6D40B49EE |
| SimulationRuntimeOrchestrationTests | 12/12 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011047-311659e0a2584cc8bf217ddc2202a57f.xml | 4723AF16961808336BCD0DA8C449187420DF9C7134D23D5BF214657DF58FA366 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011047-311659e0a2584cc8bf217ddc2202a57f.log | CF1D223FB98C638E27241A95C0AE1CA7A128D3642C33B63172EDD2B84F6DE5A4 |
| ALL EditMode | 2251/2251 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011108-fac2663e50134309911a7f64bb9f2d17.xml | 161CD7C3240E553A22D180E32431D21929A358BBB1068F8F55FDAE0E3481DE7E | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011108-fac2663e50134309911a7f64bb9f2d17.log | 947A94F690EA72BBD00E63D8C7A888506B18D8DFE38D8205104983FC7B51B6A8 |
| Official EditMode Smoke | 5/5 | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011206-b97d724d380349a7a4ab0dc44ba36615.xml | 0343C5952CCEBC0E5254106D336D89CCC246E9B87AAB53CC3E9531A134BB0F0C | Library/ValidationResults/P15A-review-fix/EditMode-20261004-011206-b97d724d380349a7a4ab0dc44ba36615.log | 3B9F8662A75494A405088F07BEC86DD31C980BB9BFB31DDE77AD8DE55FBE49F3 |

git diff --check passed for the candidate code commit. The original
validation table below is retained as historical evidence for its initial
code tree and is not used to validate the current candidate.
## Refreshed architecture compatibility audit

The current architecture branch was refreshed to f6924e63d8e5731da1d33021d0361e7defe6dad7.
Its current P15-A checkpoint is docs/design/PHASE15A_RUNTIME_STRUCTURE_CHECKPOINT.md.
The independent reviewer confirmed the P15-A checkpoint text is unchanged by
this architecture update; the new P10/P14/P20 product-direction clarification
does not alter P15-A scope or prerequisites.

The checkpoint's deterministic result/receipt requirement is represented by
the committed StructureRecord keyed by the supplied StructureId. It retains
the fixed proving definition, requested LocationId, actual runtime
CurrentDay boundary, and causal order; the operation returns deterministic
success/failure. Order zero is the causal order for this bounded profile,
which permits one creation total and adds no global allocator or ordering
semantics. No second receipt owner is introduced. The P12 daily profile
remains fail-closed and the synthetic proving fixture is not generalized into
a building catalog or player-choice surface. The review found no code or
design mismatch, and no code tree changed, so the current-tree validation
table above remains valid.

Candidate-to-review-repository path: codex/phase15/P15ARuntimeStructureCurrentBaseIntegration.
Exact reviewed candidate docs tip: f19a42d3c830a473b0dac8fa4d468a68bfc1b3fd.
Exact implementation commit/tree: c99541b0292b79c8a89540dafa82348bda99de91 /
94114a1c4804ddc2f7cc84eff1cbeb876bd4900f.
## Original validation on the initial code tree

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
