# CORE-F restored TravelParty Core binding validation

Date: 2026-11-02 (local)

## Candidate and bounded scope

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Source commit: `0df84c30c4000081d30a3e09bb9697e620c5cc7c`
- Source tree: `e8cdea9b48a30eb3ba12af43509f136453fa4008`
- Parent: `424b2029ab57f64b22bf13a0806fa6908de6c42a`
- Core canonical remains: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The selected Core owner graph now validates restored TravelParty membership
against its exact TravelParty store, identity index, spatial network, City
owners, and NPC travel owners. The selected Unity restore path invokes that
Core check and separately verifies Unity object projections. Non-Core legacy
restore keeps its previous validation path. Two Core regressions exercise
synchronized member progress/reciprocal party identity and exact route endpoint
binding.

This moves one restored-graph integrity decision to the shared Core authority.
It does not complete public `DailyV1Application.TryStart`, the full Core restore
lifecycle, detached .NET hosting, or CORE-F. No P12 profile or owner coverage is
expanded, and no canonical promotion is claimed.

## Validation

- Official CoreValidation: cold `997/997` PASS; warm `997/997` PASS. Both new
  restore-binding tests passed in both runs. Run ID:
  `db2b9b6fc7a24697a3d1046a0c860a10`.
- Core private manifest SHA-256:
  `0f462fcbd91f0395f4c0ebec15272265ca1fe047583dd9ce3f42aed9d05a3be5`.
  Core input digest: `b4a9d16365c2879090871bf197ca9b4f4b75b190a169f8887e4e8eba64ea1401`;
  source and generated dependency inputs were stable. Cold TRX SHA-256:
  `b38bd8d648e3672e8e418195158ef3ce88ba1fb1946a8b0160f0a0a23a3caa58`;
  warm TRX SHA-256:
  `cafa64b70a90cef3ab7a5561d663ef63f78b18ce87c08b744fb3a8c465690d13`.
- Official Unity ALL EditMode: `3296/3296` PASS, zero failures, skips, or
  inconclusive cases. All five Smoke cases are present and passed in this
  unfiltered run. Unity input digest was stable at
  `59b96d04259b4f0f094d5b426714d8770a1f36fcbb55babe5b5a23273e8e566d`.
  Unity Editor: `6000.3.9f1`; run ID:
  `1c75eb4db5e141cc8da8f81c243bffeb`.
- The privacy-normalized Unity result package is in `Unity/`. Its manifest is
  `coref-restored-travel-party-bindings-20261102.manifest.json`, SHA-256
  `787883291a759be667dc12cce81c907de50078116a88f43a222d0cc954a9cd73`.
  The result archive is `coref-restored-travel-party-bindings-20261102.zip`,
  SHA-256 `0eeacff841e90c02dcf22f96134525e68bf4fa76b13970ce912590dfb659873f`
  (174,626 bytes). The official verifier passed against the protected original:
  one run, one complete result entry.
- The CoreValidation runner reported a Unity Library in this checkout; this
  evidence does not claim Library-independent .NET validation.
- `git diff --check`: PASS.

The original Core cold/warm TRX, manifest, and Unity XML/log remain in the
protected local evidence store under their run IDs. No public .NET TRX package
is claimed: CoreValidation emitted its native `simulation-core-validation/v1`
manifest, not the distinct `dotnet-trx-source-context-v1` schema required by
the approved adapter. No context was fabricated and no original was changed.

Validation ran against these exact source bytes before the source commit; each
hash matches commit `0df84c30c4000081d30a3e09bb9697e620c5cc7c`, with no later
source changes:

- `Packages/com.simulation.core/Runtime/Application/DailyV1SelectedRuntimeOwnerGraph.cs`:
  `ccda535bba85bdc99647c4d65e851bb0ceed67487396e97e2782230f7eb48ce3`
- `tests/Simulation.Core.Tests/DailyV1SelectedRuntimeOwnerGraphTests.cs`:
  `318f665075fbe00ca620fe1fcd7cd8ae157e8bd61a3437760ea688a2ba4842a7`
- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`:
  `78e0fa51bfcee4a40e7d2edc508a446005f32ef18c66988bb360d8641f0e3093`

## Attempt disposition and preservation

- Initial CoreValidation stopped at cold build because two new test fixture
  calls omitted the party ID. The test inputs were corrected before the passing
  cold/warm run; no test results were claimed from the failed build.
- Unity run `4b56e3f37ad347308bea3c99fc1c8f02` executed `3296` cases and had one
  diagnostic compatibility assertion fail. Core correctly rejected the
  corrupted party link, but the new message omitted the established
  `ActiveTravelPartyId` label. The Core diagnostic was corrected; the final full
  run passed.
- Unity run `7eb9374369454dc89b5110a1792f70cc` produced no result XML because
  compilation rejected an adapter check for a nonexistent
  `SimulationBootstrapComposition.OwnerGraph` property. That check was removed;
  the final full run passed.
- Three pre-existing uncommitted ProjectSettings changes were present in the
  Unity input and remain untouched and unstaged.

Independent exact-tip review is pending. This candidate is not canonical.
