# Validation — initial start admission token

Candidate: `52dfcdcc62a1544f33bf643e201cb6ebd45744e6` (tree `25fed4035badb043dfcdc524dc4f3b822eda85b0`), parent `31df5581e3d0727ef7f2db541838f4e37d25c0b1`. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This checkpoint binds the initial persistence-slot result and any loaded continuation image to the exact `DailyV1Application` instance. Genesis requires that application's exact `NoSave` result; restoration must use the exact admitted image. It does not complete `DailyV1Application.TryStart`, move selected runtime construction or the P12G projection into Core, complete detached hosting, or claim CORE-F acceptance.

## Validation evidence

The official CoreValidation runner passed cold and warm, 1005/1005 each, zero failures. Its run identity is `63ac2d3e13e64030848726ebe264d229`; manifest SHA-256 is `13f43f03ba9f70bfa86cabf7bd742a9d46c93fe7d0e89c245d673eacf6f649f`. This was incremental Core validation; the runner reported `LibraryAbsent=False`, so it is not a final Library-free .NET proof.

The official Unity EditMode RuntimeAdmission suite passed 243/243 with zero failed, skipped, or inconclusive cases and stable inputs. Run identity: `22fe08a500cb4af29ac8c865fa2ebec7`; manifest SHA-256: `abb8b25c5c924df233c130133fdd21870bd89b36eac91b83a98ef13b96c463ec`.

The official Unity EditMode BootstrapComposition suite passed 39/39 with zero failed, skipped, or inconclusive cases and stable inputs. Run identity: `0891051404504d50acc4d20176e06301`; manifest SHA-256: `91b7754156d40851f91a664b00e6f10148f89ee6ec4000c04fc00336b9aba058`.

The runs were executed while the five candidate source files were stable in the worktree, before the commit was created. Independent review compared the captured input hashes to the committed file bytes and confirmed all five changed files match. The initial Unity attempts that produced no XML exposed two compile errors; both were corrected before the passing runs and are excluded from passing evidence. `git diff --check HEAD^ HEAD` passes.

Protected run manifests and reports remain in the private E: evidence store. This record exposes only run identities, result totals, and manifest digests. Unrelated `ProjectSettings` modifications were not staged or committed.
