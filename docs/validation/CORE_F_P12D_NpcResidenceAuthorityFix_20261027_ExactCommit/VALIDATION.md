# CORE-F P12-D NPC Residence Authority Fix Validation

## Candidate identity and scope

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Fix base: `ec27067a075c9e37a813feb9a9d99d4ff2b4820b`
- Code commit: `6a72c2c1e12ff98e7272a70f3a404c27b057cf15`
- Code tree: `52f1246e073ca93046c4fbd2207846d04513fd4a`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

This follow-up fixes two findings from the independent review of the prior
candidate:

1. Selected NPC residence idempotency and resident-death clearing now consult
   the selected Core residence owner through `ResidenceSettlementRuntimeId`,
   not the stale Unity compatibility backing field.
2. P12-D export now records the residence revision from the bound Person owner
   when that Person is the residence authority; unbound NPCs continue to use
   their NPC residence owner revision.

No P12 schema, operation, or profile scope changed. The tests cover repeated
same-value residence assignment, selected immigration followed by resident
death (including residence/life revisions, City population revision and
epoch), and a Person-backed NPC export whose Person residence revision differs
from the NPC compatibility owner's revision.

## Validation

### CoreValidation

The official CoreValidation runner passed **891/891 cold** and **891/891 warm**.
Its build, demo and Lab gates passed; all 891 discovered tests executed with
zero failures. The input snapshot was stable and its changed Core source/test
hashes match the committed files.

- Run: `79eeeb1fd55145348a30604b212a797f`
- Private runner manifest SHA-256:
  `8dc5bd0a876014698b1f6bc829e7bb5cd3e353616581806efa9d10d6c63bcb58`
- Input digest: `61a58f9af55100635974786ddeef0d912def4786dd8dd1a37ab05c9c4c99bd45`
- Cold TRX SHA-256: `194614a76cd95bf5dc1ce0bf0f16f941d884eb461065968610d75b7bcf054660`
- Warm TRX SHA-256: `6a44056b5f534507db754e603aa0c6c8817eef0396f7b4287aa1d5380fcb8c01`

Original .NET reports and logs remain in the owner-only local evidence store;
raw TRX is not included in this public record. CoreValidation ran with this
worktree's existing Unity Library, so this evidence makes no Library-free
environment claim.

### Unity

Validation used a clean, detached E: worktree at the exact code commit, with a
separate Unity Library. Unity 6000.3.9f1 was used. The first Unity import changed
two generated ProjectSettings files, so its successful test result was excluded
as unstable. The following runs were made after import and all reported stable,
identical input digest
`d6696c3c7375746bbc0ca71937566ee90c4bbb6973d3d1c1cd3d247bdc15511a`; the
code HEAD remained the exact commit above:

| Gate | Result | Run ID |
|---|---:|---|
| `P12PopulationLifecycleInvalidationTests` | 22/22 PASS | `0789fea9f7d440049270470fa57f7407` |
| ALL EditMode | 3274/3274 PASS | `2c0decda4bb2472b92fc43ee4a637a05` |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 PASS | `72966559d1c449cba394f55a4dfc6e0e` |

The full-run original XML SHA-256 is
`8bfe6df213efd127793e3b8c363424f0949e6f4ca39f1ae9369f09fc0ae6cba4`.
Its normalized XML-only package passed verification against the protected
original run. The package is
[`coref-p12d-npc-residence-authority-fix-20261027-exact.zip`](coref-p12d-npc-residence-authority-fix-20261027-exact.zip),
archive SHA-256
`6d1483c89c1c940e3ab37f57622f32f130f78dcfd493cc595d165c8a7e96d9c1`;
manifest SHA-256
`c23e1909cbe395c85e19ba52b9095ac4ea769a8b4eb0c43c8f1114edec63a7a7`.
It contains all 3274 case identities and outcomes. Original XML/logs remain in
owner-only local retention.

The official Smoke gate is the repository's five-test EditMode `Smoke` filter.
A separate PlayMode filter probe selected zero cases and is not counted.

### Integrity

- Changed Unity and Core source/test files match the exact validation input
  snapshots.
- Package verification against the protected source run: PASS.
- Public package manifest local-path/credential-pattern scan: PASS.
- `git diff --check`: PASS.

## Status and limits

This resolves the listed P12-D NPC residence authority review findings for the
CORE-F candidate only. It does not complete CORE-F, promote Core canonical, or
change P12-A/P12-D readiness or Phase status. Shared application composition,
the remaining detached restoration/host work, and separate-process continuation
remain outstanding.