# Simulation Vertical Slice v1 - Acceptance

**Status date:** 2026-11-28 (America/Sao_Paulo)

> Sanitized public checklist. Protected observations, private evidence, machine paths, and unreleased source are excluded. Component tests alone do not establish integrated release acceptance.

## Release gate

Accept v1 only after one integrated local product demonstration and required component-level native checks. Do not call a local build distributable until supported-runtime qualification, packaging notices, and exact release manifest are complete.

## Integrated demonstration

### 1. World Atlas and authoritative Simulation

1. Launch the exact qualified World Atlas package from the release manifest.
2. Start a fresh supported Daily-v1 world through Atlas. Do not use an external Simulation command to initialize or advance it.
3. Verify the initial view is coherent and paused after the first genuine completed daily cut; inspect only authoritative facts supported by the chosen profile.
4. Use Atlas Step once. Verify the next supported daily cut and a real state change produced by Core. Do not substitute fixtures, prerecorded data, synthetic observations, or fabricated output.
5. Verify Pause, Resume/Play, Slow, Normal, and Fast. Speed changes scheduling only; one accepted operation advances one supported day. No hourly advancement is claimed.
6. Verify failures present only the latest proven detached view. Shutdown must stop new work, safely settle or fault the owned operation, close the Lab child, and remove only its owned scratch root.

**Status: OPEN.** Core, evidence profile, facade, official producer/encoder, and local Host child candidate are promoted/validated at their separate scopes. ATLAS-B remains unvalidated: corrected attempt-17 reached the locked restore graph from `WorldAtlas.Tests.csproj` and failed with exit 1, zero warnings/errors, and no explanatory error. Seven locks, existing assets, and approved cache stayed unchanged. No build/tests/conformance followed. The private binlog remains; read-only checks found no parser already installed in the checked locations and no root cause in text output. The previous proposal to repeat the same Tests-root restore is superseded. Atlas controls and the full real producer-reader-Host session still require successful ATLAS-B validation and conformance.

### 2. Dungeon Studio and Atlas Dungeon Viewer

1. Generate an Atlas-supported non-Crypt document in Dungeon Studio, edit, validate, save, close the app, and reopen the same file.
2. Open that unchanged validated file in Atlas-C. Verify document origin, IDs, and local coordinates are presented as document facts.
3. Return to the selected Simulation world during the integrated flow. Do not associate the standalone Dungeon with a world, City, site, or Location.

An existing Comb v1 or other supported non-Crypt profile is enough for v1. Crypt v1/v2 are not prerequisites.

**Component acceptance: PASS.** User acceptance confirmed open/display/integrity for the approved Comb v1 Seed 42 fixture, including validated standalone status, eight rooms, seven passages, element list and selected-room Inspector. Return-to-Demo was then checked in the reviewed local package and restored Demo World Day 1 / sequence 1. Return to the selected live Simulation world in the integrated flow remains separate. Public promotion awaits written redistribution rights for the imported Dungeon source trees.

### 3. City Studio and Atlas City Viewer

1. Generate and inspect Cities for seeds 0 and -1; save each, close City Studio, and reopen each unchanged file in a fresh process.
2. Open each unchanged validated file in Atlas City Viewer. Verify origin, document IDs, and local coordinates come from the file.
3. Verify the viewer does not infer Simulation identity, world association, settlement ownership, Location binding, or runtime geometry.

**City Studio Foundation: CLOSED / USER ACCEPTED.** User confirmed generation for seeds 0, -1, 21, and 42; rendering; zoom in/out; middle-button pan; Fit; map selection; map-to-list synchronization; Save/Load. For seeds 0 and -1, the user saved, closed Studio, reopened and loaded without regeneration, and confirmed pre/post Document ID, canonical result digest, seed, generator/version origin, and layout matched. Digest values are not reproduced here.

**Implementation identity:** local-only candidate `75d72047e56339382b10e39bd7fde941bec40291`. Independent implementation review passed; locked restore and Release build passed with zero warnings/errors; City.Tests 34/34; City.Studio.Tests 7/7; self-contained win-x64 publish passed. These do not qualify the Atlas consumer.

**Atlas City Viewer:** local-only design `ac2830876da184d88ef88ec5f32e92e4c0572698`; design and consumer review PASS. Implementation not started. Locked win-x64 consumer restore, native open/return, and integrated flow remain.

List-to-map highlighting and cross-viewer selection consistency are deferred UX outside the approved Foundation contract. No City v2 or optional generation/UI work.

### 4. Release manifest and compatibility

Record per accepted component:

- exact branch/commit, tree where applicable, review and validation identity;
- exact executable/package name and SHA-256;
- launch command and working directory;
- supported OS, architecture, SDK/runtime and qualification;
- schema/pin compatibility and viewer limits;
- local-development proof versus distributable bundle;
- limitations and deferred features.

Identify the exact files used in the integrated demo. Do not label a build distributable before runtime qualification. The reviewed Host boundary targets .NET 8, which ended support on 2026-11-10; the reviewed distribution route is separate .NET 10 LTS qualification using a supported patch. See the [official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

A distributable containing the Windows ANGLE native dependency must include the applicable BSD-style copyright, conditions, and disclaimer.

**Status: OPEN.** Exact package identities, supported-runtime qualification, notices, and compatibility manifest are incomplete.

## Component validation dashboard

| Component | Identity / review | Automated validation | Native acceptance | State |
|---|---|---|---|---|
| Core/Lab chain | Promoted Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b`; producer `3eaa62d98faf3fb68958588f04ef25a576ccd0a9` exact-tip review PASS. | CoreValidation 1,241/1,241 cold and warm; eight gates PASS; original comparison and integrity PASS. | Protected real two-cut pair exists; contents/digests omitted. | Promoted; Atlas conformance pending. |
| ATLAS-B | Local-only successor `a28b1c26379ccbd148d3829b35a3322b2274a20e` / tree `ba85253f82407d669ec6cb3f947a7c78bf1a4bfe`; static source review PASS/no findings. | Correctly invoked attempt-17 locked restore from `WorldAtlas.Tests.csproj` reached graph processing then exited 1 without explanatory MSBuild/NuGet error. Locks, existing assets, cache unchanged. No build/tests. | Real producer-reader conformance not run. | Restore blocked; unvalidated. Repeat restore proposal superseded; inspect the preserved private binlog only if a viewer is already available. |
| Interactive Lab Host | Local-only candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10` / tree `8932a9300305cbb262cb5f1e9a669e208ae9c24c`; exact-tip independent review PASS/no findings. | Locked restore, Release net8.0 build, 38/38 focused host regressions, diff check PASS. | Atlas integration/native controls await real producer-reader-Host conformance. | Local child checkpoint PASS; not Atlas-integrated or distribution-qualified. |
| .NET 10 Host admission | Local-only design `b9d0d66bcd8e780f724714cbeb58bc56c9c26070` / tree `379503fcb76e9558c388852c90719ee9b4f0a338`; exact-tip design review PASS. | Implementation and population-specific evidence profile active; locked restore/build/test/win-x64 folder publish pending. | Atlas conformance remains downstream. | Implementation dispatched; no validated net10 candidate yet. |
| ATLAS-C | Local-only candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df`; exact-tip review PASS. | Locked restores, RID graph checks, Release build, single-file win-x64 publish; direct tests 39/39; six-project package closure 50/50 matching lock metadata. | Open/display/integrity and Return-to-Demo PASS; selected live-world return remains integrated. | Native component accepted; public promotion rights hold. |
| City Foundation v1 | Local-only candidate `75d72047e56339382b10e39bd7fde941bec40291`; independent implementation review PASS. | Locked restore/build PASS, zero warnings/errors; City.Tests 34/34; Studio.Tests 7/7; self-contained win-x64 publish PASS. | Seeds 0, -1, 21, 42 and interactions/persistence PASS; fresh-process 0/-1 reopen preserved identity, digest, origin and layout without regeneration. | Closed / accepted. |
| Atlas City Viewer | Local-only design `ac2830876da184d88ef88ec5f32e92e4c0572698`; design and consumer review PASS. | Consumer locked win-x64 restore not qualified. | Viewer open/return not run. | Implementation pending the serialized Atlas slot. |
| Crypt v1 | Owner-reported validated implementation; not needed for initial demo. | Reproducibility correction and implementation review PASS. | Pan/zoom/Fit, Save/Reopen, and regeneration comparison for seeds 42 and -21 remain. | Parallel native closure; Crypt v2 deferred. |

## Rights and distribution restrictions

ATLAS-C public promotion is held because redistribution rights for two imported Dungeon Studio source trees have not been established. Git history is consistent with in-project development but does not establish legal title. Required evidence is a written declaration/license from the rights holder or authorized representative covering the complete imported trees, public source publication, modification, and application-package redistribution, with attribution and conditions. If employment or client terms assign the rights, obtain authorization from that entity. Missing directory license files alone are not treated as evidence of third-party ownership.

No Dungeon source, executable, protected observation, private validation log, package cache, or binary evidence is included in this status mirror. Repository visibility changes remain unauthorized. Permitted local acceptance and functional development may continue.

Audited six-project NuGet closure had known license metadata for all 50 packages; no other unknown license metadata was identified. `Avalonia.Angle.Windows.Natives` requires its BSD-style notice in any distributable containing the binary. This is a packaging obligation, not an unknown-rights finding.

## Remaining gates and next milestone

- **ATLAS-B:** open the preserved private attempt-17 binary log with an already-installed structured-log viewer if available. The same Tests-root restore has already failed at graph processing and must not be repeated without a concrete cause and new authorization. Then complete the locked restore/build/test route, real producer-reader conformance, and exact-tip review.
- **Host:** local child implementation `9023caa` and exact-tip review PASS. Complete producer-reader-Host conformance, Atlas controls/native behavior, and separate supported-runtime qualification.
- **.NET 10 admission:** complete the dispatched implementation, population-specific evidence profile, locked restore/build/test and win-x64 folder publish; obtain independent exact-tip review. Do not claim Atlas conformance before ATLAS-B passes.
- **ATLAS-C:** open/display/integrity and Return-to-Demo PASS; integrated return to the selected live Simulation world remains.
- **City Viewer:** implement only in the single serialized Atlas slot after ATLAS-B/Host prerequisites, then pass consumer locked win-x64 restore/build/tests/review and native open/return.
- **Rights and packaging:** keep Dungeon source/binaries local until written rights-holder authorization is reviewed. Include the ANGLE BSD-style notice before any distributable containing the binary.
- **Integrated product:** demonstrate authoritative Day 1/Day 2 state change, temporal controls, detached viewers, persistence workflows, failure behavior and safe shutdown; then record the exact release manifest and supported runtime.

Next meaningful milestone: actionable cause from the existing attempt-17 binlog, followed by an authorized complete ATLAS-B validation and real producer-reader-Host conformance. The Vertical Slice remains open until the integrated demonstration is accepted.

