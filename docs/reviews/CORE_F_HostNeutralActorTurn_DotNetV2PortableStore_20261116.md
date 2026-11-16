# CORE-F host-neutral actor turn and .NET-v2 store review

**Disposition:** `VALIDATED_CANDIDATE` for the bounded implementation below.
This record does not promote Core canonical, close CORE-F, or claim a complete
detached host/restoration package.

## Reviewed identities

- Repository: `TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Remote candidate base at review: `a55e0cfa1cc8d8b742f99324adb58f90c8f49af1`
- Current Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Core actor-turn implementation: `bc3262b6b1f2aefc7e1de80950618c2a2cc099fa`
  (tree `14eae3ec0316497778bcc1553b9af829b27e319f`)
- Portable v2 store test addition: `731719deb53bad2da8abd3bcda77f79516127fa2`
- Test-double correction and final reviewed tip:
  `bc0e331035cce78226cc51db313907d7004edb0f`
  (tree `1f0505b0a74b6099b5c4df405ab23ff5f1f9a799`)

The implementation is the nine-file Core-owned selected actor-turn slice in
the first commit. The two follow-up commits add portable `.NET-v2` descriptor,
codec, slot round-trip, host-mismatch, and failed-reread coverage in
`DailyV1ApplicationHostTests.cs`. The test fake's `Delete` removes both a slot
and temporary data, so the failure-atomicity assertion detects accidental
deletion of the existing destination.

## Independent review

`core_currentaction_hostneutral_review` reviewed the implementation at
`bc3262b6b1f2aefc7e1de80950618c2a2cc099fa` against its exact base and returned
PASS. It found no weakened prior assertions or actionable implementation
defects. It noted a nonblocking test opportunity: explicit Core-only preflight
rejection cases for sentences, arrested state, and hidden-status expiry.

The same independent reviewer first returned `NEEDS_CHANGES` on the portable
store tests because the in-memory fake did not model deletion of the existing
slot. After the one-line correction in `bc0e331035cce78226cc51db313907d7004edb0f`,
the reviewer rechecked the exact commit and returned PASS. It confirmed the
v2 frame round-trips byte-identically, a different runtime identity is rejected
without returning an image, and the fixture explicitly does not claim the full
89-family P12 census.

## Validation

- Official CoreValidation on exact commit `bc0e3310…`: 1,140/1,140 cold and
  1,140/1,140 warm; no failed, skipped, inconclusive, or not-executed tests.
- Run: `9c34589eb22b4f31b02d1084e5e85a71`.
- Stable input digest: `4f84b6c71bcd62ebaafadc17af170682e07f9d1fa0cabbf1f69f1a0b4056667a`.
- Changed portable test source SHA-256:
  `889f3ff01aa25a16ab3a48ca80b8111e461007c60830422ca19e638cce034226`.
- Cold TRX SHA-256:
  `4859f4df0018bac6e3c3e057cf0211cf9da7ee77bb2718ff0f723ca69f7089a0`.
- Warm TRX SHA-256:
  `084a64fe747a8af4bf5ff7ced0a9b659dd06091881f186d9ad68963decf6be53`.
- Existing exact-source Unity evidence for the unchanged Unity input set:
  EditMode 3,321/3,321, run `f983aac04b3849e3879e4d4367d8ac98`; Smoke 5/5,
  run `5f9fc00f97244587b7264011bc5f975e`. Unity was not rerun for the portable
  Core test-only successor because it changes no Unity production or test input.
- `git diff --check`: PASS for the reviewed test change.

The runner reports that this checkout contains a Unity `Library`; this run is
not evidence of Library-free .NET development. Raw TRX and command logs remain
in protected local evidence storage. This review record does not publish those
reports, replace them with hashes, or satisfy a final promotion evidence-package
gate. No sanitized result package was produced.

## Scope and remaining limitations

The new portable persistence fixture uses one synthetic required owner section
to test the descriptor/codec/atomic-slot boundary. It is not a P12 census
acceptance fixture. Full 89-family owner coverage, the entire selected profile,
public `DailyV1Application.TryStart`, complete host-neutral daily execution,
detached restoration, and separate-process continuation remain outside this
checkpoint and must not be inferred from these results.
