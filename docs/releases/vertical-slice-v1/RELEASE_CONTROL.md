# Simulation Vertical Slice v1 - Release Control

**Last reconciled:** 2026-11-28 (America/Sao_Paulo)

> Sanitized public status mirror. The authoritative, complete record remains on local-only branch `codex/release/vertical-slice-v1-control`. This branch starts from the already-public architecture commit `d496af79a35bec9c9632c146579fa62f3d6eb76e`; local control history is not published. Candidates marked local-only are review references, not objects available from this branch.

## Objective and scope

Deliver a functional Simulation Vertical Slice v1 around authoritative Simulation Core and World Atlas. Daily-v1 starts a real world paused, presents detached Core facts, advances one supported day per accepted step, and exposes pause/resume and Slow/Normal/Fast scheduling. The demonstration must show a real Core-owned state change across adjacent completed daily observations; protected observation contents and digests are excluded from this mirror.

The slice also includes standalone Dungeon Studio and City Studio workflows and read-only Atlas viewers for valid Dungeon and City documents. Studio documents remain detached from world entities; viewers must not invent world, location, settlement, ownership, or geometry associations. Studio processes must not remain open while Atlas simulates. One step means one supported day. Hourly advancement, Crypt v2, advanced City generation, dynamic geometry, historical playback, full Web/Unity frontends, and distributed execution are outside v1.

## Evidence and publication labels

- **Promoted/public:** the identity is present on a verified public repository ref.
- **Local-only:** the candidate is not present on the public refs checked for this record; the SHA is provided only for coordination.
- **Validated:** only the listed gates passed. This does not imply native acceptance, integration, distribution qualification, or release.
- **User-accepted:** limited to actions and outcomes the user explicitly reported.
- Review, automated validation, native acceptance, runtime qualification, promotion, and final product acceptance are separate gates.

## Checkpoint dashboard

| Checkpoint | Current state and identity | Owner | Remaining gate / release effect |
|---|---|---|---|
| v1 roadmap | **PROMOTED / PUBLIC** `d496af79a35bec9c9632c146579fa62f3d6eb76e` on `codex/architecture/world-identity-projection`. | Architecture General | Defines scope and sequence. |
| Atlas base | **PROMOTED / PUBLIC** canonical ref `2abb1a9c3c8e82d5adc3262562112ff86c3b1353`. | Atlas Orchestrator | Supplies shell and shared viewport. |
| Core/Lab evidence profile, facade, producer/encoder | **PROMOTED / PUBLIC** on Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b`. Profile integration `c0275928ad319f62dc40415d676c21bcdf0eb7a3`, facade `46503a0a82c0ef514061e0a0836d22d3017f0f08`, producer `3eaa62d98faf3fb68958588f04ef25a576ccd0a9`. | Orchestrate Simulation / Core | Atlas real-reader conformance remains required. |
| ATLAS-B reader/comparer | **LOCAL-ONLY; static exact-tip source review PASS; restore graph FAILED; unvalidated/unpromoted.** Candidate `a28b1c26379ccbd148d3829b35a3322b2274a20e` / tree `ba85253f82407d669ec6cb3f947a7c78bf1a4bfe`. | Atlas Orchestrator / Architecture General | Attempt-17 correctly invoked locked restore from `WorldAtlas.Tests.csproj` and reached MSBuild graph processing, then exited 1 without an explanatory MSBuild/NuGet error. Locks/assets/cache stayed unchanged; no build, tests, or conformance. Existing private binlog has no installed parser in checked locations; no retry or tool installation. |
| Interactive Lab Host `serve-daily-stdio-v1` | **LOCAL-ONLY; child implementation PASS.** Candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10` / tree `8932a9300305cbb262cb5f1e9a669e208ae9c24c`; exact-tip independent review PASS/no findings. | Orchestrate Simulation | Locked restore, Release `net8.0` build, 38/38 focused tests, and diff check PASS. Real producer-reader-Host conformance, Atlas controls, and supported-runtime qualification remain separate. |
| Host .NET 10 admission | **DESIGN REVIEW PASS; IMPLEMENTATION DISPATCHED, NOT YET VALIDATED.** Local-only design `b9d0d66bcd8e780f724714cbeb58bc56c9c26070` / tree `379503fcb76e9558c388852c90719ee9b4f0a338`. | Orchestrate Simulation / Architecture General | Exact-tip design review PASS/no findings. Local implementation is active on an isolated successor; preserve the current `net8.0` and P12 v2 route. Population-specific evidence profile and exact candidate validation remain.
| ATLAS-C Dungeon Viewer | **LOCAL-ONLY; technical validation, exact-tip review, component acceptance, and Return-to-Demo PASS; public promotion held on rights.** Candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df`. | Atlas Orchestrator | Approved Seed 42 Comb v1 standalone fixture rendered and validated; Return to Demo restored Demo World Day 1 / sequence 1. Returning to the selected live Simulation world remains an integrated gate. |
| City Foundation v1 | **CLOSED / USER ACCEPTED.** Candidate `75d72047e56339382b10e39bd7fde941bec40291` is local-only. | Simulation UI Orchestrator | No Foundation work remains. List-to-map highlighting is deferred UX; no City v2 or optional generation work. |
| Atlas City Viewer | **DESIGN REVIEW PASS; implementation not started.** Design `ac2830876da184d88ef88ec5f32e92e4c0572698` is local-only. | Atlas Orchestrator / Architecture General | Sole Atlas owner implements it in the reserved serialized slot after ATLAS-B and Host-control prerequisites. No separate City dispatch. |
| Dungeon authoring / Crypt v1 | Existing supported non-Crypt authoring, such as Comb v1, is sufficient for the initial demo. Crypt v1 native closure remains open. | Dungeon Studio Orchestrator | Crypt pan/zoom/Fit, Save/Reopen and regeneration comparison are parallel, not release-critical. Crypt v2 and optional improvements deferred. |
| Integrated acceptance and manifest | **OPEN / NOT RELEASED.** | Release Orchestrator | Requires the real integrated Daily-v1 demo, all required native flows, runtime qualification, notices, and exact manifest. |

## Core/Lab evidence chain

Core evidence profile, coherent observation facade, and official LAB-OBS-A producer/encoder are promoted on Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b`. The producer passed exact-tip review; official CoreValidation reported 1,241/1,241 assertions in both cold and warm runs, and all eight package gates passed. Original comparison and integrity gates passed. A valid pair of adjacent completed daily observations exists, with matching predecessor relationship and a real state change. Observation contents, field values, source reports, and digests are protected and intentionally omitted. These results do not prove Atlas reader or Host conformance.

## ATLAS-B recovery and validation

Candidate `a28b1c26379ccbd148d3829b35a3322b2274a20e` / tree `ba85253f82407d669ec6cb3f947a7c78bf1a4bfe` contains the reviewed identity, selection-context, fixture line-ending, and reopen-message fixes. Exact-tip static source review returned PASS/no findings. This is source review only.

The predecessor passed focused 4/4 and full 49/49, but those results do not transfer to the current candidate. Attempt-16 was an argument-construction preparation failure (MSB1009) before project load. Corrected attempt-17 used the exact `WorldAtlas.Tests.csproj` root in locked mode under SDK 10.0.404 and reached MSBuild 18.9.15. It then failed in `_GetAllRestoreProjectPathItems` / `_GenerateRestoreProjectPathWalk` with direct exit 1, zero warnings and zero errors, and no explanatory error or exception. No locked restore completed, and no build, tests, or real-pair conformance followed. All seven lockfiles, existing project assets, and the approved cache remained unchanged.

The attempt-17 diagnostic text does not expose a root cause. Its private binary log remains preserved, but a bounded read-only inspection found no already-installed parser in the checked SDK/Visual Studio/.NET tool/cache locations. No tool was installed and no environment, cache, ACL, source, or evidence was changed. Repeating the Tests-project restore is superseded by attempt-17 and is not authorized. The only useful immediate inspection is to open that existing private binlog with a viewer already installed and accessible; otherwise keep the restore gate blocked pending a concrete recovery route. No private logs, hashes, paths, observation artifacts, or package data are published here.

## Host and supported runtime

The bounded local `serve-daily-stdio-v1` child candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10` / tree `8932a9300305cbb262cb5f1e9a669e208ae9c24c` is locally complete and independently exact-tip reviewed PASS/no findings. Its cumulative local-only diff from Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b` touches only six paths: `tools/Simulation.Lab/DailyV1StdioChildHost.cs`, `tools/Simulation.Lab/DailyV1ObservationProducer.cs`, `tools/Simulation.Lab/Program.cs`, `tools/Simulation.Lab/Simulation.Lab.csproj`, `tests/Simulation.Core.Tests/DailyV1StdioChildHostTests.cs`, and `tests/Simulation.Core.Tests/Simulation.Core.Tests.csproj`; it changes no Core or P12 source. Locked restore, Release `net8.0` build, 38/38 focused regressions, and diff check passed. The fixed child implements bounded v1 length-prefixed UTF-8/JSON frames; validates request order and run/day/sequence identity; keeps Core genesis, one-day advancement, capture, encoding and disposal on one owner thread; and uses the promoted Core path plus official observation encoder. Tests cover adjacent real cuts, semantic repeatability across independent runs, stale/foreign/duplicate requests, frame bounds, malformed/truncated input, EOF, blocked/broken output cleanup, safe stop, scratch-root ACL ownership and reparse-point rejection. This is local Host validation only, not Atlas integration.

The child preserves the existing `net8.0`/P12 v2 admission by scope. The separate Windows x64 NoSave Genesis design passed exact-tip independent review at local-only commit `b9d0d66bcd8e780f724714cbeb58bc56c9c26070` / tree `379503fcb76e9558c388852c90719ee9b4f0a338`; a bounded implementation is now dispatched and active. The reviewed design pins SDK `10.0.404`, runtime `10.0.15`, a separate `net10.0` host, locked `win-x64` restore and deterministic self-contained folder publish. It requires Core-owned genesis/census/session/capture, a shared protocol/encoder boundary, unchanged net8/P12 behavior, runtime and binary identity evidence, failure/lifecycle regressions, semantic parity on consecutive cuts, a population-specific evidence profile, and later Atlas-reader conformance. The .NET 8 candidate is not a supported distributable runtime. [Official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

## City Foundation and City Viewer

City Foundation candidate `75d72047e56339382b10e39bd7fde941bec40291` passed independent implementation review, locked restore, Release build with zero warnings/errors, City.Tests 34/34, City.Studio.Tests 7/7, and self-contained win-x64 publish. The user confirmed native generation for seeds 0, -1, 21, and 42; rendering; zoom; middle-button pan; Fit; map selection and map-to-list sync; Save/Load; and fresh-process Save/Close/Reopen/Load.

For saved seeds 0 and -1, the user confirmed pre/post Document ID, canonical result digest, seed/generator/version origin, and layout matched, with no regeneration. Digest values are omitted. No Foundation gate remains. List-to-map highlighting and cross-viewer selection consistency are deferred UX; the approved contract does not require them. No City v2 or optional work is authorized.

City Viewer design `ac2830876da184d88ef88ec5f32e92e4c0572698` has owner and consumer review PASS. Atlas is sole owner of its additive implementation paths. Integration remains serialized because project references, locks, RID behavior, solution membership, and shell transitions are shared files. City Studio and library pins remain with Simulation UI. Atlas must prevent RID propagation to City references and pass locked win-x64 restore before integration.

## ATLAS-C acceptance, rights, and notices

ATLAS-C candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df` passed locked restores, RID graph checks, Release build, single-file win-x64 publish, direct tests 39/39, independent exact-tip review, and package-closure review. The six-project solution closure classified all 50 required packages as present with lock metadata matching. These technical results do not clear source redistribution rights.

The user accepted open/display/integrity for the approved standalone Comb v1 Seed 42 fixture and its document, room, floor and inspector facts. In the subsequent permitted native check, Return to world restored Demo World Day 1 / sequence 1 with normal navigation and observation view. Return-to-Demo is PASS. Returning from the viewer to the selected live Simulation world during the integrated flow remains separate.

Public promotion is held because redistribution rights for the two imported Dungeon source trees (`Dungeon.Core` and `Dungeon.ProjectFiles`) have not been established. Git history is consistent with in-project development but does not establish legal title. Required evidence is a written declaration/license from the rights holder or authorized representative covering the complete imported trees, public source publication, modification, and application-package redistribution, with attribution and conditions. If employment or client terms assign the rights, obtain authorization from that entity. Missing directory license files alone are not treated as evidence of third-party ownership. Keep source and binaries local; do not change visibility or publish to another remote.

The audited six-project NuGet closure had known license metadata for all 50 packages; no other unknown package license metadata was identified. `Avalonia.Angle.Windows.Natives` requires its BSD-style notice in any distributable containing the binary. Include the exact copyright, conditions, and disclaimer before a distributable release. This is a packaging obligation, not an unknown-rights finding.

## Dependency order and critical path

1. Resolve ATLAS-B restore-graph diagnostics by inspecting the preserved private attempt-17 binlog with an already-installed viewer if available; do not repeat the same restore or install a parser.
2. Only after an evidence-based recovery hypothesis and bounded authorization, complete locked restore, the separate Desktop `win-x64` restore, build/tests, real official-pair reader conformance, and exact-tip validation review.
3. Complete real producer → Atlas reader → local Host conformance, then Atlas live-session controls, daily advance/pause/speed behavior, safe shutdown, and native acceptance.
4. Complete the active .NET 10 NoSave Genesis implementation and its population-specific evidence-profile route, then obtain exact-tip implementation review. Preserve compatibility with the existing net8/P12 route.
5. In the single serialized Atlas integration slot after the reader/Host prerequisites, implement the reviewed City Viewer and pass consumer locked `win-x64` restore, build, tests, review, and native City open/return checks.
6. Complete Dungeon Studio and integrated viewer flows, return to the selected Simulation world, supported-runtime qualification, exact release manifest, and packaging notices. Dungeon source/binary public promotion remains gated on rights.

## Current blockers and next milestone

- **ATLAS-B:** attempt-17 is the latest corrected project-root restore and failed in graph processing without a root cause. No candidate build/tests or producer-reader conformance exist. A read-only binlog check could not proceed because no parser was already available in the checked locations.
- **Interactive Host:** child implementation `9023caa` is locally validated and independently reviewed. Atlas integration, real producer-reader-Host conformance, Atlas controls, and final supported-runtime qualification remain.
- **Runtime:** reviewed design `b9d0d66` is PASS; the bounded local implementation is dispatched and active. The test-population evidence-profile route and all runtime validation gates remain.
- **ATLAS-C:** native component acceptance and Return-to-Demo PASS. Integrated return to a selected live Simulation world remains; public promotion awaits rights-holder evidence for the imported Dungeon source trees.
- **City:** Foundation v1 is closed. City Viewer implementation is held for the serialized Atlas slot and its consumer RID qualification.
- **Release:** the integrated demo, exact manifest, supported runtime evidence, and ANGLE notice are incomplete. The Crypt v1 native closeout remains parallel and does not block a first demo using supported Comb v1.

The only immediate user-side diagnostic action is optional: if an appropriate structured-log viewer is already installed, inspect the preserved private attempt-17 binlog and report any innermost exception; no restore command or tool installation is requested. The exact attempt-17 Tests-root restore has already failed and is not to be repeated. Public Dungeon redistribution additionally requires a written rights-holder/authorized-representative declaration. Computer Use can be requested again for later integrated native acceptance. The .NET 10 implementation and read-only evidence-profile assessment are active in their separate owners.

**Next milestone:** an actionable diagnosis from the existing attempt-17 binlog, followed by an authorized complete ATLAS-B validation and real producer-reader-Host conformance. The Vertical Slice remains OPEN until the functional integrated demonstration and release manifest pass.

