# P12 Core restored-boundary finalization validation

## Validated source identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Validation baseline HEAD: `881d4beb` (the preceding Core-stage review record).
- Exact Unity input snapshot digest: `372e209f1cee5f2ece4e26da0477fec6886941b8aa93be0c15fd250ab4a006d7`.
- Changed production source SHA-256: `P12GDailyV1RestoreCoordinator.cs` — `96671882a6d289d08cfedd5bce7ccc77592f733b374015252392d4658a6b6fdc`.
- Every runner manifest reported `inputStable: true`; all three used the same input digest.

## Bounded change

After P12-G validates the private capture-bound target graph and owner vector,
the application path now calls
`DailyV1Application.TryFinalizeRestoredCandidateBoundary`. The Core application
binds the exact host ports to the Core session, binds mutation guards, performs
the required owner-vector recaptures, and admits/publishes the restored
completed-boundary token. The target projection is checked again before the
restore returns. The `application == null` compatibility/test path keeps its
existing local finalization. Source-boundary, staging-attempt, and later
projection publication checks remain in place.

This does not move all P12-G capture, codec, image reconstruction, target
graph checks, or host projection construction into Core. It does not deliver
`DailyV1Application.TryStart`, a projection-free detached .NET host, separate-
process T/T+1/T+2 parity, or full CORE-F acceptance. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Validation

| Gate | Result | Evidence identity |
|---|---:|---|
| `SimulationRuntimeAdmissionTests` | **241/241 PASS** | Private run `31a23ef66f3b4f0d82f0d70d2363e80b`; private manifest SHA-256 `bbe8c6a198c77ccf9509b4eaf475bc8ace506f7db0f7c0ad9cf9a650a5c53573`; original XML SHA-256 `ec634d8a7d28276825d4fb442af4628dd42f73d1985af40ee989b1a5cd7ad88c`. |
| Full Unity EditMode | **3294/3294 PASS** | Private run `1f43da462b5e436591ee3739c9b29ac4`; private manifest SHA-256 `01edf37d03af54a50d747641dc601c171b3fd4b325d66a101c3ebeda28d2d7da`; original XML SHA-256 `d2ac1e961b8e91a0daa5f4d2448da678d36e4d9d77f9b0c565ef0ed1de7edfc4`. The normalized package is in `FullEditMode/`; private original comparison and archive verification passed. |
| Official EditMode Smoke filter | **5/5 PASS** | Private run `c5dbeb580c6947b9b098b1ae58776886`; private manifest SHA-256 `dfe18c5aaee6b6b7e4a5d4e682048d9d0e7954a97856aadf27432531681c0f1f`; original XML SHA-256 `3ba65ef6caadfe3888867b3d023b5b6b4da3f0fc06b01100fdd39ef2d7421a79`. |
| `git diff --check` | **PASS** | Checked on the candidate source diff. |

The full public package has bundle ID `p12-core-finalization-full-editmode`.
Its manifest SHA-256 is
`56830d3d1a3d6a2bed62d673755bc95a57edf40959ca2a72228eeaccc9fba092` and its
archive SHA-256 is
`4a8a15b539aa5a95e102a66c092a6bc1492d7fbf0d9a4e5cc2b95ea4ac254bb9`.
Verification returned `VALID` for one normalized result entry.

Filtered original XML and manifests remain in protected local retention.
Their results were not rewritten or repackaged; the full normalized report
contains the complete test tree, including the filtered tests. The focused
filtered case count is extra coverage; the full suite is the publishable
complete result.

Core source and Core tests did not change in this increment. The preceding
checkpoint's CoreValidation result remains 988/988 cold and warm; it was not
rerun solely for this Unity-coordinator-only change.

## Preserved boundaries

- Existing P12 source token, staging-attempt and whole-target validation
  continue to gate restoration; Core does not publish before those checks.
- Existing Unity projection checks and compatibility path remain intact.
- No tests or assertions were weakened.
- ProjectSettings, `TesteSimulacao.cs`, unrelated untracked files, and prior
  protected evidence were not staged or modified by this checkpoint.
