# P12-B selected-profile TravelParty advance operation candidate

**Status:** implementation candidate pushed for independent exact-tip code
review. No canonical promotion or Phase closure is recorded here.

## Candidate identity

- Canonical base: `6b30d86c3214a98603bea809154e2dc06047d6a3`
- Current-base design revalidation: `c1d80c7af460b68a0508f8bb5eaf00905bb0ef61`
- Code candidate: `ac0bcffe4d345c81d77bfa56b19e3591a9ebb46c`
- Code tree: `14e2f4e485a83791af43b781546bd6f90b3913f5`
- Implementation branch: `codex/phase12/P12BTravelPartyAdvanceImplementationCurrent`
- Design contract: `docs/design/PHASE12_P12B_TRAVEL_PARTY_ADVANCE_OPERATION_DESIGN.md`
- Independent design review: PASS for design tip
  `c75c673be48b506c71730d12503b42bebc6f7f4c`, recorded at
  `27e4f80ca95adb3301cbb711af6415b34b246b64`.
- Current-base review confirmed that FR-C promotion changed only the factual
  reader registration outside this operation; the TravelParty design remains
  compatible and requires no revision.

## Bounded delivery

This candidate adds the selected-profile nested daily operation
`runtime.travel-party.advance` around the existing
`TravelPartySystem.AdvanceParties()` call, inside `runtime.advance-day`. It
adds an exact per-installed-NPC travel-state census witness with local
revision, binds only the required existing TravelParty, City-presence,
SpatialKnowledge and SimulationRecordSequence owners, and batches the
changed owner-section set into one notification when the nested operation
closes. Empty operations do not advance the shared mutation epoch. Legacy and
P18 execution paths do not acquire this P12 operation.

The implementation instruments successful travel-progress mutators and the
presence transitions needed to keep the exact owner witnesses truthful. It
preflights revision capacity before travel-state commits, preserves current
arrival ordering and partial progress, and faults selected-profile admission
closed if a later failure follows earlier commits. It adds no rollback or new
arrival-event failure policy.

The implementation and its focused tests are limited to the accepted design
in `PHASE12_P12B_TRAVEL_PARTY_ADVANCE_OPERATION_DESIGN.md`. It does not change
TravelParty formation/start, Expedition return, ordinary single-NPC travel
policy, P18 temporal ordering, or activity/participant ownership semantics.

## Validation on exact code tree

All XML files reported `result="Passed"`, zero failures, zero skipped tests.
Every artifact below is from code tree `14e2f4e485a83791af43b781546bd6f90b3913f5`.
XML and log artifacts are retained under
`Library/ValidationResults/P12BTravelPartyAdvanceFinal/` in the validation
worktree.

| Gate | Result | XML / SHA-256 | Log / SHA-256 |
|---|---:|---|---|
| `P12TravelPartyAdvanceTests` | 10/10 | `EditMode-20261003-022018-7aacec19240b4a21b84a7c88fe4142a9.xml` / `1440439D704822D40E6981CB2C667DB654240E924C82DD1606E72E81769EA20A` | `EditMode-20261003-022018-7aacec19240b4a21b84a7c88fe4142a9.log` / `C12B6E38B474F53C50D2A15F38CE2B02107EEA3A7CB6FA7158933E2C3645A3CD` |
| `TravelPartyCensusTests` | 10/10 | `EditMode-20261003-022028-82e4713dd9b24accb8bc14f505901ede.xml` / `2267EB12B483B639D133F4AFDF8ACBA96708EF373E879DABE1D1CF4F2647EA51` | `EditMode-20261003-022028-82e4713dd9b24accb8bc14f505901ede.log` / `5A4AE11D2E0B6964E3C6EBA9D7FAAC82F709B2D60711D15F07220C6FF4388130` |
| `GroupTravelTests` | 32/32 | `EditMode-20261003-022039-0821fe3595434bf1ae39784056c3b76f.xml` / `D6222D78BF64AF9672000D40533F05B16EAAEF8B04D0020FF4380B7534FD7716` | `EditMode-20261003-022039-0821fe3595434bf1ae39784056c3b76f.log` / `9E977031883A73E29900FBD01366FF149A395ABFD13CF7D4903B8E34463B7880` |
| `NpcOwnerCommitInvalidationTests` | 13/13 | `EditMode-20261003-022050-70e473129d594596a5255dc4acc7d4f4.xml` / `88508D3982227DDD2D923575A7E0D5438C2D6BF1E217C56BDDE5024B890D9ABB` | `EditMode-20261003-022050-70e473129d594596a5255dc4acc7d4f4.log` / `D5CEE9F3DCE4D76334499591A8941B25A37D339A3202B662690F1307BBB02A54` |
| `SpatialKnowledgeCensusTests` | 16/16 | `EditMode-20261003-022101-82641a166eb1409bb2c5845b7ee89977.xml` / `4B42847E7ED1F934E33AA93FCF9447176E7426CBD943EC5E0D35FBF12E7F3037` | `EditMode-20261003-022101-82641a166eb1409bb2c5845b7ee89977.log` / `5511ECE3D959BC0E7B1D0DD5A8E1A42C5A8B891A28E5AED2657AFA0DC3104D99` |
| `SimulationBootstrapCompositionTests` | 21/21 | `EditMode-20261003-022111-288982a577e14dc490e52631740b86c6.xml` / `D1FB899A28E6070F39F497DEBFDD58B6859AFF6BB42839E9E28106D1F3413BEF` | `EditMode-20261003-022111-288982a577e14dc490e52631740b86c6.log` / `E47FC214D717F7960AD4CDE58199DCEF7EE7D385AA3C7698141ADAA78B822116` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `EditMode-20261003-022122-f053447a912f4ed5bf518e0528d72363.xml` / `0408C62B0101B50DAD3FB9E71AB3D20509B4E64CF0AC30C01CF1DE4C4EF2297C` | `EditMode-20261003-022122-f053447a912f4ed5bf518e0528d72363.log` / `D55EBE2DD1E7A8D04CE3913189F5B26D3808E94D690465D727B6343DEFCBBB0F` |
| `SimulationRuntimeOrchestrationTests` | 12/12 | `EditMode-20261003-022132-8a6a3b692ca743f48787d630b88e8d05.xml` / `8E9FC978F8730C4EE328EEA4F605D838A804B6AAE979D1A311BFC890E85D5167` | `EditMode-20261003-022132-8a6a3b692ca743f48787d630b88e8d05.log` / `A220768430F8A09B7201708C99652434B97F0E7FB59535FFC5D4716D5CB5E6AE` |
| `SimulationRuntimeLongRunTests` | 7/7 | `EditMode-20261003-022143-13d99ed292a749c884b1389292c3eb51.xml` / `C6B18D7A8B0CD29AD44B280375C1F5A287546BB5B1B3266BB6703B87C6D2AD94` | `EditMode-20261003-022143-13d99ed292a749c884b1389292c3eb51.log` / `01CC580BD8A73D8A0C1DAA17F2A0325016589262DA783A79571463084C3D6F71` |
| ALL EditMode | 2200/2200 | `EditMode-20261003-021821-210bcc4dbc0a46b98c428beed930a823.xml` / `60ABD872DC0246BBE7A7BEB5E1DDD2343D27E62C9FFE50E5019EAB4AAC9742B9` | `EditMode-20261003-021821-210bcc4dbc0a46b98c428beed930a823.log` / `D798D792443D14324E6939DFB019EC85CB32207ED7DB2E1622A537CA1FDBC297` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | `EditMode-20261003-021858-17f8cbd341e649c599bfc9521a285d8a.xml` / `D51ED141D2F0D3AC4204AAB8965BFDFBE8CA5C3C768359BFF4ADD8810E6D52E4` | `EditMode-20261003-021858-17f8cbd341e649c599bfc9521a285d8a.log` / `F483FEB292D9D884A06F023A9D2C42D16949C6650BBDAFFC3370E80908578F75` |

The exact code-commit diff passes `git diff 'HEAD^' HEAD --check`. The unrelated
ProjectSettings edits and ArmedForce `.meta` files were left outside the
candidate commit; their preserved baseline SHA-256 values were rechecked after
validation.

## Limits retained

This is a bounded P12-B owner/operation slice only. It does not establish
complete owner or operation coverage, complete shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, P12-B
completion, P13 readiness, or Phase 12 closure. Canonical State remains
unchanged until a separate approved promotion.
