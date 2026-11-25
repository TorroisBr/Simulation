# P12-E Core Justice owner restoration validation

## Candidate identity

- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Implementation commit: `6ecb3f8d14c596cb171dd5e715fb300ab9ecef0f`
- Implementation tree: `7e2a92ff719067893dc54c5cda6279363b928b39`
- Parent: `e8496bda09fe321d804b711571949ff668c82ce0`
- The implementation commit changes only the seven Core/Unity restore and test files reviewed for this slice. The pre-existing ProjectSettings edits, untracked user files, and protected evidence were not staged.

## Results

- Official CoreValidation: **865/865 cold and 865/865 warm PASS**, SDK `9.0.315`; the fixed CalendarDemo and Simulation.Lab gates also passed. Private run ID: `f6396b149bf840a79f07ee467b62b85f`.
- Focused Unity `P12EJusticeRecordsOwnerSnapshotTests`: **11/11 PASS**. Private run ID: `85569f5214164cea8cfc7303b728a4b7`.
- Full Unity EditMode: **3271/3271 PASS**, Unity `6000.3.9f1`. Private run ID: `dbf26d60143b48cfbda94c1a3326722a`.
- Official filtered Smoke (`EditMode`, filter `Smoke`): **5/5 PASS**. Private run ID: `b692e7ae5f6c4536b2b50300ba8eaf87`. The five Smoke cases also appear in the full EditMode report.
- The selected Unity validation inputs were stable before and after the full run; input digest: `44a0bb63071c10fcfa5fb08872f7859f6c7bf1001cc9b3b513bd5b076ce5a954`. The candidate source hashes were checked against the captured run inputs after the implementation commit.
- `git diff --check` passed for the implementation. The worktree still contains pre-existing, unstaged ProjectSettings edits and unrelated untracked data.

## Reviewable artifacts

- The privacy-safe full EditMode package is [coref-p12e-justice-core-restore-editmode.zip](coref-p12e-justice-core-restore-editmode.zip), SHA-256 `c333210dcf3c81844592c27aa9b3aa6a1ea2bbba3c358fd85b0a6b2efa1ccde3`.
- Its manifest is [coref-p12e-justice-core-restore-editmode.manifest.json](coref-p12e-justice-core-restore-editmode.manifest.json), SHA-256 `b08e691abc816914eceabadd15409b00f9fb35965074bac394e1b3c8bf7ea011`. The approved packager verified the package against the protected original run manifest (`runs=1`, `entries=1`).
- Original CoreValidation manifest and cold/warm TRX reports remain in owner-only E: retention. Manifest SHA-256: `cd16f37a0aea61893282a27eea1fde7246516397537826c908137de34c2d0943`; cold TRX SHA-256: `7e20c0c266e01391e5d570ccb2a5c39bfba4b15daf455a35348a67c02641357d`; warm TRX SHA-256: `60aa2bdb0c83161445bd371791203a9eaf6812a2ce0df60cc007d896c60c3696`.
- No public .NET TRX package is claimed. The runner emitted `simulation-core-validation/v1`, while the approved DotNetTrx packager requires the separate `dotnet-trx-source-context-v1` context; passing the runner manifest directly was rejected, and no context was fabricated. Original TRX and logs were not modified.
- The CoreValidation run reports that this worktree contains Unity `Library`; this evidence does not claim .NET-only or Library-independent execution.

## Scope and status

The tests establish that P12-E reconstructs one Core Justice owner from the validated snapshot and exact staged Core City/NPC/Person roots, then projects Unity references onto that same owner. They cover ordered wanted/sentence rows, reciprocal Person bindings, exact revision restoration, root ambiguity, target/City/Person mismatch, and warrant identity. This is an implementation checkpoint only; it does not claim completion or canonical promotion of CORE-F.

Independent exact-tip implementation and evidence reviews passed for candidate
`15b81cffe172b4031c4cbd0e362c28e3b290d376` (tree
`1774fecc5964fe9c3e9d5a1eb3052c8c5b673cba`); the durable findings are in
[`INDEPENDENT_REVIEW.md`](INDEPENDENT_REVIEW.md). The code tree remains the
implementation tree recorded above.
