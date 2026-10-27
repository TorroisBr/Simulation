# Independent Exact-Tip Review - CORE-F Daily-v1 NoSave Genesis Gate

**Verdict: NEEDS_CHANGES**

## Reviewed identities

- Repository origin: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-runtime-start-admission`
- Candidate tip: `35e7708f2e58dca8bc158f62f48979e18e4b89fb`
- Candidate tip tree: `37b74e850938f03defdda7724aa0276d35273fd5`
- Implementation commit: `209dd9c120012d248c25c3d410bb7fedffb54f47`
- Implementation tree: `1c77a1cf559d9399d8affaa427d1d05f0b8a291e`
- Exact base: `697d60cf02fc79301d2bc5a6cbe97708bb6b2ac8`
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

Remote refs were checked during review: the candidate branch remained at `35e7708f`, and Core canonical remained at `a07402f`. The implementation commit is a direct child of the reviewed base. The candidate tip adds only the three validation-package files over the implementation commit. The candidate diff contains no ProjectSettings or unrelated files. No canonical promotion is claimed.

## Implementation and tests

The prior direct-genesis bypass is fixed. `TryPrepareInitialGenesis` now rejects unless the Core application recorded an attempted, successful slot inspection; the slot must be absent and no failure may block genesis. The guard runs before setting the genesis-attempt flag or composing/allocating the candidate. `P12AContinuationStore.TryHasSlot` returns `NoSave` exactly for a successful missing slot; present slots and failed inspections do not satisfy this gate.

The production Unity path inspects the slot before its sole genesis call. Core regressions exercise a direct unprobed call (rejected with no candidate), followed by successful absent-slot inspection (accepted). Unity fixture updates arrange the missing-slot probe before selected-profile bootstrap. Existing assertions were retained; the corrupt-slot expectation now checks the more specific NoSave-required diagnostic. No P12 or CORE-F completion claim is supported by this slice.

## Validation and source identity

- Independent official CoreValidation rerun at candidate tip `35e7708f`: cold and warm **901/901 passed**, zero failed/aborted/skipped/inconclusive; discovery 901; input snapshot stable; `Library` absent. Run `5eec480d04f94b51b4a4bed142787c7b`, input digest `efe086433cbc063c4216fa858022e5f89263fc62dda464b4f6596e59d4f0b3d5`. Protected raw TRX/log evidence remains private; the approved .NET TRX adapter is not delivered, so this does not satisfy canonical-promotion evidence requirements.
- Candidate validation record reports full Unity EditMode **3274/3274** and filtered Smoke **5/5**. Official packager verification passed: one run, one archive entry; archive SHA-256 `5c3e07d48775049b583db2f5c342b6ed270dc7970a374f3131f0d28a6257e0ef`. Packaged XML contains 3274 passed cases and no absolute local path tokens. Changed Unity source/test file hashes match the committed implementation sources. Protected raw Unity originals were not independently re-read.
- `git diff --check`: PASS for the exact base-to-tip diff.

## Actionable finding - validation record hash mismatch

`docs/validation/CORE_F_DailyV1StartAdmissionNoSaveGate_20261027/VALIDATION.md` lists normalized NUnit XML SHA-256 `38e1418f86ad753b1192cadced994583ac70ea79fb02cd85cc6a5624b722cec7`. The committed package manifest identifies the normalized XML entry as `2b0892d6add5969b54a3b46a85fc29541c6c77939cb11854de589726603675ef`; hashing the actual sole ZIP entry confirms `2b0892d6...`. The archive itself verifies and its digest matches the manifest and validation record. Correct the stale XML hash in `VALIDATION.md` to the actual manifest/package entry hash, then request a fresh exact-tip review of that docs-only successor.

## Scope and evidence limits

This is a validated implementation candidate with one documentation/evidence-record correction required. Core canonical was not changed. The candidate does not complete `DailyV1Application.TryStart`, detached restoration, a fixed .NET host, separate-process T/T+1/T+2 continuation, or CORE-F. The private CoreValidation TRX is not published because the approved adapter is unavailable; no promotion readiness is asserted.
