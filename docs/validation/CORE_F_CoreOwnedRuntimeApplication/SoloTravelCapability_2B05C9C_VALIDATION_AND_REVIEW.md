# CORE-F solo-travel capability selection validation and review

## Candidate identity

- Candidate branch: codex/core/CORE-F-core-owned-runtime-application
- Implementation commit: 2b05c9c7b5fe1ac4cc5f80ef962cc32f40870681
- Implementation tree: d668a733f251febda4a1b890c81922366aa42ef3
- Parent: aafce277a2ad4e3a9dfac12c4a7777f913dcf700
- Current remote Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Candidate merge base with remote Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Changed files: Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs; Assets/_Project/Tests/EditMode/Editor/ContinuationCensusProtocolTests.cs.
- This is a candidate implementation checkpoint only. It was not promoted to Core canonical and does not complete CORE-F.

## Validation

- CoreValidation: cold 907/907 and warm 907/907; run ID 8b9ef13318b0454c8008404cd9a65ea2; evidence manifest SHA-256 b91061e6b698ca29a237058314073e66c4bb655d25b50529233b831f0eda904a.
- Focused Unity EditMode SelectedSoloTravelAvailabilityComesFromSealedCoreOperationInventory: 1/1; run ID 0300e451fed04cd3b25eb636af777db0; manifest SHA-256 530bd665a930cbd8aae2094a56b46acd45ec7be5ca927c370f1d6e9c1fcd8f56.
- Full Unity EditMode: 3276/3276; run ID d8150c55ac8048c9a0442cf916d205a5; manifest SHA-256 064cc53ec447d167965a34ad4de6771290b480fff556f2c5d297aec504b5386d.
- Official Unity Smoke: EditMode filter Smoke, 5/5; run ID be54675c9eac449ca804fc893395b34b; manifest SHA-256 7393d514d07ca523af9dcef101d27e6a2c01e6c14aea49d320cd3119096cd156.
- All Unity input snapshots were stable. The CoreValidation manifest records the exact changed Core source hash and the run's current test source/worktree inputs. It reports that this checkout contains a Unity Library; no Library-independent claim is made.
- git diff --check: PASS before the implementation commit.

Original XML and raw logs remain in the protected local validation store. Run IDs and manifest hashes identify retained evidence without publishing workstation paths or raw logs.

## Independent exact-tip review

Verdict: VALIDATED_CANDIDATE — PASS; no actionable findings. The independent reviewer inspected commit 2b05c9c7b5fe1ac4cc5f80ef962cc32f40870681, its two-file diff, current remote Core canonical ancestry, and the supplied exact-content manifests. The reviewer did not rerun tests or edit candidate files.

The review confirmed:

- When the execution state's operation inventory is sealed, Core selects solo travel from the existing runtime.travel.start contract. The host-reported component-presence value is used only when no sealed inventory exists.
- The operation contract is an existing conditional entry in the complete Core catalogue; no operation or profile contract is broadened.
- The new regression checks both disagreement directions: host reports travel while Core omits the operation, and Core admits the operation while the host reports unavailable.
- ProjectSettings edits, untracked work, and private evidence were not included in the commit.

## Scope limits

This checkpoint makes the admitted Core session authoritative for selected solo-travel stage availability while retaining the pre-inventory compatibility fallback. It does not remove Unity's host-specific travel projection/event adapter. Shared application startup, full detached hosting, P12 restoration integration, and separate-process continuation remain incomplete. No CORE-F completion, canonical promotion, or broader runtime ownership claim is made.
