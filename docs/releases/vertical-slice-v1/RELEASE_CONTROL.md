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
| Core/Lab evidence profile, facade, producer/encoder | **PROMOTED / PUBLIC** on Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b`. Profile integration `c0275928ad319f62dc40415d676c21bcdf0eb7a3`, facade `46503a0a82c0ef514061e0a0836d22d3017f0f08`, producer `3eaa62d98faf3fb68958588f04ef25a576ccd0a9`. | Orchestrate Simulation / Core | Atlas real-reader conformance is still required. |
| ATLAS-B reader/comparer | **LOCAL-ONLY; static exact-tip review PASS; restore BLOCKED; unvalidated and unpromoted.** Candidate `a28b1c26379ccbd148d3829b35a3322b2274a20e`, after `0de71af27a567ce96d49bbf5c219df14d063a2e5`. | Atlas Orchestrator / Architecture General | The authorized project-root attempt using `WorldAtlas.Tests.csproj` failed before project load because PowerShell argument construction corrupted the path (MSB1009). Restore graph not exercised; no build/tests. The single authorization is consumed; a corrected invocation needs new authorization. |
| Interactive Lab Host | **LOCAL-ONLY; bounded child checkpoint PASS.** Candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10` / tree `8932a9300305cbb262cb5f1e9a669e208ae9c24c`; independent exact-tip review PASS/no findings. | Orchestrate Simulation | Locked restore and Release net8.0 build PASS; focused host regressions 38/38 PASS; diff check PASS. Atlas integration still requires ATLAS-B and real producer-reader-Host conformance. .NET 8 is out of support; this is not distribution qualification. |
| ATLAS-C Dungeon Viewer | **LOCAL-ONLY; technical validation and exact-tip review PASS; native component acceptance partial; public promotion held on rights.** Candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df`. | Atlas Orchestrator | Open/display/integrity accepted. On 2026-11-28 Computer Use was stopped with Escape before UI inspection or input; Return-to-Demo remains unverified. This is pending acceptance access, not a failed product test. |
| City Foundation v1 | **CLOSED / USER ACCEPTED.** Candidate `75d72047e56339382b10e39bd7fde941bec40291` is local-only. | Simulation UI Orchestrator | No Foundation work remains. List-to-map highlighting is deferred UX; no City v2 or optional generation work. |
| Atlas City Viewer | **DESIGN REVIEW PASS; implementation not started.** Design `ac2830876da184d88ef88ec5f32e92e4c0572698` is local-only. | Atlas Orchestrator / Architecture General | A bounded reassessment of City-only implementation parallelism is underway. Shared project references, locks, RID behavior, solution membership, and shell remain serialized; no implementation starts until an isolated boundary is independently approved. |
| Dungeon authoring / Crypt v1 | Existing supported non-Crypt authoring, such as Comb v1, is sufficient for the initial demo. Crypt v1 native closure remains open. | Dungeon Studio Orchestrator | Crypt pan/zoom/Fit, Save/Reopen and regeneration comparison are parallel work, not a v1 demo prerequisite. Crypt v2 and optional improvements deferred. |
| Integrated acceptance and manifest | **OPEN / NOT RELEASED.** | Release Orchestrator | Requires live session, both viewer flows, persistence, safe shutdown, supported runtime, and exact manifest. |

## Core/Lab evidence chain

Core evidence profile, coherent observation facade, and official LAB-OBS-A producer/encoder are promoted on Core canonical `02051d1f971f131cc73b1c50976bbd96860c057b`. The producer passed exact-tip review; official CoreValidation reported 1,241/1,241 assertions in both cold and warm runs, and all eight package gates passed. Original comparison and integrity gates passed. A valid pair of adjacent completed daily observations exists, with matching predecessor relationship and a real state change. Observation contents, field values, source reports, and digests are protected and intentionally omitted. These results do not prove Atlas reader or Host conformance.

## ATLAS-B recovery and validation

Candidate `a28b1c26379ccbd148d3829b35a3322b2274a20e` contains four review-directed fixes: identity-token validation, Inspector selection isolation across worlds, deterministic line endings for the demo predecessor fixture, and accurate isolated-cut reopen messaging. Architecture General's exact-tip source review returned PASS/no findings. This is source review only.

Predecessor `0de71af27a567ce96d49bbf5c219df14d063a2e5` passed focused tests 4/4 and the full suite 49/49. These results do not transfer to `a28b1c26`, which has no successful restore, build, tests, or real-pair conformance.

Recovery conclusions:

- An early diagnostic did not reach MSBuild because the capture wrapper passed an empty argument vector. This was a capture-tool defect, not a restore result.
- A corrected host probe resolved the pinned SDK; a no-target project evaluation resolved the expected project path and target framework.
- The solution locked diagnostic restore failed in `_FilterRestoreGraphProjectInputItems`, exit 1, with no underlying MSBuild/NuGet error or exception.
- The direct Desktop-project locked restore failed in `_GetAllRestoreProjectPathItems` / `_GenerateRestoreProjectPathWalk`, exit 1, again without an explanatory error. The nested Viewport path walk succeeded.
- Workload-locator notices also occurred during an earlier successful restore and are not established as the cause. Root cause remains undetermined.
- Candidate source, seven lockfiles, project asset files, approved package cache, and protected producer artifacts were reported unchanged. Diagnostic logs and binary evidence remain private.
- No equivalent-command retry is authorized. The single project-level diagnostic using `WorldAtlas.Tests.csproj` was attempted after owner/SYSTEM-only ACL verification. PowerShell inline escaping corrupted the project-path argument; MSBuild returned MSB1009 before loading the project. The restore graph was not exercised, no NuGet locked restore or binlog was produced, and no build/tests followed. Candidate, locks, assets, and approved package inventory remained unchanged. The attempt's authorization is consumed; any corrected invocation requires new authorization. If later authorized, a project-root PASS remains restore evidence only; architecture/evidence review must establish whether a complete project-level build/test route has equivalent coverage. RID validation, producer-reader conformance, and independent exact-tip review remain separate gates.

## Host and supported runtime

The reviewed Host amendment permits bounded `serve-daily-stdio-v1` child work independently of ATLAS-B. Candidate `9023caa2db6f1bbee2bdf8b013d2aaefc66f3f10` changes only the child host and focused tests. It passed locked restore, Release net8.0 build, 38/38 focused regressions, diff check, and independent exact-tip review. The implementation preserves bounded framed I/O, same-thread ownership, per-session isolation, validation, and safe owned-root cleanup. Local process/lifecycle results do not satisfy Atlas integration, producer-reader-Host conformance, or distribution qualification.

The reviewed Host boundary targets .NET 8, which reached end of support on 2026-11-10. The reviewed distribution route is separate .NET 10 LTS admission and locked win-x64 qualification, preserving Core/P12 and without major-version roll-forward. .NET 10 LTS is currently supported through 2028-11-14 subject to a supported patch. See the [official .NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). No distributable Host claim is made.

## City Foundation and City Viewer

City Foundation candidate `75d72047e56339382b10e39bd7fde941bec40291` passed independent implementation review, locked restore, Release build with zero warnings/errors, City.Tests 34/34, City.Studio.Tests 7/7, and self-contained win-x64 publish. The user confirmed native generation for seeds 0, -1, 21, and 42; rendering; zoom; middle-button pan; Fit; map selection and map-to-list sync; Save/Load; and fresh-process Save/Close/Reopen/Load.

For saved seeds 0 and -1, the user confirmed pre/post Document ID, canonical result digest, seed/generator/version origin, and layout matched, with no regeneration. Digest values are omitted. No Foundation gate remains. List-to-map highlighting and cross-viewer selection consistency are deferred UX; the approved contract does not require them. No City v2 or optional work is authorized.

City Viewer design `ac2830876da184d88ef88ec5f32e92e4c0572698` has owner and consumer review PASS. Atlas is sole owner of its additive implementation paths. Integration remains serialized because project references, locks, RID behavior, solution membership, and shell transitions are shared files. City Studio and library pins remain with Simulation UI. Atlas must prevent RID propagation to City references and pass locked win-x64 restore before integration.

## ATLAS-C acceptance, rights, and notices

ATLAS-C candidate `68a4c1f760da3c65ce5211b82fd999baf9f0a6df` passed locked restores, RID graph checks, Release build, single-file win-x64 publish, direct tests 39/39, independent exact-tip review, and package-closure review. The six-project solution closure classified 50/50 required packages as present with lock metadata matching. These technical results do not clear source redistribution rights or pending native navigation.

The user opened the approved read-only Comb v1 Dungeon fixture in Atlas. The screenshot showed validated standalone status, document identity, eight rooms, seven passages, the element list, and selected-room Inspector; the post-view integrity check matched the approved fixture. This accepts component open/display/integrity. Shell Return-to-Demo (restored world selection, observation pair, zoom, pan, and focus) was not reported. Integrated return from Studio-authored Dungeon to selected Simulation world is separate. On 2026-11-28 the reviewed package was launched with permission, then the user stopped Computer Use with Escape before inspection or input. No acceptance result was obtained; this is pending access, not a failed product test.

Public promotion is held because redistribution rights for two imported Dungeon Studio source trees are unconfirmed. Git history is consistent with in-project development but does not establish legal title or redistribution grant. Obtain a written declaration or license from the rights holder or authorized representative covering the complete imported trees, public source publication, modification, and binary/package redistribution, with attribution and conditions. If employment or client terms assign rights, obtain authorization from that entity. Missing directory-level license files alone do not prove third-party ownership. Until reviewed, keep source and binaries local; do not change visibility or publish to another remote.

The audited six-project NuGet solution closure had known license metadata for all 50 packages; no other unknown package license metadata was identified. Windows native package `Avalonia.Angle.Windows.Natives` has a BSD-style notice requirement. Include its exact copyright, conditions, and disclaimer in a distributable containing that binary. This is a packaging obligation, not an unknown-rights finding.

## Dependency order and critical path

1. Keep the bounded Host child checkpoint local and independently reviewed. Architecture General is reassessing which Host work can proceed before Atlas reader integration; no gate is waived for end-to-end conformance.
2. Run the single authorized ATLAS-B project-root diagnostic only after the protected-path preflight. Then establish the complete locked build/test route, real producer-reader conformance, and exact-tip review.
3. Complete producer-reader-Host conformance before Atlas controls or the live session are accepted.
4. Complete the architecture reassessment of City-only implementation parallelism. Keep shared Atlas shell/project/lock/RID files serialized; pass locked win-x64 consumer restore before integration.
5. Complete ATLAS-C Return-to-Demo when Computer Use is available; separately verify Dungeon return to selected world and City open/return.
6. Qualify runtime and package notices, record exact release manifest, then complete integrated Daily-v1 demonstration and safe shutdown.

City Foundation is closed and does not block this sequence. Crypt v1 native work is parallel and does not block the first demo with an already-supported Dungeon profile. ATLAS-C public rights do not block permitted local acceptance or functional development.

## Current blockers and next milestone

- **ATLAS-B:** solution and Desktop project locked restore fail in graph processing with exit 1 and no root-cause diagnostic; current candidate is local-only and executable-unvalidated.
- **Host/Atlas:** real producer-reader-Host conformance and integrated controls remain.
- **ATLAS-C:** component open/display/integrity accepted; Return-to-Demo and integrated return pending; public redistribution waits for rights evidence.
- **City Viewer:** design/pin reviews pass; implementation and locked consumer qualification remain in the reserved serialized slot.
- **Distribution:** supported runtime qualification, exact manifest, and ANGLE notice inclusion remain open.
- **Dungeon source rights:** written declaration for the two imported trees remains the public-promotion blocker; provenance history is not a title determination.

Next product milestone: new authorization for one corrected ATLAS-B project-root diagnostic that proves its exact argv before launch, then an approved complete validation route and real producer-reader-Host conformance. The Vertical Slice remains open until the integrated demonstration and manifest pass.

