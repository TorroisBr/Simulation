# CORE-F Core-owned restore admission validation

## Candidate and scope

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Code candidate: `f00c0152375b163e4cff9dd8ea7cb0de4a2ee84f`.
- Code tree: `d5397df32fa458543916627a376a281b29b9f9e9`.
- Code base: `781a607e1b5bc83be13f85d31a9b6e10474c25de`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The Core application now owns the completed/restored capture-token admission
decision used during restored-session publication. The runtime host contributes
only a fresh owner-section census and mutation epoch. Core checks the exact
bound execution state, owner thread, idle lease/operation boundary, token
provenance and identity, configuration/calendar/composition/world/day identity,
owner identities/cardinalities/revisions, and mutation epoch. A source session
replacement also requires the application's exact exclusive reservation before
Core requests census evidence under the replacement scope.

This does not move Unity P12 restore staging, owner-specific census collection,
or durable storage into Core. It does not deliver a detached .NET host, complete
CORE-F, or claim a full Phase 12 persistence package.

## Validation results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 747/747 PASS | `31b782ca81e7438bacbcdfaf67f8bea3` |
| Official CoreValidation warm | 747/747 PASS | `31b782ca81e7438bacbcdfaf67f8bea3` |
| Unity `SimulationRuntimeAdmissionTests` | 223/223 PASS | `7ad93a9b7eec42eba46b83603ccbd510` |
| Unity `SimulationRuntimeOrchestrationTests` | 13/13 PASS | `268cdbdd246f454e86cb96297f80ace2` |
| `git diff --check` | PASS | code candidate `f00c0152` |

The CoreValidation manifest records a stable input digest of
`d92dd4c268f5566a13f05964aa5433e37c365233dae6bdf0ccedc88ebbe53b80` across
373 inputs. It ran with the implementation and Core test changes present in the
worktree at base `781a607`; those six changed Core implementation/test file
hashes match code candidate `f00c0152`. The checkout contains a Unity `Library`,
so no Library-independence claim is made. CoreValidation's complete cold and
warm TRX files and manifest remain in owner-only retention.

The Unity runs used Unity `6000.3.9f1`, each with a stable input snapshot. The
two manifests have the same input digest
`877563e2389351f9198a9b5ce86e2aa050f9992e708095dd8ef2cf45e0ed1681`; hashes for
the current `SimulationRuntime`, Core application, Core runtime advance stage,
and affected Unity test sources match the committed candidate. The tests ran
before the code commit, on the same source files; only the Git commit identity
changed afterward. No code or test source changed between validation and that
commit. Pre-existing local `ProjectSettings` edits and untracked local data
were excluded from the commit and remain untouched.

| Unity filter | XML bytes / SHA-256 | Log bytes / SHA-256 |
|---|---|---|
| `SimulationRuntimeAdmissionTests` | 474,258 / `1c2a76ad759b2934638862b9b217e35e4af6ed3a2704ba3e013ba5ebcda1e1ff` | 94,479,019 / `4dbb073fcfd4a182a2acac367e92ce0bc44b696b6e865240b48aa20f9cba7d80` |
| `SimulationRuntimeOrchestrationTests` | 12,393 / `a7796b87038dd3942d329bc3aac62c468d0e805ff557bd6b61c5bc587c0c8e0d` | 202,617 / `2b41b26bc9afd5068134e735c977a218aeff23f77b1418d13f5c6d9ad49c7c04` |

The complete original reports, raw logs, and private manifests remain in
owner-only validation retention under their run IDs; they are not copied into
Git. No Smoke, full EditMode, LongRun, or full CORE-F acceptance gate is claimed
by this incremental restore-admission slice.

## Review and limits

Independent exact-tip code review is pending. No canonical promotion has been
performed. Core canonical remains
`a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; CORE-F remains open.
