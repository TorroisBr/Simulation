# Independent review — selected planned NPC SellGoods dispatch

Verdict: **VALIDATED_CANDIDATE**. This is not a Core canonical promotion and does not close CORE-F.

## Reviewed identities

- Repository: https://github.com/TorroisBr/Simulation
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch tip reviewed locally and remotely: `6784690fa33c677ac8944bd4aab25f6eac2bbffd`
- Exact implementation/test commit: `008a910c6f43542e87ac2763594c957063f85c11`
- Reviewed implementation/test tree: `a74adaffc6162232100cf6353f56cfd40b6888d0`
- Implementation base parent: `6bae7cf7ba43a485fb0ea7ad866a001658d10b34`
- Later commits in the reviewed candidate add validation documentation and evidence packages only; they do not change the reviewed source/test tree.

## Review result

The independent exact-tip review found no actionable code or test issues. It confirmed the selected planned NPC sale projection remains bound to the exact Core action slot, target NPC, item and seller City; missing or duplicate projections fail before RNG or owner writes; Core remains authoritative for execution; and the compatibility projection itself adds no P12 mutation.

The reviewer’s earlier `NEEDS_CHANGES` was evidence-only. It is resolved by the completed normalized .NET TRX package and exact-source verification recorded in `VALIDATION.md`. The official TRX verifier confirmed the cold/warm original-to-normalized comparison, full 987/987 outcome accounting, and archive validity. The Unity package and source identities remain unchanged.

## Retained validation

- Focused planned NPC SellGoods: 1/1.
- SimulationRuntimeAdmissionTests: 240/240.
- CoreValidation: 987/987 cold and 987/987 warm; all non-pass outcomes zero.
- Fixed Lab validation on the same exact code/tree: PASS.
- Full Unity EditMode: 3293/3293.
- Official Smoke: 5/5.
- Official Unity and .NET evidence-package verification: PASS.
- `git diff --check`: PASS.

Evidence package hashes and exact run identities are recorded in `VALIDATION.md`.

## Scope limit

This validates only planned NPC SellGoods projection recovery and its Core dispatch gate. Shared runtime composition, DailyV1Application/TryStart, detached restoration, the executable .NET host and T/T+1/T+2 continuation remain incomplete. No Core canonical promotion is implied.
