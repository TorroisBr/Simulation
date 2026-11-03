# Independent Review — CORE-F P12 Restored Owner Vector (84d26ff)

**Verdict: PASS — `VALIDATED_CANDIDATE`.** No actionable implementation findings.

## Reviewed identities

- Repository: `TorroisBr/Simulation` (`origin` verified).
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Reviewed implementation tip: `84d26ff0b21a7373ea219bc643fb9434112a75b7`.
- Reviewed implementation tree: `bb98684a0559191e9972ff243ce46e7bdea5f126`.
- Candidate base: `120213dad68ee7adaa9b7a70f66896ded6ce17bf`.
- Candidate branch evidence/docs tip at review: `65ea050fe05d66d3d7ff5f1fed042021fe18070d`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (ancestor of the reviewed candidate).
- The evidence/docs commits after the implementation tip do not change the implementation tree. `65ea050` only corrects the full 64-character CoreValidation input-digest label in `VALIDATION.md`; its parent adds the validation record and Unity package.
- The reviewed base-to-code diff contains exactly five intended production/test files: `SimulationRuntime.cs`, `SimulationRuntimeOrchestrationTests.cs`, `DailyV1Application.cs`, `DailyV1RuntimeAdvanceStage.cs`, and `DailyV1ApplicationSessionGateTests.cs`. Commit `84d26ff` changes only `DailyV1Application.cs`; the complete two-commit code delta was reviewed against the stated base.
- Candidate source files are unchanged from the tested identities. The candidate worktree also has unrelated local ProjectSettings edits and untracked/private evidence; they are outside the candidate diff and were preserved. Validation used an isolated exact-source worktree with stable before/after source snapshots.
- Review method: independent read-only exact-tip code/evidence review; no tests rerun.

## Findings and contract review

No actionable source, owner identity, complete census-vector, authority, failure-atomicity, revision/epoch, determinism, or scope defect remains.

Commit `64334695` moves selected census row removal to Core. The Unity host supplies the complete quiescent census and shared mutation epoch. Core removes/recomposes migrated rows only when the adapter proves the exact selected Core source bindings. Otherwise the normal capture path retains the complete host vector. Core validates the existing selected Daily-v1 structural manifest and exact owner role/schema/instance/cardinality/revision before filtering. Additional or mixed host rows are preserved for the existing compatibility/stale-boundary path; selected City rows are removed only when their full exact witnesses match. The schema/version and census section identities are unchanged.

Commit `84d26ff` applies that same exact-source requirement to restored-image target composition. If the fresh target cannot prove the exact Core-owned selected sources, the private target vector is rejected, outputs are cleared, and mutation epoch is zeroed before final admission or session publication. On success, Core removes only the migrated rows, recomposes them from the fresh target owner graph, and preserves the captured shared epoch. This is consistent with the P12-A contract to reject unsupported target compositions and compare a fresh target census before admission. It does not change ordinary runtime/capture mixed-roster fallback: complete host vectors remain intact when the normal adapter lacks exact bindings, and compatible extra/mixed rows are not spliced or dropped.

The restored-image change does not widen P12 scope or add a writer. It preserves owner-local revisions through census recapture and does not mutate the epoch during recomposition; failure cannot publish a partial vector. The exact Core source predicate is based on the selected execution roots and same-instance owner references, not filenames or counts.

## Validation evidence inspected

The committed record is `docs/validation/CORE_F_CoreOwnedRuntimeApplication/P12RestoredOwnerVectorCoreAdmission_20261103/VALIDATION.md`. Its corrected input digest is the 64-character value `b4e56cb367094de70c98a6dd38345bd927f8d4ffeb881082ae94c57c879acabc` and matches the CoreValidation manifest.

- CoreValidation run `a4430f0d5f1f47d6a0ae2d1da57332cd`: cold 1001/1001 and warm 1001/1001 PASS; stable input digest. The run used base commit `64334695` with the tested `DailyV1Application.cs` working input hash `eb1c1c574b8e494ab2b465ab152ff79ceee28c68ce21eabf33fc856ea6ffd8f8`; its clean Git identity is candidate blob `3d563365cb25be8821512bf1ac9dd36a616992b4`.
- Focused restored-image continuation run `438f8e6d5bfe4956865209e1dcc23072`: 1/1 PASS, stable exact head/tree `84d26ff` / `bb98684`. XML and log hashes match its run manifest.
- Full Unity EditMode run `96f5bdb35ce8452bbd9933371d677f78`: 3296/3296 PASS, zero failed/skipped/inconclusive, stable exact head/tree. XML and log hashes match its run manifest. The focused regression and all five official Smoke test identities are included in this full report.
- Official Smoke-filter run `ba7d7db0950c49c9aa1f49f36a7acf6c`: 5/5 PASS, stable exact head/tree; XML and log hashes match its run manifest. The standalone filtered-Smoke packaging attempt was rejected by the packager's suite testcase-count coherence check; the original report remains preserved and unmodified, and all five Smoke cases are included in the successfully packaged full EditMode report.
- The official `Package-UnityValidationEvidence.ps1 -Action Verify` command passed against the full EditMode run: `runs=1`, `entries=1`; archive SHA-256 `370bcbbb2c0a910c9cdaa03051cf0067fd65f483c028c1725fd0a4d7558d1b14`. The committed package manifest SHA-256 is `7b7b275fe8cc67ea45218fdde60d05ba23e69f29643f59cb4dbc0b848ca0c2c8`; both hashes match the committed validation record. Original XML/logs remain protected outside Git; the public package contains normalized result XML only and passes the privacy scan.
- `git diff --check 120213d..84d26ff`: PASS.

The reviewer verified raw XML/log hashes against the stable run manifests, exact before/after source identities, package/archive identity and package verifier result. No test rerun was needed.

## Scope and limits

This validates only the bounded Core census-composition and restored-image target-vector correction. It does not complete or promote CORE-F, change Core canonical, reopen or broaden Phase 12, or authorize the complete detached .NET host. Existing P12-A semantics and profile boundaries remain in force. Further CORE-F integration remains separately required.
