# P12-B / WI-A current-tip runtime composition revalidation

**Status:** `VALIDATED_CANDIDATE` for the bounded current-base WI-A runtime
publication composition. Independent exact-tip review and the focused, full
EditMode, official Smoke, and diff-check gates passed. Canonical promotion is
a separate human gate. P12-B remains incomplete, P12-A remains
`WAIT_DEPENDENCY`, P13 remains blocked, and Phase 12 remains open.

| Identity | Value |
|---|---|
| Candidate branch | `codex/wia/P12CurrentRevalidation` |
| P12 canonical base and current P12 canonical | `d6e52dcdbf5fe2a36de92c6516485040d54e4279` |
| Architecture authority | `c285466c355103d3637ac165246591b72eb7bda0` |
| WI-A canonical | `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5` |
| Revalidated WI-A implementation source | `3b39e0d89858dce517ad72cbb76da621eb954bad` |
| Code candidate | `19bcf3415555b5ed336664e9dee925d4497b219c` |
| Tested code tree | `5b82e0e67a2f454850c4a8da70729443016c61c4` |
| Exact-tip independent integration review | PASS against P12 base `d6e52dcdbf5fe2a36de92c6516485040d54e4279` and WI-A canonical `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5` |

## Delivered boundary

This candidate revalidates the existing WI-A `WorldId` runtime publication
handoff on the latest promoted P12 tip. The runtime and
`SimulationBootstrapComposition` receive the same unpublished `WorldId`
object; composition checks that exact identity. `TesteSimulacao` keeps public
identity and composition unavailable until bootstrap, owner-thread/admission
checks, and the selected P12 owner census assessment succeed. Failure clears
the draft identity/composition and faults admission. P12’s registered NPC
money-transfer operation and owner notifications remain present through the
automatic merge of the older WI-A change onto current P12 canonical.

The candidate changes the expected nine WI-A paths only: runtime and bootstrap
composition, `TesteSimulacao`, `WorldIdentity`, their relevant integration
tests, and the WI-A handoff note. The integration changed no FR-B or P12
implementation files. `SimulationRuntime.cs` auto-merged without conflict;
no conflict resolution or unrelated ProjectSettings/`.meta` path is included
in the committed candidate.

This proves only the current WI-A/P12 identity-publication composition. It
does not establish a real FR-B Faction/Person read cut, complete P12 owner or
operation coverage, global mutation-epoch coverage, runtime-wide quiescence,
capture eligibility, export, hydration, P12-A readiness, or P12-B completion.

## Exact-tree validation

Every result below was produced against tested code tree
`5b82e0e67a2f454850c4a8da70729443016c61c4`, before this documentation-only
record. XML and log files are retained under the ignored local archive
`Library/ValidationResults/WIA-P12-20261002/`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P18DConsumerIntegrationTests` | 10/10 | `39C7C9C20B85361502EEAE6B28410CCB6D28CB382E7099289F383EAF8EA61AEC` | `ED2C96DA734311FFF06396DE4AF9DB6379317323CD8D303941446EAEE627F1B0` |
| `SimulationBootstrapCompositionTests` | 19/19 | `DFC0A06551AA0E1ABA56977BBB9AF93882D0A1AA3B704DBCEF9A320D7686B98B` | `B6FD405CDA6CCED4F49CD18A78555B81CDFF56B5B5BAE8B7B4E11D983AD6CA1D` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `EE952E438B545FD2AB91117A7453421701A91A722ED661388DB98214040EB507` | `D21ED220A65239F8818F4E70FF66B6B2598E2646071F928D0AF7DFD33BEC21A4` |
| `EconomyTransactionTests` | 45/45 | `05E20DBE439597EC8E5F0B26F2967494A76BDFB7542674107546F7DD2F36E652` | `3625912BDD5F39347D8C73AA5A6F3285909F4E4D0B5D4872460AC761658DEAD8` |
| ALL EditMode | 2166/2166 | `6230AFE0D8BBFBBE56C5FA09F2C2C503C57AA9219E94131DED4DB46E7F5B0F76` | `CAA7DD5B78BF4EBAF122842784A6E7DABDCCB380A17FFBAF3237C68BCEC423C3` |
| Official EditMode `Smoke` | 5/5 | `3FF344965C04D0A0F0D49CF40BD9AB0F1405675E0A1CF611EFEFF000C78BA7B6` | `90789BB906CB18B3E159AA9ED783F1B7C523D0F5C01909AB4E5DEADDFDAC4C28` |
| `git diff --check` against P12 canonical base | PASS | — | — |

## Exact-tip integration review

An independent Luna reviewer inspected exact candidate
`19bcf3415555b5ed336664e9dee925d4497b219c` (tree
`5b82e0e67a2f454850c4a8da70729443016c61c4`) against P12 canonical base
`d6e52dcdbf5fe2a36de92c6516485040d54e4279` and WI-A canonical
`534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5`. Review result: **PASS; no
composition finding.** The review confirmed the expected nine WI-A paths, the
preserved P12 admission wiring, exact-instance `WorldId` binding, publication
gating, failure cleanup, and no FR-B/P12 implementation changes. The prior
review at `faa06800d93db2f17f9ae0a39f6f27185e36b104` applies only to the old
candidate based on `a66c215` and is superseded for current-base evidence.
The reviewer also confirmed the current architecture authority
`c285466c355103d3637ac165246591b72eb7bda0` and canonical WI-A handoff add no
constraint that changes this result: P12 admission and owner notifications
remain intact, while the exact Faction/Person read cut remains separate FR-B
work.

This validated candidate is not canonical and is not a promotion approval.
