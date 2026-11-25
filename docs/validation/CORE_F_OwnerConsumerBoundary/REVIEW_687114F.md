# CORE-F integrated owner-consumer candidate review — 687114f

## Verdict

**Independent exact-tip code review: PASS; no actionable correctness defect was confirmed.**

This is a review of the candidate diff, not a claim that CORE-F is complete or
eligible for Core-canonical promotion. Shared `DailyV1Application`/`TryStart`,
complete selected-owner closure, detached .NET hosting, and T/T+1/T+2
continuation remain outstanding under the approved CORE-F contract.

## Identity and scope

- Repository: `TorroisBr/Simulation` (`https://github.com/TorroisBr/Simulation`).
- Review base / Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Candidate branch: `codex/core/CORE-F-city-runtime-integration`.
- Exact reviewed candidate: `687114f33bc8d4ed81cdcccf15934ab178d3a6c0`.
- Candidate tree: `cb711113fe480ae4f0973e5cedecd139b7a67811`.
- Relevant source subtrees at the reviewed tip:
  - Unity production scripts: `31162c0bb9b19d6e0f81c1f74e6bb25e0ca9246c`.
  - Unity EditMode tests: `8b25f8928880512a38e586ebd8d3f73903a59fe7`.
  - Shared Core runtime package: `493c00ab58035f7a7e0358168caf6ea336e7f4c6`.
  - .NET Core production sources: `aada45c9a102286832f87f837f4f7860b83f153b`.
  - .NET Core tests: `9f684817a9ebbf49188c4aefce1ac519e5b60f28`.

The reviewed integration routes selected Merchant planning and reads through
the admitted typed Core definition/owner graph; binds Inventory and Market
operations to their selected owners and exact canonical item nodes; preserves
CommercialKnowledge observations, liquidity, revisions and restoration; and
maintains selected and legacy NPC/runtime compatibility. The review examined
the complete base-to-candidate diff, including P12 owner capture/hydration,
P18-D mutation and prepared-install paths, action selection, status projection,
identity binding, rollback, deterministic ordering, and failure atomicity.

## Independent review evidence

The independent reviewer examined the exact candidate and reported:

> No actionable correctness defect was confirmed in the diff.

The reviewer's own attempted .NET test invocation did not execute because the
environment denied access to the Windows SDK directory. This is an execution
environment restriction, not a reported source/build failure. It is not
counted as test evidence. The exact-tip validation below was performed and
preserved separately.

## Exact-tip validation evidence

- Focused selected NPC integration regression: 1/1 PASS; run
  `32c43aa6672d4c628bf72d2550235da3`.
  - XML SHA-256: `8b730afc3cfc57bf1f5bd86946f14f68634ea20e131a2576e5b69a4f2759866e`.
  - Log SHA-256: `49eeb5ca95f8f50c5bb5d149139b0a35214f3fbd480c9283a54e7e52933f224c`.
- ALL EditMode: 3209/3209 PASS; run
  `eb3b461a789c41d697d1abcb9326fef4`, exact candidate/tree above.
  - XML SHA-256: `3323b6223335c52b916cb7da2fc8d59ade7e9bb9c36acd58bbb69252f8634c23`.
  - Log SHA-256: `d3cac00af2be93d0bfb3fc62789e4828915581e9ea9658f1decd5552c382a26d`.
- Official Smoke: 5/5 PASS; run `96ecf3a2a7974054b2603992720e22e3`.
  - XML SHA-256: `9e86c0bdc41fb7524d39675d14a1bbb99886ea4354bb225c4a4151b289d98dc6`.
  - Log SHA-256: `f5e7cf78edb2d85a1fbf945a2c5812d98e7c7671b8292dac9764517c7bc139cf`.
- .NET CoreValidation: 310/310 PASS cold and 310/310 PASS warm; run
  `1d5a7751d7964fb58424d82e1ee38621`, target `net8.0`.
- Fixed Lab cold/warm deterministic scenarios: PASS; run
  `3a884b1b6d704a5fa6220a1e237f3dad`; output matched between attempts.
- `git diff --check a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea..687114f33bc8d4ed81cdcccf15934ab178d3a6c0`: PASS.

Private original validation artifacts remain in protected local storage. This
public record includes hashes and run identities only; it does not publish raw
reports, logs, machine paths, or private metadata. No test was rerun merely for
this documentation record.

## Remaining CORE-F boundary

This integration does not complete the approved CORE-F Daily-v1 host package.
The selected shared application and lifecycle, complete source/owner closure,
detached persistence/restore composition, fixed .NET host, and deterministic
T/T+1/T+2 continuation still require their approved implementation and
evidence. No P12-A readiness, P13 readiness, or broader profile compatibility
is claimed. Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
