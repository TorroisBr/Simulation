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

**Status: OPEN.** Core, evidence profile, facade, and official producer/encoder are promoted. ATLAS-B remains unvalidated: the one authorized project-root attempt using `WorldAtlas.Tests.csproj` failed before project load because PowerShell argument construction corrupted the path (MSB1009), so it did not exercise restore. The authorization is consumed; a corrected invocation needs new authorization. The bounded Host child candidate passed local validation and independent review; Atlas controls still require accepted ATLAS-B validation and real producer-reader-Host conformance.

### 2. Dungeon Studio and Atlas Dungeon Viewer

1. Generate an Atlas-supported non-Crypt document in Dungeon Studio, edit, validate, save, close the app, and reopen the same file.
2. Open that unchanged validated file in Atlas-C. Verify document origin, IDs, and local coordinates are presented as document facts.
3. Return to the selected Simulation world. Do not associate the standalone Dungeon with a world, City, site, or Location.

An existing Comb v1 or other supported non-Crypt profile is enough for v1. Crypt v1/v2 are not prerequisites.

**Component status:** User accepted ATLAS-C open/display/integrity for the approved Comb v1 fixture. The screenshot showed validated standalone status, document identity, eight rooms, seven passages, element list, and selected-room Inspector. The user checked integrity after viewing. **Open:** shell Return-to-Demo restoration was not reported; return to selected world in integrated flow is separate. A permitted Computer Use session was stopped by the user with Escape before inspection or UI action, so it produced no new acceptance result and was not a product failure. Public promotion is held on Dungeon source-rights evidence.

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
| ATLAS-B | Local-only successor `a28b1c26379ccbd148d3829b35a3322b2274a20e`; exact-tip static source review PASS/no findings. | Solution and Desktop locked restores exited 1 in graph processing without underlying MSBuild/NuGet error. The authorized `WorldAtlas.Tests.csproj` attempt returned MSB1009 before project load due corrupted path argument; restore graph not exercised. Current successor has no build/tests. Predecessor `0de71af27a567ce96d49bbf5c219df14d063a2e5` passed focused 4/4 and full 49/49; results do not transfer to `a28b1c26`. | Current successor real producer-reader conformance not run. | Restore blocked; unvalidated; new authorization required for a corrected attempt. |
| Interactive Lab Host | Local-only amendment `2612be197c2b128d056d48068d50c831325f5927`; child candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10`, independent exact-tip review PASS/no findings. | Locked restore and Release net8.0 build PASS; 38/38 focused host regressions; diff check PASS. | Atlas integration/native controls await real producer-reader-Host conformance. | Local child checkpoint PASS; integration gated; .NET 8 is not a supported distribution target. |
| ATLAS-C | Local-only candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df`; exact-tip review PASS. | Locked restores, RID graph checks, Release build, single-file win-x64 publish; direct tests 39/39; six-project package closure 50/50 matching lock metadata. | Open/display/integrity PASS; Return-to-Demo unreported. Computer Use was stopped with Escape before any UI inspection/action; no new result and no product failure. Integrated selected-world return separate. | Native partial; public promotion rights hold. |
| City Foundation v1 | Local-only candidate `75d72047e56339382b10e39bd7fde941bec40291`; independent implementation review PASS. | Locked restore/build PASS, zero warnings/errors; City.Tests 34/34; Studio.Tests 7/7; self-contained win-x64 publish PASS. | Seeds 0, -1, 21, 42 and interactions/persistence PASS; fresh-process 0/-1 reopen preserved identity, digest, origin, layout without regeneration. | Closed / accepted. |
| Atlas City Viewer | Local-only design `ac2830876da184d88ef88ec5f32e92e4c0572698`; design and consumer review PASS. | Locked consumer win-x64 restore not qualified. | Viewer open/return not run. | Implementation pending serialized slot. |
| Crypt v1 | Owner-reported validated implementation; not needed for initial demo. | Reproducibility correction and implementation review PASS. | Pan/zoom/Fit, Save/Reopen, and regeneration comparison for seeds 42 and -21 remain. | Parallel native closure; Crypt v2 deferred. |

## Rights and distribution restrictions

ATLAS-C public promotion is held because redistribution rights for two imported Dungeon Studio source trees have not been established. Git history is consistent with in-project development but does not establish legal title. Required evidence is a written declaration/license from the rights holder or authorized representative covering the complete imported trees, public source publication, modification, and application-package redistribution, with attribution and conditions. If employment or client terms assign the rights, obtain authorization from that entity. Missing directory license files alone are not treated as evidence of third-party ownership.

No Dungeon source, executable, protected observation, private validation log, package cache, or binary evidence is included in this status mirror. Repository visibility changes remain unauthorized. Permitted local acceptance and functional development may continue.

Audited six-project NuGet closure had known license metadata for all 50 packages; no other unknown license metadata was identified. `Avalonia.Angle.Windows.Natives` requires its BSD-style notice in any distributable containing the binary. This is a packaging obligation, not an unknown-rights finding.

## Remaining gates and next milestone

- ATLAS-B: the one authorized project-root attempt was consumed by malformed argument construction before project load; obtain new authorization for any corrected attempt, then establish an architecture-reviewed complete locked restore/build/test route, real producer-reader conformance, and exact-tip validation review.
- Host: bounded local child checkpoint `9023caa2` is validated and independently reviewed; await the architecture dependency decision, then prove producer-reader-Host conformance before Atlas controls.
- ATLAS-C: request native Computer Use at a suitable later opportunity for Return-to-Demo; do not infer it from screenshots or treat access denial as product failure.
- City Viewer: implement only in the reserved serialized Atlas slot; pass locked win-x64 consumer restore before integration.
- Integrated product: complete viewer/Studio flows, daily controls, latest-proven-view behavior, safe shutdown, supported runtime, notices, and exact manifest.

Next meaningful milestone is a newly authorized, correctly formed ATLAS-B project-root diagnostic and an approved complete validation route, followed by real producer-reader-Host conformance. The complete Vertical Slice remains open until the integrated demonstration is user accepted.

