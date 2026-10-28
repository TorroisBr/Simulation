# CORE-F P12-E Core owner restore staging validation

This candidate moves the selected Military, Conflict, War, and Battle owner
snapshot staging into the Core P12-E restore chain. The Core builder retains
dependency order and private staging; Unity consumes the staged Core owners
and keeps the established projection and observer seams.

## Results

| Gate | Result | Run |
|---|---:|---|
| CoreValidation cold | 902/902 PASS | `080a00a72b0b433890ce7773f9c09e5f` |
| CoreValidation warm | 902/902 PASS | `080a00a72b0b433890ce7773f9c09e5f` |
| Focused P12-E owner composition | 1/1 PASS, stable input | `ac8f0a8c841744b5be5d4e33aaaa5aea` |
| Focused restore stage-order regression | 1/1 PASS, stable input | `06bb6cd148f24f69828729e706676fcf` |
| Full Unity EditMode | 3274/3274 PASS, stable input | `d1ed853985ed44b19310b2a2e9a8e45d` |
| Official Smoke selection | 5/5 PASS, stable input | `65f4210ab1f74f82b47ce2fdbd22040a` |
| `git diff --check` | PASS | candidate worktree |

The full Unity run has zero failed, skipped, or inconclusive cases. The focused
restore regression verifies the existing failure-atomicity test seam observes
the completed earlier Core stages; assertions were preserved.

## Unity evidence

The full EditMode report was packaged with the approved Unity evidence
packager, then verified against its private run manifest. The package contains
only normalized result XML and relative source identities; original XML and log
remain unchanged in private retention.

- Bundle: `coref-p12e-owner-restore-editmode-20261027`
- Archive SHA-256: `c333bb10f2c40a2f7e487aab21d5929658b97453f11628305ccf851c79c63c05`
- Public manifest SHA-256: `9834b04af2802cf5bd062e0f9ee619ca16b13e4d444ca8a96277a48f2965b54a`
- Original XML SHA-256: `abffcd5bb52104e629367e9e50b4dca28fddae6a5c9697a37d8657225ddb983d`
- Normalized XML SHA-256: `65219462aaa56d9b0aa36faa7d9d8720c762ef67c18af2a1e3ba6f45f245e16c`

The packaged manifest has no Windows absolute paths. The separate filtered
Smoke XML is retained privately; the full EditMode package already contains
the same five Smoke identities and outcomes.

## .NET evidence limits

The initial CoreValidation run (`e9d4223e545a429282d4ee0b5c4bda73`) is not
used as exact-source evidence: independent review found that its recorded
hashes for the two changed Core test files did not match this candidate. The
approved runner was rerun on exact candidate commit
`bb408eb489cccd5e8d642853d3cf164be33c129d`, tree
`2041fbd258591a3553defc72d03a305331ef1e9b`, with stable input digest
`e006428dcc98d73a5d69d75c541cd3fc4d6cc99748faca46d9034b356fc449f4`.
Both changed test inputs now match the candidate: `DailyV1ApplicationSessionGateTests.cs`
`ff7c0417a51c6ca7bce40a523571db3b3668a3c8cd05e188120c45880a226583`, and
`P12ECoreOwnerRestoreStagerTests.cs`
`708db3b138ffa7a313c820bc664e28d0d7aea8fb43307bd9f05353dcb65f3c9a`.
The protected manifest SHA-256 is
`70a5bb5e7e2385173b5964bbb87fb00fa394086b7def72ed9222744df9ee94b1`; cold
and warm TRX SHA-256 values are
`6d714e207c0770656f8a46ebcd8a235725d90f099c43e597defa4cfa6d81b64d` and
`82213f2b24643d74087607501943e40ecedf4391d701043a5b7c0b46c7fe6881`.
Original results remain in protected E: retention. The runner emitted
`simulation-core-validation/v1`, not the separate
`dotnet-trx-source-context-v1` required by the approved TRX packager, so no
public DotNetTrx package or private-to-public comparison is claimed. No
source-context record was fabricated.

CoreValidation reported that the checkout contained a Unity `Library`.
This run makes no Library-independent .NET-host claim.

## Source identity

The Unity runs used a separate E: validation worktree. All six changed source
and test files were verified byte-for-byte against the implementation
worktree before and after the stable full run. The two unrelated local
ProjectSettings inputs were copied byte-for-byte into that isolated worktree;
the original files were not modified. The full Unity input snapshot remained
stable throughout the accepted run.

This is a validated candidate checkpoint only. It does not claim CORE-F
completion or Core canonical promotion.