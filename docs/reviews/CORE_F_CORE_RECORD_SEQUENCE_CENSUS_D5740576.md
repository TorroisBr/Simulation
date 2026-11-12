# CORE-F Core Record-Sequence Census — Exact-Tip Review

Date: 2026-11-12

## Reviewed identity

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Exact candidate: `d5740576f934359f414876ee3f4ac9d2e4cd8f20`
- Candidate tree: `2cc215074c3677d91b24ba8bf11a9a2f7f328106`
- Exact base: `d9a64b2e650412c5b48c9528eb5cda5423d510c3`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- The candidate was published to its feature branch; Core canonical is unchanged.

## Verdict

**VALIDATED CANDIDATE.** Independent exact-tip review found no actionable defect.

The selected runtime owner graph now retains the exact `SimulationRecordSequence` used by its execution boundary. The graph binds the sequence once and preserves that same instance across owner-graph and membership replacement. Core derives the P12 census witness from the bound owner, verifies the source row's identity, schema, required role, cardinality, and revision, removes it from the host remainder only through the graph-aware path, and recreates it before every full-vector structural check. Duplicate, malformed, or stale source rows remain rejected.

The unchanged P12 inventory and cardinality rule remain authoritative: **61 + 22*N + U + P + 4*C** across the complete 89-family contract. Mixed-roster behavior is unchanged. The stale-token regression now changes the revision of the host-owned exact-empty P8-D route-observation row, leaving its role and cardinality intact; it no longer mutates a Core-bound owner whose revision is checked during composition.

The independent reviewer noted that a legacy no-graph removal helper accepts section IDs without checking record-sequence facts. That overload has no production caller in the reviewed path; live composition uses the graph-aware validation path. No change was required for this checkpoint.

## Validation evidence

- Official CoreValidation run `d5cb9c0e17214d3b8650cfe9386dc8ca`: **1,115/1,115 cold and 1,115/1,115 warm PASS**; input digest `3eb2bf8f051c9c4431505f76cdfe0de82e1c77ba0bdb7939dc92402a29e3b2f9`; inputs stable.
- Cold TRX SHA-256: `1755cc3ba288f8bae7e63cef5ddbd809b3f56b35f488928b0882fab4f606734d`.
- Warm TRX SHA-256: `f8dd0e274f5138e5fd008d6f9369cbbeef1f17a0bd10becd6a9f772fbb129268`.
- `git diff --check`: PASS.
- Unity EditMode and Smoke were not run for this checkpoint. The existing E: worktree had less than 2 GB free, and no new Unity import/worktree was started.

The original reports and private manifest remain in protected local evidence storage; they are not copied into this repository.

## Scope limits

This checkpoint binds only the `p12c.simulation-record-sequence` family to the Core owner graph. It does not establish Core ownership of the full live census protocol, all 89 owner sources or writers, Unity validation, `DailyV1Application.TryStart`, complete detached restoration, the independent .NET host, or CORE-F completion.
