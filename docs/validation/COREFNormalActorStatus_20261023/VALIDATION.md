# CORE-F Normal actor status without Unity action projection

## Result

The bounded change allows an exact, targetless selected Normal action slot to
apply its admitted actor-side status plan through the existing Core status
owner and host P12 operation boundary without requiring a Unity
`NpcActionRuntime` projection. Target status changes and action payloads remain
outside this path. The regression removes both Unity action projection fields,
executes the real selected runtime entry point, checks that the exact admitted
Core status definition is installed, and verifies one mutation-epoch advance.

This is a selected action execution slice. It does not establish a shared
`DailyV1Application`, detached restoration, a detached .NET host, or CORE-F
completion.

## Exact validation input

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Committed base at execution: `4bc24b9b1fa3bbc3a849c2f9c1ade007150e9704`
- Base tree: `b136381727eeeef25be9b40242d2f6f4f10b8235`
- Unity editor: `6000.3.9f1`
- Unity full-run ID: `0f6d18a111ce49f0955e1c153ce08bb3`
- Unity input inventory digest: `8d3fd2148d6e09c58fc63f1502002c83ff8e78ee3f7c655db4585768a6adae03`
- Production source SHA-256: `fd80e3e1af959ac0770016c4f6926068cf8301443937736b08359fffa973719d`
- Test source SHA-256: `34939e47c34327141230ec2c1a38ecef53d93e92bfa96b76e5108d26fdfdcb43`

The Unity inventory binds the complete validation inputs, including the local
`ProjectSettings` bytes present during the run. Their pre-existing edits were
left untouched and are excluded from the candidate changes.

## Gates

| Gate | Result | Run identity |
| --- | ---: | --- |
| New regression | 1/1 PASS | `fc92eb8e8e6c462f8fa9738860297dca` |
| `SimulationRuntimeAdmissionTests` | 222/222 PASS | `89414a7024af413fbd4f3b51678af359` |
| ALL EditMode | 3,257/3,257 PASS | `0f6d18a111ce49f0955e1c153ce08bb3` |
| Smoke selection (`EditMode`, filter `Smoke`) | 5/5 PASS | `fda0cad19dfc4185b63d108e1c818d06` |
| CoreValidation cold/warm | 682/682 PASS each | `33c75597d726400f97b065d88cac6359` |
| `git diff --check` | PASS | changed code/test paths |

The official Unity evidence package contains the complete ALL EditMode result
and passed original-to-package verification. The five Smoke test results are
also present as passing cases in that full report. The separate filtered Smoke
XML remains in protected local retention: the packager correctly rejects its
partial NUnit hierarchy because parent suite `testcasecount` values retain
unfiltered totals. No original XML was edited or normalized outside the
approved packager.

The PlayMode-filter attempt selected zero tests and is not counted as a gate.
The successful CoreValidation run used `-SkipLab` from this candidate worktree;
the Lab project is absent here. Its protected manifest and TRX remain private
and are not represented as a published DotNetTrx package in this checkpoint.

## Published result-only package

- Manifest: [`coref-normal-actor-status-editmode-20261023.manifest.json`](EditModeEvidence/coref-normal-actor-status-editmode-20261023.manifest.json)
- Archive: [`coref-normal-actor-status-editmode-20261023.zip`](EditModeEvidence/coref-normal-actor-status-editmode-20261023.zip)
- Archive SHA-256: `56ee3f309dfed44fd080dc4253e90c2de891ee3f17e344f76f8f7dfb740dc195`
- Archive size: 171,939 bytes
- Original-to-package verification: PASS

Only normalized result XML is in the archive. Raw Unity XML/logs and Core
TRX/log evidence remain in their protected local retention directories.
