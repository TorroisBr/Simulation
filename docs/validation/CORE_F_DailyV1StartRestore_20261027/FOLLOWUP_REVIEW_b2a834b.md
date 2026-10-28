# Independent follow-up review: CORE-F Daily-v1 initial restore admission

## Verdict

**VALIDATED_CANDIDATE** — The invalid-initial-host fail-closed finding from the earlier review is resolved at this exact tip. This review does not approve canonical promotion and does not claim CORE-F completion.

## Exact identities

- Repository: https://github.com/TorroisBr/Simulation
- Candidate branch/tip: `codex/core/CORE-F-app-start-restore` / `b2a834b23a9594a7a5d2fc04a6e762660b13da1b`
- Candidate tree: `62bcb66721f23f4323bbc92818da556e96e28cad`
- Exact base: `f054622b75ffbcd4b932f512f675a6df485193df`
- Core canonical reference: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate branch was verified at the stated tip during this review.

## Scope reviewed

Reviewed only the two-file candidate change:

- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `tests/Simulation.Core.Tests/DailyV1ApplicationHostTests.cs`

The production change records and blocks an invalid initial host attempt only while the application has no prior initial-load/genesis attempt, prepared candidate, active session, or active operation. The test asserts that an invalid host returns `InvalidOwnerValue`, clears the image output, and prevents genesis on that same application.

## Findings

No actionable findings.

The invalid-host branch keeps `InvalidOwnerValue` precedence and, under the session-gate lock, marks the one-shot initial continuation attempt and sets `initialContinuationLoadBlocksGenesis` only when the application is otherwise unused. This closes the previously reported bypass in which invalid host input returned before the block was set.

The admitted NoSave behavior remains intact: a valid initial load that completes with `NoSave` records that genesis may proceed. Present-but-invalid slot handling and the rest of the load flow are unchanged. The regression is additive; existing assertions are not removed or weakened.

## Validation evidence and limitations

The task supplied these results for the exact code tree:

- CoreValidation: 899/899 cold and warm; run `8cfb11b0e3c04b1aaebb6d527d90facc`.
- Lab: cold and warm PASS.
- Unity EditMode: 3274/3274; stable input digest `975c2519386c2ef4cbaea408dfc72ab36e6b7f2efa8e8c40371ebcd1a2149597`.
- Smoke: 5/5, same stable input digest.
- CoreValidation cold/warm TRX SHA-256: `45058f2f1dbe8682c9ece876d8bcdf07f62a02391b8781249adea268c3e38a86` / `3adec0e2f37328a0977fde52e3d3c84aa52aed90e148e4335ff978a5ebf05362`.
- Unity EditMode/Smoke XML SHA-256: `deb5b2b6b33a75801526e3f40a1d7c19e04cf927bf61e8b6c18a69a379852ca1` / `aae6de836d576d87e40262da62e39b32c127c2a163057bcef7e2bd1af80c4101`.

I independently inspected the exact GitHub commit diff and confirmed its two-file scope and tree identity. The protected E: validation manifests and report files were not accessible to me; therefore I did not independently recompute their hashes or rerun tests. The validation results above are recorded as supplied evidence, not independently rehashed evidence.

This review does not assert full CORE-F acceptance, canonical promotion, or completion of detached-host continuation.
