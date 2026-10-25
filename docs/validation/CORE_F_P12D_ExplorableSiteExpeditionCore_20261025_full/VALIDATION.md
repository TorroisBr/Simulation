# CORE-F P12-D Core Site and Expedition Staging — Validation

**Status:** validated candidate implementation; not promoted to Core canonical.
This bounded slice moves the existing ExplorableSite runtime/store/snapshot and
Expedition restore stager into `Simulation.Core`. Unity continues to project
the admitted NPC roster IDs and P12-D/F rows into the Core stager.

## Source identity

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`
- Code commit: `332565113c118608786ba8681c9a44ce00054bf2`
- Code tree: `5deea1f1920cc8ddd6cd1fdd0ec4ab0b4d092dbe`
- Candidate base: `84e0934ba4a223fc30f82cf5920df2a3d7010bb9`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The implementation commit contains only the Core source boundary, Unity
adapter call sites, Core compile/test inputs, and focused Core tests. Existing
ProjectSettings edits, untracked user data, Unity caches, and private evidence
were not committed. The three pre-existing ProjectSettings edits were byte
stable during Unity validation and remain outside the candidate commit.

## Validation

- Official CoreValidation run `11b1802c04ea4ec39b3054b61c3e2f65`: **803/803
  cold and 803/803 warm PASS**. Locked build, CalendarDemo, and Lab gates passed.
  The owner-only run manifest SHA-256 is
  `d24f3e3ef2e44bd7826f2b967a06b542e4fb86cb4ef6ed985471eceb2f364d4b`; its 455
  recorded input hashes match the validated worktree. Input digest:
  `77da0e4c8cc51e31f68bd95b18c1baa0dda1508e516b6c88248548589c1dc1fb`. Cold
  and warm TRX SHA-256 values are `3c149eb10cdda9cfd127bead59d6f6f4f583aa087d3f409d236360c972af4f5f`
  and `92f8fe09f6990840cff9451b2a24665e57257ec19f6c91f14362e72123c1f68a`.
  The worktree contains a Unity Library, so this is not a Library-independent
  .NET-host claim.
- Focused Unity EditMode `P12FOwnerPackage`: **2/2 PASS**, run
  `6405eb32a7f1432ba558e3133267a0f9`; original XML SHA-256
  `7f39b45cc1a1bb9a0a4e206bcb70552fdaadee26bb3927f90ed6d2a459ca60c9`.
- Full Unity EditMode: **3264/3264 PASS**, zero failed, skipped, or
  inconclusive cases; run `e4feb34cad1d49c89021bc057319ff81`; original XML
  SHA-256 `586badf19590f34db41026db2c6bd3083074ff1939a88c93f17be2b0a876becc`.
- Official Smoke (`EditMode`, `TestFilter=Smoke`): **5/5 PASS**, zero failed,
  skipped, or inconclusive cases; run `0665bf03bbc44b49b335c4ff5edce334`;
  original XML SHA-256
  `ba4c18dff4001ac58e3658ebcc1a5b5934425440c6db1524dc3940491c21eb7e`.
- All three Unity runs share the stable 1,916-input digest
  `db89ce56e74c9e72b171187f9844a12b27863c6950f8e8e739e31a469dba8c0f`; all
  recorded input hashes were rechecked against the committed worktree with no
  missing or mismatched files.
- The approved result-only package is
  [`coref-p12d-site-expedition-full-20261025.zip`](coref-p12d-site-expedition-full-20261025.zip),
  SHA-256 `8240e45f2ede62ef2b3cadda052604040426746bdd61e30d391ee273cb6b648d`,
  172,447 bytes. Manifest SHA-256 is
  `5834b3de1a00fef391e20f5128d3bd6d15fd398344585cff18501d2c78234dda` in the
  CRLF worktree checkout (`core.autocrlf=true`) and
  `b05d1735cf34b8577ae19f3140ad6f55f862c83382afeec901b5e0235036f77d` in the
  committed LF Git blob. Package
  verification passed against the protected original. The package contains the
  complete full EditMode report; exact identities for both focused cases and
  all five Smoke cases were confirmed present in that report. Filtered reports
  remain in protected local retention and were not forced into a result-only
  package because their suite discovery counts describe the unfiltered
  assembly.
- A PlayMode run without a filter returned zero tests and is excluded from the
  gates. The required Smoke gate is the official EditMode `Smoke` filter above.
- `git diff --check` passed. A `Library` was present; no Library-free
  validation claim is made.

## Scope and limitations

The Core stager validates site identity against the Core site store, locations
and routes against the Core spatial owner, and travel/member relationships
against Core NPC owners, the detached P12-D/F rows, and the staged TravelParty
owner. The positive test uses populated Core roots; a separate test confirms
that a populated Expedition is rejected when the accepted Daily-v1 profile
provides its currently exact-empty Site roots. The stager does not impose an
Expedition hard-zero and does not widen P12, P10, or the bootstrap profile.

This does not complete P12-F, detached-host restoration, the .NET application,
or CORE-F. No Core-canonical promotion or Phase closure is implied.
