# P12-D City/NPC restored-owner validation

Date: 2026-10-24
Candidate: `60fd2401a8ea5be889f084149a9ee9d493082c3d`
Candidate tree: `bceabfe42e7c4641eac2fbb689ce51a7d5d620a0`
Base: `6676c7015c48992818181f17cae0edeb74c98848`
Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Scope

This candidate adds the bounded P12-D restored-owner construction for the selected City and NPC graphs. Core stages detached values and creates the authoritative owner instances; Unity projections bind to those exact instances. The added tests cover value/revision restoration, identity binding, invalid or foreign graph rejection, and no partial owner publication. The final metadata-only change supplies the standard Unity folder importer fields while preserving the existing GUID.

This is a partial P12-D restoration-owner slice. It does not complete P12-D, the shared Daily-v1 runtime/restore package, or CORE-F, and it does not promote Core canonical.

## Validation

All evidence below is for the exact candidate commit/tree above. Unity source inputs were stable during every run.

| Gate | Result | Run |
|---|---:|---|
| Focused NPC restored-root tests | 30/30 PASS | `232249d373d34542adf0d26b4a6fc474` |
| Unity ALL EditMode | 3261/3261 PASS | `9f6ce105aeb748328fe3b00a735caf5c` |
| Official Smoke filter | 5/5 PASS | `0f44fbe2b89944f8b255b002bcdaf2f7` |
| CoreValidation cold | 709/709 PASS | `60727994918847aead2cfdbd992fc8bd` |
| CoreValidation warm | 709/709 PASS | `60727994918847aead2cfdbd992fc8bd` |
| CalendarDemo cold/warm; Simulation.Lab build and cold/warm | PASS | `60727994918847aead2cfdbd992fc8bd` |
| `git diff --check` | PASS | candidate source/meta tree |

The CoreValidation manifest binds source commit `60fd2401a8ea5be889f084149a9ee9d493082c3d`, tree `bceabfe42e7c4641eac2fbb689ce51a7d5d620a0`, input digest `d0703ca31a5b2314d9bfe17f20ae08611d3372429f1ddeab85c4c48027a4cd04`, stable source/generated inputs, and no worktree changes. The private runner manifest SHA-256 is `5433f134bc9967c6adc510fb8dc3dd4b822b0ef9962e8bc46a0f83b4ad315cfb`. Original cold/warm TRX SHA-256 values are `867ed2177d4fdc93837a6156eb036f681f88f9bea7264b52a6b637c7caaf821e` and `2ad648ee546427624dd688f825dc3ca7388ee919332574eca7489891fec048c6`; their original bytes and the runner manifest remain in protected private evidence storage and are not copied here. No public DotNetTrx package is claimed for this run.

The earlier run made with `core.autocrlf=false` is excluded: it checked out the content fixture with LF line endings, while the existing test pins the Windows checkout representation. The successful official run used the repository's normal Windows checkout configuration; the fixture was 212100 bytes with SHA-256 `422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.

The official normalized Unity package for ALL EditMode is included beside this record. Its archive SHA-256 is `6128915dd9770a5f5f5e343b700e283648bddaf7c1c0a59d26bcd78fb8a55044`; its manifest SHA-256 is `88cb568cc0c75d453c07e86b9b0820815772f7f6ae7afac733bb1756922c73f8`. The private source-run manifest for that package has SHA-256 `6994611c941462850f2a93623f2bff23bf9bf3f73f0ffbde8a8d9c65f671a023`. The package was produced and verified by the official Unity validation evidence packager. Focused and Smoke original reports remain protected; their private manifest SHA-256 values are `1d46b79b96a133421a31f1726fcdae44cb802afa010d23358fa3285852126129` and `9be5f5b0fc9425c73190e578dc0ad1487c8e71026b274b4d2e536f0cd73608fe`.

No absolute workstation paths, raw TRX reports, command logs, or private runner manifests are included in this public record.