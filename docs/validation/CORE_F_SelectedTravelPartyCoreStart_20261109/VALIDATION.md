# CORE-F selected TravelParty start through Core

**Status:** validated candidate milestone; not Core-canonical and not full CORE-F completion.

## Candidate identity

- Candidate implementation commit: `97213fca8ce070abd2d758b99b1d8db6d614c077`
- Candidate implementation tree: `fd95a2d6a1a7e3d779a32ec558867f6b43ab9119`
- Parent candidate: `3f9e0b1f653951b83d7b4f8b487aa69d08216a0b`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Branch: `codex/core/CORE-F-restore-stage-transaction`

## Bounded behavior

For a TravelParty wholly bound to the selected Core owner graph, the shared application now owns the selected group-start operation boundary. Core resolves the admitted participants, roles, shared origin, unique route, exact City owners, and charge from the selected runtime graph, then executes the existing authoritative P12 transaction against the exact Core owners. The Unity host operation remains responsible for host projection and approved P12 preparation/commit integration. Mixed or unselected rosters retain the existing Unity compatibility path.

This is a selected TravelParty start slice. It does not complete general `DailyV1Application.TryStart`, the whole daily action system, detached restoration, the fixed .NET host, or separate-process continuation.

## Validation

| Gate | Result | Source identity |
|---|---:|---|
| CoreValidation cold | 1,069/1,069 PASS | run `63b59b79ba64459a9173bb4fd2853db2` |
| CoreValidation warm | 1,069/1,069 PASS | same run |
| Focused `P12TravelPartyStartOperationTests` | 18/18 PASS | run `ef57a9d006354c2681e483b2fa857731` |
| Selected all-Core TravelParty start test | 1/1 PASS | run `7ba247a173e24327a9f27a53f90ddb53` |
| Full Unity EditMode | 3,302/3,302 PASS | run `41104fab27ca410c91b55b2bfd34d272` |
| Official Smoke filter | 5/5 PASS | run `5a3d677b82bc4e608a425772685a3e81` |
| `git diff --check` | PASS | candidate implementation commit |

The Unity input digest was stable and identical across the focused, full EditMode, and Smoke-filter runs: `563a586d2ba5649ebd1d40b280fab7bead6cefe61fd0a039cbe021d93ca5feef`. All 1,978 Unity input-file hashes and all 540 CoreValidation input-file hashes still match the current worktree. Full EditMode had zero failures, skips, or inconclusive tests. The five Smoke identities are present as Passed in the full EditMode report and the filtered Smoke report.

The public, privacy-reviewed full EditMode package is [here](Evidence/CORE-F-TravelParty-editmode-20261108.zip), SHA-256 `0bc6a1f80bcf72cbbdc8a52f3c62431b1a75fdeff7d9b2dbffc055a17fe454e0`; its package manifest is [here](Evidence/CORE-F-TravelParty-editmode-20261108.manifest.json), SHA-256 `6659d0872543577f2a373a11da4efa6aec171fc00d6a072f29b9c4234c1b4cc2`. The official verifier validated this package against the original run manifest.

The separate filtered Smoke runner passed 5/5, but its XML retains the full-suite aggregate `testcasecount` while containing only the five filtered cases. The approved packager rejected that filtered XML as internally inconsistent; the original report remains unchanged. The verified full EditMode package contains the same five Passed Smoke identities. An earlier `TestCategory Smoke` attempt returned zero cases and is excluded from these results.

CoreValidation emitted `simulation-core-validation/v1`; the DotNetTrx packager requires a separate `dotnet-trx-source-context-v1` record and rejected the runner manifest. No source context was fabricated and no raw TRX/log was altered or published. The protected manifest SHA-256 is `99082430b666439da9f7559c7e9d100d7148c5f6cbcfc88cd20025a8f240d739`; cold and warm TRX SHA-256 values are `bd7657babf1c74737de7eb0d97c9cd542876ff5d4b996b682f6aad0c0c858f29` and `33430f86b4a8976ba3dd2d98ee96950e70453ad7106c8ad425edf7829e4109c7`. CoreValidation reported that a Unity `Library` was present, so this run makes no Library-independent execution claim. Original private reports and manifests remain in protected local retention.

## Scope and preservation

ProjectSettings, the pre-existing validation note, unrelated untracked files, and private evidence were not staged. The checkpoint preserves the selected Core execution boundary and its Unity projection compatibility; CORE-F remains open and unpromoted.