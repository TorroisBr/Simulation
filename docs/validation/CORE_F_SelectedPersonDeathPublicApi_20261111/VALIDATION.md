# CORE-F selected Person-death public API validation

## Candidate identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base commit: `f6f09fd44ad09944a03e04ba6a65885fe41969b9`
- Implementation commit: `e60492e3caf7e7eeac4404e7a451a945e5db2e11`
- Implementation tree: `d52901955188b3b61a97785683f87fa958c870ef`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Official input snapshot: `1bc2c4d35c874688eb7ef826b12a758c95e14eeb413ab1a311daaa164375571e`
- Snapshot stability: `true` for all three passing runs.

The tested files are the committed `SimulationRuntime.cs` and
`P12PopulationLifecycleInvalidationTests.cs` blobs. Their run-manifest input
hashes match the worktree bytes, and Git's clean-filter object identities match
the candidate blobs.

## Official Unity results

All runs used Unity `6000.3.9f1`, Test Framework `1.6.0`, and the repository's
official PowerShell 7.6.5 evidence runner. Original XML and logs remain in the
owner-protected local evidence store; no original was rewritten.

| Run | Result | Run ID | XML SHA-256 | Raw log SHA-256 | Run manifest SHA-256 |
|---|---:|---|---|---|---|
| `P12PopulationLifecycleInvalidationTests` | 23/23 passed | `e82e573428ce4eb28acc411a38477d77` | `308a4e29d0214706420daf54611a0f3b80bd0038770260fd7bd17147af528029` | `e57d234b87c8824797d6da19c511aa7919af8a4c335a37fdd5e2e6f919dffcc2` | `78cadd67aa3037eb721b4bf406bb0cd42aefb64939792b8ac0b690a641b121c5` |
| `PersonNaturalMortalityFoundationTests` | 15/15 passed | `fdf78e275e4a4f728c628eb7540b18ed` | `d97961f6f3107e54e855a18ba267fae42ab8821efc6c9efc0a938727d56e38c2` | `c8ab40dea9adfc3251a1ce72c75870445915a0a52371e48ea1fd05729744b7ac` | `dfca605014bccc6f1e85b5a18256f194ba1ea661fb323789f38b093b3da06aa6` |
| Official `Smoke` filter | 5/5 passed | `07609ec5992b4625af0c03b9c646b9d2` | `c992a7e948a849bab6f9c101ba3cb926f61a6b911afe8e079c0ce2b3f6b5092e` | `005608787c8208630b1ca4433a727abe013d26c06612bd97994d0bda29bd43e7` | `7ef2be946f9b6eb40003ca5975308ebc300de59cc2e1309c7f7a61b92397a7cb` |

`git diff --check` passed for the implementation change.

## Full-suite attempt and limitation

The official unfiltered EditMode run `87c863b903f24acdbd51717c6bd51634`
ended with runner status `NoResultXml`: zero tests were reported and no XML was
produced. Its manifest SHA-256 is
`6bc8c927512ad7a7e9bca35fc4a204841a5f53056b2b905725a498b185424dd5`; its raw
log SHA-256 is
`c03093aa8abe4d4f483eff86eb6e3225d608593d17c355b422f419975992fc24`. The
runner waited about 16 minutes (exit code `-1`, before its configured 120-minute
timeout). Unity licensing, project load, unfiltered EditMode setup, and many
tests/log entries completed. The last visible log sequence was
`GMConsolePanelTests.SelectedObserverPlaceCanPopulateCommandTarget`, followed
by a scene load and managed-domain reload. Only that run's verified child
process was stopped so the official runner could record `NoResultXml`. The log
does not establish whether the interruption was caused by a particular test,
the all-tests run, or the final reload. This is an operational no-result, not a
test assertion failure or pass.

The run's two changed-source input hashes match the bytes later committed at
`e60492e`; the commit's clean-filter hashes match both source blobs. The full
EditMode suite therefore remains unvalidated for this candidate. This
checkpoint does not claim full CORE-F acceptance, canonical promotion, or
completion of the shared application/host work.
