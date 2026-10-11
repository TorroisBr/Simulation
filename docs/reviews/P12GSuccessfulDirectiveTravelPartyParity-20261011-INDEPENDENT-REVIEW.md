# P12-G successful ScheduledDirective + active TravelParty parity — independent review

Status: **VALIDATED_CANDIDATE**

Date: 2026-10-11

## Exact review target

- Current P12 canonical/base: `ab7c4b4e8347546c6bd8f4353d98353176b99807`
- Code candidate: `31241dab2e81d036b63f1b7a7d839f9d94f903e4`
- Code commit parent: `ab7c4b4e8347546c6bd8f4353d98353176b99807`
- Reviewed code Git tree: `080355aa35d8ebe4ccc6975441b47f59a7989c5d`
- Reviewed `Assets` tree: `7b8414ea93f73eef9a81a4f2a470035d79e0467d`
- Evidence candidate tip reviewed: `39e964d9e5659717ebd2567228030e48330d3806`
- Evidence commit parent: code candidate `31241dab2e81d036b63f1b7a7d839f9d94f903e4`
- Evidence-only delta: four successful XML reports, four compressed logs, and `VALIDATION.md`; no executable file changed after code validation.
- Candidate branch: `codex/phase12/P12GSuccessfulDirectiveTravelPartyParity` (local; remote publication not yet authorized)

## Review result

The independent reviewer inspected the exact code diff, test behavior, current P12-G contract, committed evidence, XML reports, hashes, and the candidate's unrelated-file boundary. No actionable findings.

The test uses normal bootstrap and domain flows: successful Steal, Guard Arrest, normal TravelParty creation with the actor and Guard excluded, restoration at a healthy completed day-2 boundary, and a due day-3 EscapePrison directive. It confirms the party is active when the scheduled effect succeeds, compares the full included-owner projection and continuation roots across source, uninterrupted, and restored executions, and checks day-4 no-replay behavior. No gameplay outcome is manually injected and no production code or profile contract changes.

The committed test file and working/index blobs match the reviewed code blob. The candidate diff contains only the test file and its validation record/artifacts; unrelated `ProjectSettings` edits and untracked `.meta` files are not included. Exact-tip `git diff --check` passes.

The reviewer independently confirmed focused 1/1, `SimulationRuntimeAdmissionTests` 193/193, ALL EditMode 2867/2867, and official Smoke 5/5, with zero failures, skips, or inconclusive tests. All XML, compressed-log, and decompressed-log SHA-256 values match `VALIDATION.md`. Evidence details: [P12GSuccessfulDirectiveTravelPartyParity/Final/VALIDATION.md](../validation/P12GSuccessfulDirectiveTravelPartyParity/Final/VALIDATION.md).

## Limits and integration

This checkpoint proves one bounded continuation-parity case for a successful scheduled EscapePrison effect while a TravelParty is active. It does not establish universal deterministic parity, complete owner inventory, writer/shared-epoch coverage, complete dynamic-transition coverage, restore rejection/atomicity, or P12-G completion. It does not change any phase readiness, P12-A readiness, P13 status, persistence/export/hydration scope, or closure requirement. Phase 12 remains open; P12-G and P12-A remain `WAIT_DEPENDENCY`; P13 remains `BLOCKED`.

The review is exact to the code and evidence tips above. Remote publication and canonical promotion remain separate gates; the attempted publication was rejected by the automatic security reviewer because it would export project-derived test artifacts to `https://github.com/TorroisBr/Simulation` without explicit approval for that exact payload.
