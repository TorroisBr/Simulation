# WI-A post-FR-B current-base revalidation

**Status:** exact-tip implementation revalidation PASS; canonical promotion is
a separate human gate.

## Candidate identity and boundary

| Identity | Value |
|---|---|
| Candidate branch | `codex/wia/P12PostFRBCoreRevalidation` |
| P12 canonical base | `66f91c68d367e03703ab014046d5e62c3f89ebbe` |
| P12 canonical base tree | `04c65c36b6a044707388b062c0fb312ae3da5678` |
| Architecture authority | `c285466c355103d3637ac165246591b72eb7bda0` |
| WI-A canonical baseline | `534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5` |
| Refreshed code candidate | `242ae6bf81c2f4da832be1e7948bbaac004a620b` |
| Tested code tree | `284e7a9a229671419c4ea0c545c02b4513e41717` |
| Exact-tip independent review | PASS against the candidate, tree, and base above |

The candidate replays the previously reviewed WI-A implementation unchanged
on the current P12 canonical base. Its code diff contains the same nine WI-A
paths. FR-B additions in the new base are limited to factual-read core,
`FactionStore`, `PersonStore`, and documentation; WI-A does not modify those
components. The review found no semantic or file conflict.

The bounded WI-A contract remains process-local `WorldId` allocation,
identity publication through the reviewed bootstrap boundary, and exact
identity binding for the typed P18 profile. It adds no persistence,
continuation, copy, fork, or save-game semantics.

## Validation on the exact tested tree

Focused suites, ALL EditMode, official Smoke, and `git diff --check` passed.
Unity XML/log artifacts are retained under
`Library/ValidationResults/WIA-P12-PostFRB-20261002/`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| `P18DConsumerIntegrationTests` | 10/10 | `947403D2D8555CCEEB8F51C54D049A59402F24A92EF3116ABF39407D46846248` | `4290F584E1435B661F748DECB6797B137B758A460E9223A8991252B102CD76BF` |
| `SimulationBootstrapCompositionTests` | 19/19 | `F2E9EB1D98F644BBC9DAD9C53FCDD4D3FE6DB641A7DCEEF1900DAD12ACC3ADEF` | `9D67735E66F97FB92D2646B3CB380971AA58045A2A7B15F0A304B2A52849E7AB` |
| `SimulationRuntimeAdmissionTests` | 31/31 | `79C3B97C6E8198EE9D33DAEBA65E21A8579B655C5D28059F5328B71CCCDF2F93` | `6DF3438D4974C0E292551BC99F053620D1B1E5B6122F3F103CEF071594718EDD` |
| Economy transaction | 45/45 | `DC378CCA48F27645A6AF19577F7ED14C4383A01CFFB3A82C7F7114A4574C336B` | `34F647C9F0781230156C8A68B89D5C1BE1F270CC694CEEAB26E0E771CC47C2A5` |
| ALL EditMode | 2174/2174 | `78E2AC8DFE522231CA6E4B08C63F844124A4CEBBFC90E992FC792DFDDE1E3BBA` | `524CEE7E8809368BDF0B4150B6AF717E626C57618FC5A90F4A99142A9BDC8F41` |
| Official Smoke | 5/5 | `EAEA0E0E485F5638E25BB16E4626BC8099BADD4AB982B1003B46971EB7E6AC32` | `81960C5436D19149658A5A65A39EC796D50AA0012CAF3C540A4E57D9DC3305A5` |
| `git diff --check` | PASS | — | — |

## Independent exact-tip review

An independent Luna reviewer compared the full candidate diff at
`242ae6bf81c2f4da832be1e7948bbaac004a620b` (tree
`284e7a9a229671419c4ea0c545c02b4513e41717`) against P12 canonical base
`66f91c68d367e03703ab014046d5e62c3f89ebbe`, architecture
`c285466c355103d3637ac165246591b72eb7bda0`, and WI-A canonical baseline
`534d2dd1b6ed8cdc168c0c90328adc6b6c1eedf5`. Result: **PASS**. The reviewer
confirmed the nine-path WI-A boundary and found no conflict with FR-B. The
reviewer did not run Unity; the validation above was run separately against
the exact code tree.

## Limits

FR-B's production Faction/Person read composition and selected-profile read cut
remain future work. This revalidation does not establish complete shared-epoch
coverage, capture eligibility, export/hydration, P12-B completion, P12-A
readiness, or P13 readiness. P12-B remains incomplete; P12-A remains
`WAIT_DEPENDENCY`; P13 remains blocked. The candidate is not canonical and
this record is not promotion approval.
