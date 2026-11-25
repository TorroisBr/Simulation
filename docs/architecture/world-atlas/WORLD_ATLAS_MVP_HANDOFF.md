# World Atlas MVP handoff

The user approved the product direction. The architecture candidate defines the first shell checkpoint and follow-on gates. Independent technical review determines whether the shell is ready. A ready shell can be built on an isolated branch when an implementation assignment starts.

## ATLAS-A · Standalone demo shell

**First dispatch.** Implement an offline Avalonia app with a 2D Hex viewport, pan/zoom/fit, keyboard navigation between visible Hexes, click selection and contextual inspector, world City list, observation file open/compare, status/error display and persistent Demo badge. Demo JSON has fictional data and exercises the same validation, joins and diff code used by a future real input. The MVP shell ships with a Windows x64 runnable folder, two committed fixtures and quick-start walkthrough. Authority facts are immutable render inputs and do not change when users select, filter or zoom.

**Files owned.** `WorldAtlas/WorldAtlas.sln`, subtree SDK/package pins and lockfiles, `WorldAtlas/src/WorldAtlas.Observations` (immutable wire reader/validator, joins and diff), `WorldAtlas/src/WorldAtlas.Desktop` (Avalonia window, viewport, panel and local picker), `WorldAtlas/tests/WorldAtlas.Tests` (ordinary .NET and desktop headless interaction tests) and `WorldAtlas/fixtures/demo`. Use two projects plus tests. Keep the future Lab producer outside Atlas. Use the Dungeon Studio project only as a reference for workspace, UI and packaging patterns. No dependency on Dungeon code, External TypeScript, official exports, save format, live labs, SDK changes at repo root or future scenario scope.

**Implementation input.** Read [the observation contract](WORLD_ATLAS_AND_LAB_INTEGRATION.md). To freeze the demo v1 contract use this exact sample with hex coordinates (-1,0) and (0,0); colors and marker offsets are presentation. The shipped first fixture uses this shape and a second complete fixture at index 1, day 2, completed sequence 2, same inputs/scope with true byte digest of the first file, and wheat quantity 25. Demo labels are intentionally fictional. The producer declares the same fixed opaque demo build and input fingerprint throughout. Canonical JSON test output uses strict compact UTF-8 without BOM and LF at EOF. Nested keys fixed as listed, ordinal row IDs, numeric coordinates/quantity, and boundary/index as decimal strings. The reader accepts insignificant whitespace and key order differences and validates a raw-byte chain. No automatic artifact discovery or open of the real profile in the demo-only slice.

```json
{
  "schema": "simulation.lab.observation",
  "version": 1,
  "profile": "atlas.demo/v1",
  "sourceKind": "demo",
  "worldId": "demo:world",
  "runId": "demo:run",
  "observationIndex": "0",
  "previousDigest": null,
  "boundary": {"absoluteDay":"1","completedSequence":"1"},
  "producer": {"id":"atlas.demo","version":1,"build":"fixture-v1","scenarioId":"demo-map","scenarioRevision":"1","compositionProfile":"demo","effectiveInputFingerprint":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"},
  "capabilities": {
    "geography/v1": {"scope":"registered-geography","coverage":"complete","sourceStamp":null,"hexes":[{"hexId":"hex-1","q":-1,"r":0,"terrainId":"plain","terrainRevision":"1"},{"hexId":"hex-2","q":0,"r":0,"terrainId":"hill","terrainRevision":"1"}],"locations":[{"locationId":"location-1","anchorHexId":"hex-1"}]},
    "cities/v1": {"scope":"world-city-roster","coverage":"complete","sourceStamp":null,"bindingCoverage":"complete","rows":[{"cityId":"city-1","label":"Sample Town","p8LocationId":"location-1"},{"cityId":"city-2","label":"Unbound Town","p8LocationId":null}]},
    "city-market-stock/v1": {"scope":{"cityId":"city-1","itemDefinitionId":"wheat"},"coverage":"complete","sourceStamp":null,"rows":[{"cityId":"city-1","itemDefinitionId":"wheat","quantity":20}]}
  }
}
```

For demo accept precisely the three specified capabilities with mandatory full declared scope, optional association encoded as null and sourceStamp always null. Data caps and compatibility rejection follow the file contract. V1 demo quantity is checked Int64; perform delta in decimal or checked bounds. Demo index/sequence/day use canonical nonnegative decimal Int64 strings. Diff UI shows scope and both cut stamps, explicit unavailable/empty states and never labels a diff as an event. Layout is original and simple: world context and observation picker, viewport, contextual Inspector and change panel; a list-only fallback is part of acceptance. In real integration relax demo-specific constraints only under its explicit profile review.

**Acceptance.** Open both fixtures by ordinary picker, pan/zoom, select an actual Hex drawn from data, follow its Location and inspect linked demo City stock 20 → 25 as a net observation. Unbound demo City appears only in the world list. Verify a legacy/unplaced Hex, unavailable detail, wrong predecessor, different world, dangling references, zero versus absent row, malformed IDs/limits, unknown version and last-good retention with purposeful .NET tests. Test signed axial extremes without wrap, hit-test/selection consistency and stable ordering. Headless interaction plus one launched Windows desktop smoke exercising file picker, mapping, labels, badge and diff. Build/publish with locked inputs, prove no Core/Unity/external project reference and review the exact candidate. Record test output, commands, SDK, source/tree, fixture and artifact hashes in the workstream; preserve original results under repository evidence policy.

## Dependency and dispatch

| Work | Status at design review | Gate or exact result |
|---|---|---|
| ATLAS-A shell above | Eligible for `READY_FOR_IMPLEMENTATION` after independent design PASS | Scope is demo-only and independently compilable. User's present assignment is documentation and review. |
| LAB-OBS-A source mapping design | `READY_FOR_TECHNICAL_DESIGN` | Use candidate source to prepare exact capture authority/ingress/revision map and fixture checks. Existing finite-source Lab scenario runs in its own scope and is available for separate demo improvements approved by that workstream. |
| LAB-OBS-A authoritative implementation | `WAIT_DEPENDENCY` | Consumes a promoted suitable Core host and reviewed owner-thread common cut across geography, City/anchor and selected Market. Owns official Lab start/advance and versioned output. Host-access review and supported identity evidence required. Source integration is serialized. |
| ATLAS-B real artifact integration | `WAIT_DEPENDENCY` | Requires ATLAS-A and valid LAB-OBS-A files. Verify actual one WorldId, advancing cut, supported Hex/Location and changed City stock, world list for unbound cities. Named role + source/build evidence. A real City-under-Hex demonstration has a separate legitimate binding prerequisite. |
| ATLAS-C document preview | `READY_FOR_TECHNICAL_DESIGN` once a versioned Dungeon library is selected | Dungeon team reviews minimal shared viewer extraction against real second consumer; use local source for read-only design now. Bound world view separately `WAIT_DEPENDENCY` on approved mapping, capture and supported profile. |
| City Studio, 3D, streaming, IPC, time controls and historical reconstruction consumer | `DEFERRED` | Separate product scopes or P13 capability. An Atlas event panel with one typed future capability can be separately scoped after stock comparison. |

For LAB-OBS-A, prove failure-atomic capture and publication, reentrancy and owner-thread refusal, stale/foreign cut and load replacement refusal, no successful observation during/after rejected advance, complete/unsupported scope, stable writer/reader compatibility through deterministic repeated runs, actual positive advance and copied stock change, producer/Atlas conformance and no unrelated persistence, Unity or profile obligation. Use actual .NET smoke with two independent client processes (Lab producer and Atlas consumer). Run Unity regressions only if the later chosen upstream capture implementation changes affected Unity-executed source under repository policy. Any build that merely loads fabricated observations is ATLAS-A evidence.

## Worktree, ownership and review

Documentation is on `codex/architecture/world-atlas` in the small sparse E: worktree `.worktrees/world-atlas-architecture`. First code dispatch uses `codex/atlas/standalone` on the accepted design commit and a dedicated sparse E: source worktree with just WorldAtlas, applicable docs and rules. Reuse it if suitable for later waves. E: had about 10.3 GiB free during preflight, below the reported 15 GiB threshold. Measure again before adding output; use process-local temp and SDK/cache paths and documented storage defaults. Limit caches/artifacts, keep candidate evidence private where appropriate, and use a properly provisioned isolated .NET runner if local capacity is insufficient. Retain active worktrees, all evidence and existing builds. Ordinary Atlas and Lab design requires no Unity project import or Library duplication. No cleanup or persistent PATH changes are part of the plan.

An Atlas Orchestrator owns build, .NET tests, desktop smoke, candidate integration and review requests. One Atlas Architect (General Architect can cover the initial slice) owns observation semantics, view parity and capability boundaries. Lab remains owner of scenario/start/advance and wire publication. Domain/Core owner signs any admission/reader change. Keep active CORE-F host/session/runtime, owner registrations, temporal/command paths, P12, root build/tooling, shared readers and evidence outside both the shell and Lab design diffs. Serialize later production edits in these areas with the active owner, rebase integration planning against actual promoted refs, and keep Atlas off the CORE-F closure graph. Dungeon owner reviews viewer extraction when requested. No permanent specialized roles are needed for the shell.

Require independent architecture review of this candidate, independent shell implementation review, exact post-promotion Lab technical/admission review by Core/reader owner plus independent validation, and independent real integration review with cross-process evidence. Future Dungeon binding and City/P13 features use their own accepted contracts and reviewers. The repository canonical workflow still governs publication. A reviewed proposal for this new initiative does not autonomously change canonical architecture; only a separately authorized canonical decision/promotion would do so.
