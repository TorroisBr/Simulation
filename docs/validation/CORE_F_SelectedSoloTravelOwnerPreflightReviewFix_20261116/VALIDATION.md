# Selected solo-travel preflight review fix

## Candidate identity

- Code commit: `e8ba6dbb175747fa2cff0ef5f37ccbd901f6c580`
- Code tree: `b03cad617f64d1941484d0302d67a58b97ce62df`
- Candidate parent: `3091b4bd7b5cd3b0668555aa160ee3c22409cfc4`
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Scope: exact Core session and owner preflight, plus the Unity presence-projection revision-capacity check before a solo-travel charge. The legacy host-domain preflight remains on the non-Core path.

The changed Core/test files were committed byte-for-byte from the tested worktree. Their hashes were compared with the successful CoreValidation input manifest after commit. The three Unity production files in the change were likewise compared with the successful Unity input inventory. The candidate code tree is therefore identical for every changed executable/test input covered by its applicable runner.

## Official CoreValidation

- Run: `8fc44c121c894d7a89687425d6b60dbe`
- Result: **PASS**, cold `1136/1136`, warm `1136/1136`; no failures, skips, inconclusive, errors, aborts, or timeouts.
- SDK: `9.0.315`
- Stable input digest: `3abbfe1d69b0ee396f5359a02ea8f7fc2ca22f6ca6282e7757c5a7c9e8d78f60`
- Private original manifest SHA-256: `9f6686207ad83b6da7736107ebdfa243c79cc1cf20d3345cf069c7be7dae4d80`
- Private cold TRX SHA-256: `e2f1cbf20fcad49a19e6f350b79a23c008e6033ddcb148e2b62e2ab7507c4206`
- Private warm TRX SHA-256: `47600b0a2c75a4d0852d36e91f3cbe7baad412edb394ac451df75feebe49ab65`

The complete Core manifest and original TRX files remain in the verified owner-only evidence store, identified by the run ID above. They are not published as raw TRX. This default run includes the additional Lab gates, while the closed TRX package profile accepts the narrower Core-only run shape; no report was rewritten or repackaged outside that profile. Consequently, the .NET result artifacts remain private for this candidate checkpoint and are not offered as a permanent promotion package.

## Official Unity EditMode

- Run: `0c4a3ea0379e4b3da76a2936cc0677d3`
- Unity: `6000.3.9f1`
- Selection: all EditMode tests, no filter
- Result: **PASS**, `3321/3321`; zero failures, skips, or inconclusive cases.
- Stable Unity input inventory digest: `e411c23e3878334ecaafb621bb3be05f45bf8cbd2ae4e03699a0d3d1ae49386d`
- Original XML SHA-256: `87f8beb6b32862027802a88040a6f45f8d4b869107dbde311e583b5197b87622`
- Result-only package: [manifest](./Unity/coref-solo-travel-owner-preflight-reviewfix-editmode-20261116.manifest.json), [archive](./Unity/coref-solo-travel-owner-preflight-reviewfix-editmode-20261116.zip)
- Archive SHA-256: `17828e4f3ee8ad2eb0140335d417f97386d285160515c94d04ad74947b190251` (`176059` bytes)
- Official package verification against the retained original: **PASS**, one run, one result entry.

The full EditMode result includes the five Smoke-designated cases; all five passed. An earlier separate Smoke-only filter attempt timed out without producing XML and is not used as a passing result. The full, unfiltered EditMode run is the evidence for the five cases here.

The worktree had pre-existing local ProjectSettings changes. They were left untouched and excluded from the candidate commit; the Unity runner's before/after inventory was stable and binds the actual settings input used. The original Unity XML and raw log remain in protected local retention; only the verified result-only archive is included here.

## Additional checks and limits

- `git diff --check`: **PASS** on the candidate source/test changes.
- Independent exact-tip review: recorded separately in `REVIEW.md`.
- No canonical promotion is claimed. CORE-F remains incomplete.
