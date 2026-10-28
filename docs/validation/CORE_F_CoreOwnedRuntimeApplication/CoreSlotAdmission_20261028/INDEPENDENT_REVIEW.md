# Independent exact-tip review: Core initial continuation-slot admission

## Verdict

**VALIDATED_CANDIDATE — PASS.** No actionable findings on the exact candidate source. This review does not authorize canonical promotion or claim complete CORE-F acceptance.

## Exact identities and scope

- Repository: `TorroisBr/Simulation`
- Candidate branch/tip: `codex/core/CORE-F-core-owned-runtime-application` / `83e2d0f14de1314ab0f16dd290c19158712d9527`
- Candidate tree: `4398458f432f1d034fd193143e1697f06acb95b8`
- Base: `45875a1d721418606769f64ad85db3593984fc0f`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The remote candidate branch was verified at the exact candidate tip.

The candidate commit changes exactly four production/test files plus a validation record, a normalized Unity EditMode package, and its manifest. It contains no ProjectSettings, unrelated, or protected user files.

## Review findings

No actionable findings.

The Core method `TryInspectInitialContinuationSlotForStart` reserves the one-shot initial attempt under the application gate, binds the bootstrap owner thread, and leaves the application blocked while inspection is pending. A missing slot returns `NoSave` and is the only outcome that clears the genesis block. A present slot, uninspectable slot, invalid input, or wrong-thread call remains fail-closed. The method does not decode or claim compatibility for a present slot.

In Unity's bootstrap caller:

- When the host cannot construct a truthful compatibility descriptor, it delegates initial slot inspection to the Core application. Only a successful absent-slot `NoSave` allows ordinary genesis; a present slot or storage/inspection error sets bootstrap failure.
- When the descriptor is available, the existing identified-host load path is retained. `NoSave` falls through to genesis; any other load failure is reported and fails closed.
- The Core-owned path removes the prior duplicated Unity preflight read. The test's expected read count changes from two to one, which matches the single Core-owned file-port read and does not weaken rejection.
- The added tests cover absent-slot genesis, present-slot rejection, storage-error rejection, and no pre-genesis identity allocation. Existing invalid-save checks remain.

The failed-build/no-save case is correctly handled: failure to create the compatibility identity does not itself make an empty storage root fatal, but it cannot authorize replacing an existing or inaccessible save.

## Exact-source and validation evidence

The candidate validation record lists SHA-256 hashes for the four changed code/test files. I independently ran `Get-FileHash` against those files in the E: candidate worktree; all four values match the record:

- `DailyV1Application.cs`: `3add1d42a35efbb65c9f30ea45fb698e9f6e7c1eefc878744b85dd58d6274cfd`
- `DailyV1ApplicationHostTests.cs`: `e19c3f2d8abe7ad5ac36f20ffd4b7887d7f7c57be3897ff438a6e699d6d69525`
- `TesteSimulacao.cs`: `97d456c2fa5cbe5f64eb13498a986a601a764e6153f962e930d06eeb4bd24dec`
- `SimulationRuntimeAdmissionTests.cs`: `dd2ff0039388fbc3f0bd01d6aba52cb93a3370fa63dbf41370ed48c7015140db`

The validation record reports CoreValidation cold/warm 904/904, affected Unity tests 253/253, full EditMode 3275/3275, standalone Smoke 5/5, and `git diff --check` PASS. The published Unity manifest identifies the stable run and exact source snapshot; its captured HEAD/tree are the base commit/tree, with the four tested source/test overlays tied by the hashes above. I independently verified that the E: worktree manifest and normalized archive SHA-256 values match the published record (manifest `303602d5fde8776f9772ec4cda091e8e122d5b6110bea405b620a87ba3b32f8a`; archive `e89b903ac65ffc394b0102a4a10e44989a9e6a2edc5774eebb7fdd6360f3fc67`). The record reports the official package verifier PASS; I did not rerun that verifier.

No public .NET TRX package is claimed. The validation record explicitly says the official runner emitted a schema that does not meet the required `dotnet-trx-source-context-v1` input for the approved packager; its originals remain owner-only. I could not inspect or independently rehash the protected CoreValidation manifest/TRX, so CoreValidation results are treated as supplied official-runner evidence, with the changed source/test hashes independently matched to the record.

## Limits

This change provides Core-owned initial slot inspection and the existing bootstrap's fail-closed/NoSave routing. It does not complete public `DailyV1Application.TryStart`, detached .NET hosting, or CORE-F acceptance.
