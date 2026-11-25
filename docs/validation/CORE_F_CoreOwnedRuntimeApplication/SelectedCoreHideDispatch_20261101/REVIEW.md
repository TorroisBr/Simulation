# Independent exact-tip review — Selected Core Hide dispatch

Date: 2026-11-01

Verdict: **VALIDATED_CANDIDATE — PASS**. No actionable findings.

## Identity

- Repository: `https://github.com/TorroisBr/Simulation`
- Branch: `codex/core/CORE-F-core-owned-runtime-application`
- Base: `2225ed5d06b3120b600acd09680f561e6a8415ce`
- Candidate: `9414824baadc31e59f87c01184054640b2f2003f`
- Candidate tree: `abe1455f154f9bd4020db3d26a77c66d0d8706ce`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Review

The reviewer examined the complete two-file candidate diff. Projectionless Hide dispatch is constrained to the exact enabled action definition and targetless payload in the selected owner graph, the exact bound NPC runtime, and a live admission context owned by the current thread. A present stale `NpcActionRuntime` projection is rejected. The existing Core Hide path enters the P12 Crime/Justice scope before owner mutations; the change does not add another action authority or bypass the Unity compatibility path for other actions.

The regression verifies rejected missing-admission and stale-projection paths leave owner revisions and the census mutation epoch unchanged. The successful path verifies deterministic draw identity/count, exact Justice-owner identity, and the two expected NPC Crime/Justice owner writes. No test assertion weakening was observed.

## Evidence checked

- Focused regression: 1/1 PASS, run `79e39434b63c47b88b01a2c3f942c1e2`.
- Full Unity EditMode: 3285/3285 PASS, run `d4d36344007c4a7da730e7d0a44e3649`.
- Official filtered Smoke: 5/5 PASS, run `2b0c4993ba4e4ffea3c989aa582a6e65`.
- Validated source hashes: `SimulationRuntime.cs` `49c0f516b7ebb6864b2fcde471e2d51c17ad701e9d5fe3beecc708ba598c0dd5`; `SimulationRuntimeAdmissionTests.cs` `b42ac3114b62df2cd8c5543e62b933cca2fe1c7b2ce2f62f30ff96bf863497df`.
- The test runs captured parent HEAD/tree before commit; the candidate commit changes exactly those two source/test paths, and their committed bytes match the run snapshots.
- Normalized Unity package verification: PASS; archive SHA-256 `d8bde1a52a256da88bec9165ea0b1982a7badef905d0c990f58f29e4438bbb85`; manifest SHA-256 `ea405fd516d9ab401a0522c04d2ba68f4f3e12bb53274ab0ac3aad077fcfbe90`.
- `git diff --check`: PASS.

This is a bounded candidate review, not canonical promotion or full CORE-F acceptance.