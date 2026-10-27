# Independent review: CORE-F Daily-v1 initial restore admission

## Verdict
**NEEDS_CHANGES** — one fail-closed defect remains in the Core application API. This review does not approve canonical promotion and does not claim CORE-F completion.

## Exact identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch/tip: codex/core/CORE-F-app-start-restore / f054622b75ffbcd4b932f512f675a6df485193df
- Implementation commit: c6e5b73f518d2b5f67692ccaa3a656415fbf357f
- Implementation tree: 0c0b6ead7809acff372810f84d0ae34407fdf720
- Review tip tree: d4b61679606f205466bd93fde07d605f800f1d88
- Exact base: d27cb342b38bd8b841a325dc7b1c6efbe4640adf
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea
- Remote comparison: candidate is a clean fast-forward, two commits ahead and zero behind the base.

The implementation commit changes exactly three source/test files. The later validation commit adds only the validation record and its Unity package/manifest.

## Finding

**Fail-open after invalid initial-load inputs -- actionable.** In Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs, TryLoadInitialContinuationImageForStart returns InvalidOwnerValue for a null host or invalid host ports at lines 751-758, before setting initialContinuationLoadBlocksGenesis. TryPrepareInitialGenesis rejects only when that flag is set. Consequently, the same application can receive a failed initial-load attempt and then admit genesis without a NoSave result. This contradicts the accepted startup rule that NoSave is the only load outcome that permits genesis.

Add a regression that calls initial load with an invalid host, asserts InvalidOwnerValue, and then asserts genesis is rejected on that same application. Keep the production Unity caller's explicit probe-error failure behavior.

## Reviewed behavior and scope

- For completed slot reads, the implementation blocks genesis after a loaded image or any result other than NoSave; corrupt-slot rejection is covered on the same application. A successful NoSave result permits genesis.
- The Unity caller replaces the direct load with the Core application call for a present slot. Existing restoration staging/publication remains in place; this change does not modify owner hydration.
- Test changes add cases and assertions; no existing test assertions were removed or weakened.
- The Unity bootstrap still performs the slot-presence probe and profile construction. The validation record lists moving that probe/admission behind Core as remaining work. This is a stated limitation, not included as a second finding.
- This is only bounded initial continuation admission. It is not a complete DailyV1Application.TryStart, Core-owned active runtime, detached-host completion, or CORE-F acceptance.

## Source and evidence identity

The three candidate Git blob IDs and the recorded CRLF worktree SHA-256 values match the fetched committed files:

| File | Git blob | Tested worktree SHA-256 |
|---|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs | b839a84a676cda230e2c6abbd504eff1834c3ac8 | 8b9abd46281c0ff7939fdaa629d8009be785db2296e3d994b96efb0566adb4d6 |
| Assets/_Project/Scripts/TesteSimulacao.cs | c120a3788da446afba1cd7d4245a2b9147fbbf85 | 8c52e01357b0264c78125f4905c63bea9b409083825d9d7f4590f17c71d3ba04 |
| tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs | d290cd6d6d5803386588aa4b479f42f41c395694 | 6eb7cd96fb5bf2eb2aebaa2d97aff9230b830158d3d4c5a660b5c2df5b22cb33 |

The official evidence record reports CoreValidation cold/warm 898/898, focused application-host tests 6/6, targeted Unity restore regressions 2/2, full EditMode 3274/3274, filtered Smoke 5/5, and git diff --check PASS. The Unity run manifest identifies run 8b068be7fc064f02bbb10a9ba535e79c, stable input snapshot 417ffa8d12ee4418688110529281fac9c57a53197dea90ea2645ad0375c72712, Unity 6000.3.9f1, and the two candidate Unity-source overlay hashes above. The Core test file hash is recorded in VALIDATION.md; it is not part of the Unity input snapshot.

The public Unity package manifest SHA-256 is 05ed6e37d1fc11216dd191d07f904b5022a79d87e78c465b48959a0fdad9dd6; its archive is 516eac9c0e37c08187a543931419a6aab2fc4468f0cbfcb778689a8bdb2ced2e, 173139 bytes. The archive hash and size agree with the manifest, which describes one normalized test-result XML entry. The manifest scan found no workstation paths or credential-like strings.

The protected CoreValidation manifest/TRX/logs were not accessible for independent inspection and are not in Git. VALIDATION.md explicitly retains them in owner-only local evidence; its run ID and cold/warm report hashes are recorded but were not independently rehashed. No raw evidence was copied into this review branch.

The candidate commit contains no ProjectSettings edits or unrelated metadata. This review record is the only change on the separate review branch; canonical remains untouched.
