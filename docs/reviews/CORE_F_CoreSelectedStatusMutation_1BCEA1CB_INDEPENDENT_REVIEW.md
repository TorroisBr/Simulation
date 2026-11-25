# Independent Review — CORE-F Selected NPC Status Mutation Boundary

## Verdict

**PASS** — exact-tip implementation review of candidate `1bcea1cb6bfda23dd7e5be3916aa5f158fa8c8c6`, tree `dcd89a39d1b30166c735f910a603a83e33899630`, against base `9464929a1f7e7391a4977f5212db0c089a08f4ae`.

The review found no actionable defects in the bounded selected-NPC status mutation and Core dispatch changes. This is a candidate checkpoint only; it does not promote Core canonical or establish complete CORE-F acceptance.

## Reviewed behavior

- The status operation reads and updates the exact selected NPC owner from the admitted sealed P12 census, preserving the complete 89-family owner vector and exact owner identity.
- The status-owner row must have cardinality exactly one. Missing or duplicate rows are rejected before mutation; the regression covers cardinalities zero and two and confirms no epoch advance.
- A supported projection-independent Normal action is executed through the Core path only when the actor's exact admitted slot supports it. The test verifies a real state change and rejects substituting a fabricated Normal result for a non-Normal slot.
- Forced Escape retains its wider host Crime/Justice operation. The no-projection path cannot replace or split an enclosing operation.
- Revision and shared mutation-epoch changes, operation boundaries, and capture visibility remain consistent with the accepted P12 contract.
- Unity projection remains optional only for the reviewed projection-independent action. No duplicate authoritative owner or new profile scope was introduced.

## Validation reviewed

- Focused regression `CoreActorTurnUsesSealedCoreActionCensusWithoutHostMutationProtocol`: 1/1 PASS.
- Official CoreValidation run `15d616ceb77b42848f3c7673deac0082`: 1,134/1,134 cold and 1,134/1,134 warm. The 541-input digest was stable at `d86fc2a89384214879fc1e879eecfa22a8ab4c0a5c4f29dbbf36147a03162a1c`; the manifest identifies the reviewed commit and tree. The runner observed a Unity Library, so this is not represented as Library-independent .NET validation.
- Official Unity EditMode run `58a5f2aeafba470eb76e3aab9ba0f592`: 3,321/3,321 PASS with zero failed, skipped, or inconclusive tests. Manifest source identity and captured inputs match the reviewed candidate.
- Official Smoke run `f7f333cf2c7644e98840ef55b56e845b`: 5/5 PASS with zero failed, skipped, or inconclusive tests.
- The privacy-safe Unity package passed the approved package verifier. Archive: `docs/validation/CORE_F_CoreSelectedStatusMutation_20261116/Unity/core-f-selected-status-editmode-reviewfix.zip`; SHA-256 `d542f1e4f64351b05a74fd68e7cc704a73c3a92f562c1044cdce649583d91c9d`; size 176,265 bytes.
- `git diff --check` passed for the reviewed implementation checkpoint.
- CoreValidation native manifests and original cold/warm TRX remain in protected local evidence. No public .NET TRX package is claimed because the required `dotnet-trx-source-context-v1` was unavailable; no evidence context was fabricated and no original reports were altered.

## Disposition and limits

The implementation candidate remains on `codex/core/CORE-F-restore-stage-transaction`; Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`. This review does not establish a detached .NET host, cross-process continuation, or completion of CORE-F. The separately protected ProjectSettings edit and unrelated local evidence are outside the reviewed/publishable payload.
