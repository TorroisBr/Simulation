# CORE-F Core-Owned Census Capture — Exact-Tip Review

Date: 2026-11-12

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact implementation commit: `4a352c92d51b41699f0c4f1e55b6d5efe425c57b`
- Implementation tree: `8685f90ec847c87633081fec24cba8724721d537`
- Exact base: `039e101e5601330afa2a5925ce71185fa4ccacdb`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` (unchanged)

## Verdict

**VALIDATED CANDIDATE.** Independent exact-tip review found no remaining actionable source defects. The final commit contains exactly the five production/test files reviewed; their file hashes match the reviewed snapshot. The reviewer confirmed that malformed provider evidence faults the shared census protocol, while transient epoch or scope drift remains retryable as `OperationInProgress`.

Core now reads the sealed provider registrations in ordinal order, samples and rechecks its shared mutation epoch, enforces the complete 89-family census shape, and composes the exact owner-graph vector. The production `SimulationRuntime` uses this Core provider-capture path. The mixed-roster compatibility rows remain in the complete census, and restored continuation uses the same Core capture/composition boundary.

The Unity host still supplies the provider registry, validates its committed baselines, and supplies the Justice receipt projection identity. Writer registration, mutation invalidation, and residual mixed-roster compatibility ownership remain in the Unity census protocol. The legacy full-vector callback remains for host implementations that do not support the provider port. This checkpoint does not claim that all P12 mutation authority has moved to Core.

## Validation evidence

- Official CoreValidation run `93535b05a30643dea7b73402cd621d05`: **1,118/1,118 cold and 1,118/1,118 warm PASS**. The private manifest SHA-256 is `6710d8a4dbba4a46dc679119b8e1190b0de94f491c8cfd3ba2a7982f55c63784`.
- Official full Unity EditMode: **3,318/3,318 PASS**, run `2aed3da67d544a77b8a2d22401671551`.
- Official Smoke: **5/5 PASS**, run `19822ed07c2a490883d9b544760dc289`.
- Focused mixed-roster P12 capture: **1/1 PASS**; focused detached restoration continuation: **1/1 PASS**.
- `git diff --check`: PASS.
- Protected `ProjectSettings.asset` remained byte-identical during Unity validation.

Original reports and the private CoreValidation manifest remain in protected local evidence storage. Raw XML/logs are not copied into this repository.

## Scope limits

This checkpoint moves the live census read transaction and epoch sample into Core. It does not transfer selected-owner writer admission or all mutation invalidation away from `ContinuationCensusProtocol`; does not remove the legacy host-vector path globally; does not complete P12 or CORE-F; and does not expose public `DailyV1Application.TryStart` or establish the standalone .NET host. The next coordinated boundary is Core ownership of mutation admission and publication for the selected Core owner graph, with Unity retaining only residual receipt and mixed-roster compatibility responsibilities.
