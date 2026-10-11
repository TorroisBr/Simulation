# P12-G Pending ActorChoice restore failure — independent exact-tip review

## Verdict

**VALIDATED_CANDIDATE — PASS, no findings.** The review is independent of the implementation author.

## Identity and evidence

- Canonical base and exact parent: `104e437a9407fe61374abbd7d0ed88f5fd1c7bdd`.
- Reviewed implementation commit: `93dd4ddbe9434455c428039179fe7674b9a07ba5`.
- Reviewed Git tree: `e5553bad988dbc43077c9d3065e5839e1ddc2a09`.
- Reviewed Assets tree: `9f45fd146da5af7135774ef94dae6b2d7685a41d`.
- Candidate branch at review: `codex/phase12/P12GPendingActorChoiceFailureAtomicity`, tip `74e7705ce75c87eb9e75b350393c83eb460ab782`.
- Commits after the reviewed code are limited to validation documentation and artifacts; no Assets changes.
- Exact validation evidence and all six XML/compressed-log/decompressed-log SHA-256 values: [`../validation/P12GActorChoicePendingRestore-20261010/VALIDATION.md`](../validation/P12GActorChoicePendingRestore-20261010/VALIDATION.md).

## Review findings

The test uses the selected Daily-v1 Pedro SellGoods path to produce terminal `AttemptReturned/Succeeded` history. A null-default test seam transforms only the already-detached ActorChoice snapshot value to a structurally consistent `Pending` input, preserving its identities and the snapshot schema, revision, and sequence metadata. The P12-F staging path validates the Person binding and then rejects Pending through the existing `TryCreateP12FStaged` rule before `FActorChoicesStaged`; restore fails before F staging completes or publication.

The assertions cover unchanged active-session/runtime/composition identity, source token and health, current day, selected facts, continuation roots, complete included owner graph, source continuation parity against control, a valid unmodified restore retry, and parity after another completed boundary. The override defaults to null for normal restore calls and does not alter domain behavior.

The full diff is limited to the P12-F snapshot, P12-F owner package, P12-G coordinator, `TesteSimulacao` test forwarding, and the admission test. It stays within the documented P12-G failure-atomicity evidence obligation. It does not imply P12-G completion, P12-A or P13 readiness, capture eligibility, export/hydration, or Phase closure.

All committed validation XML and compressed logs match the manifest hashes, and decompressed raw-log hashes were recomputed. Results: focused 1/1, `SimulationRuntimeAdmissionTests` 187/187, `P12FActorChoiceSnapshotTests` 7/7, `ActorChoiceRuntimeTests` 11/11, ALL EditMode 2861/2861, official Smoke 5/5; zero failures, skips, or inconclusive results. `git diff --check` passes.
