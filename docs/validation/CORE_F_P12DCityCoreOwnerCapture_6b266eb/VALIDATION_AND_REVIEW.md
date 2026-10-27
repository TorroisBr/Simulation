# CORE-F selected City owner capture — candidate evidence

**Status:** `VALIDATED_CANDIDATE` for this bounded capture integration only.
This does not promote Core canonical, complete P12-D, or complete CORE-F.

## Candidate identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`
- Candidate code commit: `6b266ebf575709337a9b79a45e25df621b00b197`
- Candidate tree: `b65671b5ef7ff4de87c43def990eb808d75656db`
- Candidate parent: `269f4f91317822d362a304648059b26eaa49b6fe`
- Core canonical at validation/review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Exact 15-file code/test patch SHA-256: `6bb72ee3e0b144880795b17ddccfc5d8fa4f766f`

The committed code/test patch is byte-for-byte the patch that was validated and
reviewed before commit. Only the listed 15 implementation and test files are in
the code commit. Three pre-existing `ProjectSettings` edits and local untracked
validation evidence remain outside it and were preserved.

## Scope

The selected P12-D City capture path binds its four required City sections to
the exact Core City owner instances in the active selected runtime graph. It
revalidates the P12 continuation-save boundary and the census identity,
cardinality, revision, and alias evidence. Capture rejects excluded City
daily-economy receipts and P14 local material-flow state rather than silently
omitting it. The selected Market regression adds a graph-admitted item with no
stock row and verifies the Core owner, Unity projection, census cardinality,
and revision together.

This is a partial P12-G/Core capture input. It does not claim complete City/NPC
capture, complete owner coverage, detached-host readiness, P12-A readiness,
P12-D or P12-G closure, or CORE-F completion.

## Validation

All results below are from the same reviewed 15-file source/test patch before
its commit. The recorded source input remained stable during each run.

| Gate | Result | Run identity |
|---|---:|---|
| Official CoreValidation cold | 891/891 PASS | `ac8b780a43514470ad8ea85c07bd36f2` |
| Official CoreValidation warm | 891/891 PASS | `ac8b780a43514470ad8ea85c07bd36f2` |
| Focused City Market census | 2/2 PASS | `3c2c3d53269547899aa1c6d98500b96f` |
| Full Unity EditMode | 3272/3272 PASS | `c4e4a265b4e943778fe78a8b1eafc39c` |
| Official filtered Smoke | 5/5 PASS | `e213886326e8449d978a3a6a39fc2013` |
| Official LongRun | 7/7 PASS | `d19827bf0b5f42abba4759917c081757` |
| `git diff --check` | PASS | exact code/test patch |

The CoreValidation manifest records source commit `269f4f91317822d362a304648059b26eaa49b6fe`, committed tree `fc61fe07df8bac2803ee337d327bfa4ddd766a97`, stable working-input digest
`04bccb1409a0753b5d76cbbffb2c0fd68de552c2a050c46385ba94c7ff3dce1c`, and
891/891 cold and warm results. The Unity runs used the stable candidate input
snapshot `5afb32bb69a7d0115f626a7d1524b72275f34dc123249de424adc31d21132342`.
The full EditMode result-only package was verified by the approved packager;
archive SHA-256:
`5e715c5e99b1cfe0b0637ba28da2c7c3a8593c9a68baf38a6e2a6813e16638d8`.
Protected originals and private run manifests remain in local owner-only
retention; they are not copied into this commit. The older `.validation`
snapshot with a partial 3267/3272 result is not evidence for this candidate.

## Independent exact-tip review

**Verdict: PASS** for the code commit `6b266ebf575709337a9b79a45e25df621b00b197`.

The independent reviewer checked the complete parent-to-candidate patch,
confirmed the exact 15-file patch hash above, confirmed the remote candidate
ref and Core-canonical ancestry, and found the two earlier findings resolved:

1. Core-bound City capture now rejects nonzero daily-economy receipt count or
   revision, configured/local P14 material flow, a finite source, or a recorded
   material-flow result. Tests cover a real P18 receipt, an empty but advanced
   receipt revision, and P14 local-flow state.
2. The selected Market census test proves the new admitted item is reflected by
   the Core owner, Unity projection, and matching census cardinality/revision.

The reviewer did not rerun tests and did not use the stale local `.validation`
result; the run identities and exact-source evidence above are the validation
basis. This review is not canonical promotion or Phase closure.
