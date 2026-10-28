# CORE-F P12-D Core Owner Hydration Validation

Date: 2026-10-24

## Candidate identity

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base: `b7fbb9e4c3b4343a84a4bbd39e6af8e5753f3261` (reviewed P12-D selected-owner stager candidate).
- Implementation commit: `f555260716157a8ccf62453a39c298aac946014b`.
- Implementation tree: `d811828896ab2db6fcd48defff595aa8dac558b2`.
- Candidate code delta: five files; no ProjectSettings or evidence originals were staged.

## Delivered boundary

The P12-D restore stage now validates the paired D/F roster identities, hydrates the selected Core NPC owner vector from those rows, and only then constructs the Unity `NpcRuntime` projection. The staged Unity object binds the exact Core inventory, money, commercial, spatial, travel-plan, action-payload, and merchant-plan owners. The travel-plan projection binds after its captured value object is installed. A subsequent full selected-definition binding may upgrade the item-only merchant projection binding only when the typed graph, item asset, and City projection identities remain exact.

`MatchesRestoredP12DOwner` compares the already-hydrated owner vector against the detached row-derived restore state without reconstructing owners as part of validation. City-membership assembly remains deferred until all staged actors exist. This slice does not advance the day, replay an operation, attach a runtime-wide session, or publish the candidate graph.

## Validation

All current exact-source Unity run manifests reported stable input snapshots and passed:

- Focused `P12DNpcRootOwnerSnapshotTests`: 32/32, run `76ac732709b04a52b2c39de857d05526`.
- Full Unity EditMode: 3263/3263, run `4877b73f8eb4435bb6835a0a3290f1d1`.
- Historical `Smoke` name filter: 5/5, run `ef0a66968403416a849edb1712e1ce50`.
- Unity version: 6000.3.9f1.
- `git diff --check`: PASS.

The sanitized full EditMode package is `coref-p12d-owner-hydration-full.zip`, SHA-256 `75e3fd0563b09ab4dddcf576970e69a89441b24076ab228a229326ddea6614b0`. Its manifest SHA-256 is `4da3945bbc00e9a5bd1c90145192688b5f9b959bdfe134bcc449714ded855110`. The package passed both private-original comparison and archive-only verification. It contains the complete 3263-case NUnit result, including the focused and Smoke cases.

The filtered focused and Smoke source reports remain in the official protected local retention. The public packager rejects those filtered reports because Unity retains the full assembly `testcasecount` on the outer `Simulation` suite while the descendants contain only the filtered cases. They were not published as standalone packages or altered; the full-suite report packages successfully and is the public result authority here. A separate attempt using the NUnit `Smoke` category selected zero cases and is not counted as a gate; the passing 5/5 result is the `Smoke` name filter above.

Official CoreValidation on this exact commit passed 718/718 cold and 718/718 warm, run `ffd185e4cb864340acc1d123032a10a9`. The runner recorded commit `f555260716157a8ccf62453a39c298aac946014b`, tree `d811828896ab2db6fcd48defff595aa8dac558b2`, and stable compile/test inputs. It also reported that this checkout contains Unity `Library`; this run does not establish Library independence. The runner emitted `simulation-core-validation/v1`, not the separately required `dotnet-trx-source-context-v1`; therefore no public TRX package or original-to-public TRX comparison is claimed. Original TRX and the private CoreValidation manifest remain in protected local retention.

Two local ProjectSettings files remain modified in the worktree and are excluded from the candidate. They were not staged or rewritten by this checkpoint. The official Unity input snapshots were stable across each run.

## Limitations

This is an implementation candidate, not a Core canonical promotion or a P12-D closure record. It does not complete P12-D, P12-G, P12-A, Phase 12, CORE-F, or establish complete restored-graph validation, runtime-wide quiescence, capture eligibility, detached-host readiness, or deterministic continuation parity. The parent owner-stager review remains valid only for its exact tip; this implementation requires its own independent exact-tip review.
