# CORE-F Demographic Owners — Independent Implementation Review

**Verdict:** `VALIDATED_CANDIDATE` / `PASS`

**Review date:** 2026-10-15
**Reviewer independence:** This review was performed independently of the implementation author. The reviewer did not author or modify the candidate code, tests, or validation evidence.

## Exact identities and review scope

- Core canonical base: `0094febf7f97333d0653eaf9688fe1d24d73327f`.
- Reviewed code commit: `9c1ed5e2065f2d509b61e63b39fb6c84508fd5fe`.
- Reviewed code tree: `f9934338811f7272c8f7013f5bbda14e06c4a92e`.
- Evidence successor reviewed for applicability: `7370145471d0c5604e171fa6a51fe62ce6619214` (tree `f259191037f9c8cbfd7d1ce5a1971134b96116a1`).
- Approved architecture/handoff baseline: `a20978d2b61e07a4b131afbb3971aae346511acb`.

The full 63-file implementation diff from the canonical base was reviewed against `CORE_F_DEMOGRAPHIC_OWNERS_MASTER_HANDOFF.md` and its approved owner designs. The evidence successor adds only the validation record and the four referenced package files; it does not change code, tests, project configuration, or the reviewed code tree. The review remains applicable to that evidence tip.

## Implementation findings

No actionable implementation findings.

The implementation moves the approved seven aggregate-population source files and two genealogy source files into the shared Core package. Unity adapters call those same authoritative owners; no duplicate simulation owner or new general-purpose setter was introduced.

Mutation health is observed through the shared read-only status interface. Population and genealogy owners bind to the exact existing runtime guard object, preserving one-way same-instance binding. Population P12 callbacks retain their owner reference, preflight/commit order, and flags. The battle rollback point has an internal constructor, is owner-bound, rejects a different owner, and preserves the existing receipt-pruning rollback behavior.

P12 codec and snapshot adaptations forward through the new bridges while retaining existing transition construction, validation, and serialization contracts. Genealogy restoration uses exact export/staging only for restored continuations, preserving the captured local revision. The ordinary constructor clone retains its prior replay-and-normalize behavior. The restoration regression exercises the real P12 continuation path and checks both branches.

The change does not claim complete CORE-F runtime composition, detached .NET hosting, persistence-store integration, or deterministic cross-host continuation. Those remain separately gated.

## Validation evidence

The durable validation record is [VALIDATION.md](VALIDATION.md). Package manifest and archive hashes below were independently checked against the recorded values.

| Gate | Result | Durable evidence |
|---|---:|---|
| CoreValidation cold | 188/188 passed | .NET package manifest SHA-256 `7e875adf9d5ea4ed6e1b8c92f1061c936765399c07e545148e6e42e70f2035dc` |
| CoreValidation warm | 188/188 passed | Same package; exact code commit/tree and stable input digest are recorded |
| .NET original-report comparison | PASS | Normalized cold/warm archive SHA-256 `1513b163b861e8cbfb7868fb2d3a4ca5ec11e2d2013f2dde1d400b5636a100d0` |
| Unity ALL EditMode | 3,177/3,177 passed; zero failed, skipped, or inconclusive | Unity manifest SHA-256 `e880f97406da7cce8f9adb816a2694c6e2ae7accc1d861f1df5335f176b66a14`; normalized archive SHA-256 `f8903c1caeda8d6a1b9ea811ee6dd20eefd65f53ae99293a993a2b45ca4ec596` |
| Focused owner coverage | 17/17 passed | Included in the validation record and complete Unity report |
| `git diff --check` | PASS | Checked through evidence tip `7370145471d0c5604e171fa6a51fe62ce6619214` |

The .NET package records code commit `9c1ed5e...`, tree `f993433...`, stable input digest `7fc022921404dfcce5e2e1f5162c23aa465fc8766d94ef344cfe1b1f17cd7882`, and the cold/warm original comparison. The Unity ALL package binds to the same commit/tree and a stable 1,659-file input snapshot; its normalized report was compared with the protected original.

### Filtered LongRun and Smoke evidence limitation

The standalone `SimulationRuntimeLongRunTests` run passed 7/7 and the official Smoke run passed 5/5. The approved Unity packager fails closed on both filtered XML files because retained ancestor/fixture `testcasecount` metadata reflects full discovery while the XML contains selected descendants. The original files were not edited or published, and these filtered reports are not represented as normalized packages.

An independent read-only comparison confirmed that all seven LongRun and all five Smoke case identities and `Passed` outcomes occur in the normalized, original-compared ALL EditMode report, with no missing cases. The Smoke subset treatment follows the established CORE-F evidence precedent for filtered Smoke cases. The same identity/outcome comparison was performed for LongRun, although the precedent did not specifically address filtered LongRun packaging. Neither comparison waives the filtered-package failures or claims that either filtered XML was normalized or independently verified as a package.

## Review disposition

`VALIDATED_CANDIDATE` applies to the bounded demographic-owner implementation at the exact code identity above and to the evidence-only successor after confirming its code tree is unchanged. It does not itself promote the candidate or close CORE-F. Canonical preflight and the repository's separate promotion procedure remain required.
