# CORE-F Selected NPC Status Mutation Boundary — Validation

## Candidate and scope

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate tip: `1bcea1cb6bfda23dd7e5be3916aa5f158fa8c8c6`
- Candidate tree: `dcd89a39d1b30166c735f910a603a83e33899630`
- Candidate parent: `edbbeae276c1ea08690dbb8e30ff59bd853dc990`
- Candidate base for this bounded review: `9464929a1f7e7391a4977f5212db0c089a08f4ae`
- Core canonical is unchanged. This is a bounded candidate checkpoint, not CORE-F completion or canonical promotion.

The change adds a Core-owned selected NPC status mutation route backed by the admitted session's sealed P12 owner census. It validates and refreshes the exact status-owner row while preserving the full census vector, owner identity, exactly-one status-owner cardinality, revision and shared mutation epoch. Unity status projection remains optional for projection-independent Core actions. A real effect-free Normal action now executes through the no-projection Core path; a fabricated Normal result over a non-Normal slot is rejected. Forced Escape continues to require its wider host Crime/Justice operation; the no-projection fallback cannot replace an enclosing operation. The work does not broaden the accepted P12 profile or claim new owner coverage.

## Validation

- Focused .NET regression `CoreActorTurnUsesSealedCoreActionCensusWithoutHostMutationProtocol`: **1/1 PASS**.
- Official CoreValidation: **1,134/1,134 PASS cold and 1,134/1,134 PASS warm**. Run ID: `15d616ceb77b42848f3c7673deac0082`. The stable input digest is `d86fc2a89384214879fc1e879eecfa22a8ab4c0a5c4f29dbbf36147a03162a1c` across 541 inputs. A post-run digest recomputation matched; the manifest identifies the exact candidate commit and tree. The runner observed a Unity `Library`; no Library-independent .NET claim is made.
- Official full Unity EditMode: **3,321/3,321 PASS**, zero failed, skipped or inconclusive. Run ID: `58a5f2aeafba470eb76e3aab9ba0f592`; the manifest identifies the exact candidate commit/tree, and its captured input inventory was stable. All 1,982 current input hashes still match the captured inventory with zero mismatches. The protected local `ProjectSettings/ProjectSettings.asset` remains untouched and uncommitted; its captured content hash matches the committed blob.
- Official Smoke filter (`EditMode`, filter `Smoke`): **5/5 PASS**, zero failed, skipped or inconclusive. Run ID: `f7f333cf2c7644e98840ef55b56e845b`; input inventory stable. The full EditMode package also includes these cases.
- `git diff --check`: PASS for the implementation checkpoint.

The privacy-safe Unity package is at `Unity/core-f-selected-status-editmode-reviewfix.manifest.json` and `Unity/core-f-selected-status-editmode-reviewfix.zip`. The verified archive SHA-256 is `d542f1e4f64351b05a74fd68e7cc704a73c3a92f562c1044cdce649583d91c9d` (176,265 bytes). The original XML/logs remain in protected local evidence storage.

The CoreValidation runner's native manifest and original cold/warm TRX remain protected locally. No public .NET TRX package is claimed: this run did not provide the separate private `dotnet-trx-source-context-v1` required by the approved adapter. No context was fabricated and no original report was changed.

## Review and limitations

Independent exact-tip implementation review of `1bcea1cb6bfda23dd7e5be3916aa5f158fa8c8c6` passed; see `docs/reviews/CORE_F_CoreSelectedStatusMutation_1BCEA1CB_INDEPENDENT_REVIEW.md`. The candidate has not been promoted to Core canonical. It does not establish complete CORE-F acceptance, a detached host, or cross-process continuation.
