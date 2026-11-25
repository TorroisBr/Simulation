# CORE-F host-neutral Justice census witness

## Candidate identity and scope

- Base candidate: `71c48a255b9e28d3142cf57650d5b5cc3ae66afd`.
- Implementation commit: `cbacf61759a6403c316148c9a3a15b4c5fa2b073`.
- Implementation tree: `e57c6ba1b0193e0eea1dfea8fcf81daf1640c7ea`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

This bounded change lets Core compose the P12 Justice records and P18 receipt
census rows around one ephemeral host-supplied identity. Core derives records
cardinality and local revision from the exact selected `JusticeRecords` owner;
the host supplies the receipt revision witness. Both rows retain the existing
shared-identity invariant. No P12 contract or persisted identity changed.

The General Architect's focused compatibility review passed and found no
reconciliation necessary: [review record](../../reviews/CORE_F_JusticeCensusHostNeutralIdentity_ArchitectureReview_cbacf617.md).

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Focused Justice census regression | 1/1 passed | `CoreRejectsJusticeCensusThatDoesNotMatchItsExactOwnerAndProjection`; exact source is also included in the official cold/warm runs below. |
| Official CoreValidation cold | 1,078/1,078 passed | Run `3d97c194e2374835af3bfdaed6ccb1ac`; protected manifest SHA-256 `6a412294e7e9b0ec4ab2fdbd82bf556198c4e9cd3a7819400afa91caf992d5e0`; original TRX SHA-256 `659c79e81e525cd3e403efdfbcf1a6d860419126c7b7f466899cafa76e0e080b`. |
| Official CoreValidation warm | 1,078/1,078 passed | Same run; original TRX SHA-256 `2d960fc6d471e60e5fe296bd33b209f923677c82bf8b43a5b86f8d8514643154`. |
| CoreValidation source/dependency stability | Passed | `inputs.stable=true`; generated dependencies stable. The worktree had a Unity `Library`, so this is not a Library-free-run claim. |
| Full Unity EditMode | 3,303/3,303 passed | Stable run `2ebf81e1c35844d18ac5e6097b2ab8dc`; original XML SHA-256 `baa4b140837faa01f2b4eb8ea06f6dc1c421ac3a0375817dae82f380cf3bdb38`; raw log SHA-256 `3bafd932ee9b73ae7183d944148ca43acde0f564b141764e5fdc358ceaed920a`. |
| Official filtered Smoke | 5/5 passed | EditMode filter `Smoke`, run `0fe237cc493f4720859d61221a71a8c9`; original XML SHA-256 `a1f932c548442952ddd5030e9dd1b583e99c4668727a7a8f5a366804f2248506`. All five cases also occur in the full EditMode package. |
| `git diff --check` | Passed | Passed on the staged source diff; `git show --check` also passed for the committed implementation diff. |

The privacy-safe full EditMode package is in [`Unity/`](Unity/). Its archive
SHA-256 is `398bf46aca556fc9a115d8d521b3e8525ffd80b8a781096a9c173a5615f8fbec`,
and its manifest SHA-256 is
`047f5508097d9c2b6a0773d7bc090cdadc7d4312e50446d460e6e6adde1140bf`. The
official verifier accepted it (`runs=1`, `entries=1`) against the protected
run manifest. The filtered Smoke XML remains in protected local evidence; no
separate filtered archive is claimed.

CoreValidation's protected originals remain under its private run ID. No
public .NET TRX package is claimed: the public TRX adapter requires a
`dotnet-trx-source-context-v1` record, and none was fabricated for this run.

Unity's first project import rewrote line endings in ProjectSettings files, so
that run's input-unstable evidence was rejected by the packager and excluded.
The stable EditMode and Smoke runs above occurred afterward against the same
candidate code tree. A prior PlayMode filter matched zero tests and is not
counted; the accepted Smoke run is the EditMode `Smoke` filter shown above.

## Limits

This does not implement or prove the complete detached .NET host, does not
change P12 persistence semantics, and does not complete CORE-F. Canonical
promotion and full application/restore acceptance are not claimed.
