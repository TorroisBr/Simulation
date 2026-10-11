# P12-G ActorChoice and TravelParty continuation parity — independent review

**Verdict: `VALIDATED_CANDIDATE` (PASS)**  
**Review date:** 2026-10-11  
**Reviewer:** independent P12-G review agent

## Exact candidate and base

- Canonical base: `c8dd04ac5d002e68eb5d03db814615d3d35f70b7`
- Candidate evidence tip reviewed: `76d01b17ea0c39c6a1c37785fba1d03cb167ae25`
- Candidate branch: `codex/phase12/P12GActorChoiceTravelContinuationParity`
- Test implementation commit: `ff6961ded876b6e2b64eddecbe30c473470bc858`
- Implementation Git tree: `786c8b2ccb072ca8cd5a4e98e4e64b98edcea15a`
- Implementation `Assets` tree: `ecd855a0f2d3c767bb494f2fe34112193381d190`
- Evidence manifest: [`P12GActorChoiceTravelContinuationParity-20261011-VALIDATION.md`](../validation/P12GActorChoiceTravelContinuationParity-20261011-VALIDATION.md)

## Findings

The full candidate diff is bounded to the existing `SimulationRuntimeAdmissionTests.cs` test file and its validation manifest/reports. It adds no production behavior, profile configuration, gameplay semantics, or actor-control policy. No `ProjectSettings`, unrelated `.meta`, or other user-owned paths are in the candidate diff.

The fixture uses the existing trusted LocalPlayer/Request SellGoods path for a Person-bound authored Daily-v1 merchant and the normal TravelParty API. The successful terminal `AttemptReturned/Succeeded` choice and the active party coexist at the completed restore boundary. The merchant is excluded from the party so the two included owners advance independently. Restore preserves the complete included-owner projection, continuation roots, and exact owner-section vector. Replaying the same command ID/payload is rejected without changing the ActorChoice count, revision, sequence, owner projection, party state, or roots. The party is explicitly active on the first two post-restore boundaries; complete owner projections and roots match the uninterrupted control on all four subsequent boundaries, and both parties arrive on the final boundary.

The test meets the exact next bounded parity slice named by current canonical State and P12-G technical design §§6.5–6.6. It does not establish universal causal no-replay or parity for every owner/state combination. The wider compatibility and graph matrix, remaining reachable failure-path cases, exhaustive owner/epoch coverage, and runtime-wide quiescence remain open. No readiness or closure claim follows.

## Exact-tree validation

All reports are for the implementation `Assets` tree `ecd855a0f2d3c767bb494f2fe34112193381d190`; each reports `Passed` with zero failures, skips, and inconclusive tests. XML, compressed log, and decompressed log hashes were rechecked and match the validation manifest.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused ActorChoice + active-party case | 1/1 | `4F87FD1B84EA82A6231C75827DAF2011D3F2750D0772A0F9983963DDE848901F` | `F4E9B25EAE90D18B4028EB34D8FFE488E9BEB492F669C3052E92AA163E7CD2EB` | `EEDF0D7FA98EF9AF161E376F23BB83B5986B9BC61F5C91410F2E8C4AD7DA09BD` |
| `SimulationRuntimeAdmissionTests` | 191/191 | `708DEBBE5F85DEF0852245E068F5DF2B7C66237274615C2C0A19D343D6C87B26` | `8589553613A34690A195483514216D4ED38F2AEDBE63C157D3F4AB7160F27038` | `364BF0741396C2B0187FC0CAD3261836F375C46D907FA56BE5690396CE2491DB` |
| ALL EditMode | 2865/2865 | `8F1A43DD7AD32C1FAF6383AF6C4AC1F9F4D8DF2852DC871A74A8B69CF62C8B99` | `7A34831E810A2CBF2CEDDB849CC057B618849307FD16199778853E7A0E206D77` | `9D109FF410DEC9BD471918783F8F661C25C1EF75F72963C7538EF97247CEC966` |
| Official Smoke | 5/5 | `82C213C431513E79BDAB3294493C50AA6E072C9F2484301D2349A586915D82E8` | `E617C9EF888F98E95406694A8B976A95AA29F5BF3BE898A33EC88AC1195CC71D` | `11256186C780B573692543D81DE529088F665A4E6448DFFD445509A8294FCC11` |

`git diff --check c8dd04ac5d002e68eb5d03db814615d3d35f70b7..76d01b17ea0c39c6a1c37785fba1d03cb167ae25` passed. The candidate Assets tree is unchanged between implementation commit and evidence tip.
