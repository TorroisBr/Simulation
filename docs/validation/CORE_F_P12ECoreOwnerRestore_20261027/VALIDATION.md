# CORE-F P12-E Core owner restore staging validation

This candidate moves the selected Military, Conflict, War, and Battle owner
snapshot staging into the Core P12-E restore chain. The Core builder retains
dependency order and private staging; Unity consumes the staged Core owners
and keeps the established projection and observer seams.

## Results

| Gate | Result | Run |
|---|---:|---|
| CoreValidation cold | 902/902 PASS | `e9d4223e545a429282d4ee0b5c4bda73` |
| CoreValidation warm | 902/902 PASS | `e9d4223e545a429282d4ee0b5c4bda73` |
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

The CoreValidation runner reported the cold/warm result above. Its original
manifest SHA-256 is
`d8df70cf3d318438906061d4b6aa1b0f9a7c5390c7662f75a88d3e9135ab4e3d`; the
cold and warm TRX SHA-256 values are
`118326a6a6ddf71749473531a715ab9292b013b84cc3f1a4dfdf593581978158` and
`a5917d442ee8df76a1b6cac29eb21f6f2ff28c32fe579cde24950db41d004667`.
These originals remain in protected local retention. The runner emitted
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