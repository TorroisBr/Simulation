# CORE-F session-bound selected execution adapter

## Scope

The Core selected runtime advance boundary now retains one selected daily
execution adapter for the exact owner-graph instance it was built against.
It reuses that adapter while the graph identity is unchanged. After an
admitted roster transition replaces the owner graph, Core discards the stale
adapter and creates one bound to the new graph. The host factory is no longer
consulted again for every day on an unchanged graph.

This does not complete DailyV1Application.TryStart, detached .NET hosting,
or the full CORE-F runtime/restore contract. The existing Unity projection,
writer, and P12 adapter responsibilities remain.

## Source identity

The exact E: worktree was on branch
codex/core/CORE-F-restore-stage-transaction at source base
42da4175645f6448b88fc6ea7bfdc3e84ade374d, committed tree
8802423fac9d3978037389efada0e6ea39007e70. The tested change set comprised
the three source/test files below; their hashes bind the dirty tested inputs
to the later candidate commit.

| File | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs | f87ae6da47144984e638da59d054be14cd8d239693e0c17876f9e0cda6951e61 |
| Packages/com.simulation.core/Runtime/Application/DailyV1SelectedDailyExecutionStage.cs | 0df45408eef05fd8cab51e6bb4ebc4f532d7f5a7b43e67625143f2014d899f0c |
| tests/Simulation.Core.Tests/DailyV1ApplicationSessionGateTests.cs | 0e5859f5c829e62209ea5d7b3f921bf9912d2c2c101a0594bca71d3eebaf91e6 |

CoreValidation source/input digest: af9374c261a0f2a781284dc910280ad2f8ed1a0f519608aee2e1d21ba13e2fb3; the runner reported inputs stable.

The existing modified ProjectSettings/ProjectSettings.asset and protected
untracked validation files were not changed or included in the candidate.
The Unity input manifest records the ProjectSettings hash used by the run.
All generated caches and private evidence remained under the E: worktree.

## Validation

- Official CoreValidation, SDK 9.0.315, documented -SkipLab mode because
  this candidate does not contain the optional Lab project: 1,016/1,016
  cold and 1,016/1,016 warm. Run
  65d194f510974fadbd7180e2ad362eff; cold TRX SHA-256
  2c5a4fdff0e48aa96fd9c290883a443277972fd36cf0c84b82db7b6b054781c1,
  warm TRX SHA-256
  6579df524708e23e438938220385f7c6b63e71430961986e02ae4d79125b874f.
  The original CoreValidation reports and manifest remain in the protected
  .private-core-validation run directory. They use the native CoreValidation
  schema; no DotNetTrx public package is claimed.
- Official Unity EditMode, Unity 6000.3.9f1, Test Framework 1.6.0:
  3,299/3,299. Run
  0ceff10130f84a4c9d40560d4affcd43; input snapshot stable.
  Original report SHA-256
  5a09f8685c4e6effea68bb41d4df84643ac595a155cd9aa1802cc9cc5055a01c.
- Official EditMode Smoke filter: 5/5. Run
  e86b3ab45cdd4755aeebe7e06ed22a1b.
- Official EditMode LongRun filter: 7/7. Run
  f4afb57ef9fe46729171e04459524139.
- The full EditMode run's result-only package contains all selected cases,
  including the Smoke and LongRun identities. The official package verifier
  returned VALID (one run, one report entry). Tool version 1.1.0;
  normalized report SHA-256
  5c27f88eb0e3dae0fc0808e8be1ce6e4b13f793df653cf90b69fa296dfe3c7f6;
  package manifest SHA-256
  0c92b7a50b54d50e306163ace26847b2f9d739d0a58e07ad68b17d1d4a031589;
  ZIP SHA-256
  4c6bd1384a2f4e83db1e4a77cf48041a1954155e3be9997e078a44bb9be2615e
  (174,494 bytes). The package contains only normalized result XML; raw logs
  remain protected and were not published.
- git diff --check: PASS.

## Failure disposition

The first exact-E Unity run, d3ab7cafa4054c46aab7cff20b6ce769, ran
before the graph-aware cache correction and failed one existing test:
SimulationRuntimeAdmissionTests.DailyV1RestorePreservesDynamicNpcRosterTransitionAndContinuesDeterministically
(3,298 passed, 1 failed). The newly admitted roster transition replaced the
owner graph, but the first cache draft retained the prior graph's adapter.
Core now refreshes the adapter only when exact graph identity changes. The
final full EditMode run and the filtered LongRun run above passed on that
corrected source. The failed source version's original report and log remain
protected locally and are not presented as passing evidence.

One preliminary CoreValidation invocation was launched with a different
checkout as the process working directory; relative project paths therefore
did not validate this E: candidate. Those results are excluded. The final
CoreValidation run above was launched from the E: candidate itself. The
preliminary run artifacts were preserved and not used for any claim.
