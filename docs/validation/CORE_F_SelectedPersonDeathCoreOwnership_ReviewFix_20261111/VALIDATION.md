# CORE-F selected Person-death runtime-boundary review fix

## Scope

This is a test-only response to the independent review finding on candidate
`347bcda8f17aa9ad5c15ae539621dd21522f738c`. The review requested an EditMode
regression that exercises the Core Person-death plan through the real Unity
runtime boundary and checks the compatibility projection.

The added test constructs the admitted selected-definition graph and exact
selected City/NPC owner projections, binds a Person as a resident and to the
NPC, installs a current action within the existing daily operation, prepares
the Core death plan, and calls
`SimulationRuntime.TryExecuteCoreSelectedPersonDeath`. It verifies exact
owner references, Person/City/NPC/action owner writes and revision changes,
Unity death/action projection, one P12 mutation epoch, and a consistent census.

Natural Mortality remains disabled. This test directly exercises the existing
Person-death owner operation; it does not enable natural mortality in the
accepted Daily-v1 profile, change P12 sections or census coverage, or expand
the selected profile. No production source or .NET test source changed.

## Candidate identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Base implementation: `347bcda8f17aa9ad5c15ae539621dd21522f738c`
- Test fix commit: `34a3bf8b3cb6f63dd19267ab502b2ebf275d1e28`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Changed file: `Assets/_Project/Tests/EditMode/Editor/P12PopulationLifecycleInvalidationTests.cs`
- Changed file SHA-256: `049d9848e755a231227555c893cd9343e768b3910357b23279a818c91fb3b159`

The official runner input inventory records that exact file hash. The committed
file is byte-identical to the validated source. All previously committed
production changes remain unchanged.

## Validation

All Unity runs used Unity 6000.3.9f1, PowerShell 7.6.5, and the same stable
validation-input SHA-256:
`d522f3f47f1ed5d30af9b0716fc409a38f504c1b1235d553811f823682a8c01e`.

- Focused EditMode filter `P12PopulationLifecycleInvalidationTests`: 23/23
  passed; zero failed, skipped, or inconclusive. Run ID:
  `3e16439ef5da4fc2a9b849e0c638794c`. The original filtered report and log
  remain in private owner-only evidence retention.
- Full EditMode: 3,306/3,306 passed; zero failed, skipped, or inconclusive.
  Run ID: `2aad036bf67943c8a9e36bf48af6ae5f`.
- Official Smoke filter `Smoke`: 5/5 passed. Run ID:
  `bc497e14d4b3455bbf1f2936cabeb273`. All five Smoke test cases and passing
  outcomes are also present in the verified full EditMode package.
- The approved packager verified the full EditMode package against its
  original private run manifest:
  - Manifest: `Unity/coref-death-editmode.manifest.json`
  - Manifest SHA-256:
    `834b9181566bd7bcb36bc4221e7733634a4fc10298f1e7a59722abfc23d4ead1`
  - Archive: `Unity/coref-death-editmode.zip`
  - Archive SHA-256:
    `46d65000ecbd67637a5c1484e70e1cf722336a8ad037549ed53b91876d5e861e`
- The filtered focused and Smoke XML reports remain private. The public
  packager refused each filtered report because its parent `Simulation` suite
  count does not match the filtered descendants. No XML was changed or
  reinterpreted. The verified full-suite package includes the new regression
  and all five Smoke cases.
- CoreValidation remains 1,091/1,091 cold and warm from the unchanged
  implementation candidate `347bcda8f17aa9ad5c15ae539621dd21522f738c`.
  It was not rerun because this fix changes only a Unity Editor test and does
  not change production or .NET test sources.
- `git diff --check`: PASS.

## Review and status

A new independent exact-tip review of candidate
`34a3bf8b3cb6f63dd19267ab502b2ebf275d1e28` is pending. This checkpoint is
not promoted to Core canonical and does not complete CORE-F.
