# CORE-F P12-D NPC Core-owner hydration validation

## Candidate and scope

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Base: `45325daf8798ff962ba82da7d3df2175e529cc93`.
- Code candidate: `f2f366bd6b3c10f43043ace7047df9a7bc29ac1f`.
- Code tree: `89a0593509a59e028e8b0c834556bd434b48cd32`.
- Core canonical at validation: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The slice moves selected P12-D NPC row-to-Core-owner hydration into
`Simulation.Core`. The Unity adapter supplies detached values and identity
sets, then binds presentation/compatibility projections to those exact Core
owners. Existing P12 rows and persisted format are unchanged. This is not P12-D
or CORE-F completion, canonical promotion, detached-host readiness, or a claim
of complete restored-graph validation or continuation parity.

## Validation results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 734/734 PASS | `313a8690a2c045bf986658642794d748` |
| Official CoreValidation warm | 734/734 PASS | `313a8690a2c045bf986658642794d748` |
| Focused `P12DNpcRootOwnerSnapshotTests` | 32/32 PASS | `52c8b82a97bb4514bb5a573da26e052d` |
| ALL Unity EditMode | 3263/3263 PASS | `b9171182641d401d9f9dc8dd52580036` |
| Official Smoke name filter | 5/5 PASS | `6939ea4ca85d41f49ff4bb8504071582` |
| `git diff --check` | PASS | code candidate `f2f366bd` |

The three Unity runs use Unity `6000.3.9f1`, each reports stable inputs, and
all share input digest
`cd63609138a68f008e478bdf8b27b0ddf8948d43fb14bb8b20453ad5ecfefe13`
(1,876 files). The inventory binds the exact code commit/tree, Unity executable,
tests, package inputs and ProjectSettings used. The two local ProjectSettings
files remain excluded from the candidate commit; their validation-worktree
bytes matched the preserved candidate-worktree copies after Unity's initial
import. Neither source checkout's user-owned edits were staged or changed by
the validation workflow.

The complete normalized EditMode report is
[`coref-p12d-npc-core-hydrator-f2f366bd-all.zip`](coref-p12d-npc-core-hydrator-f2f366bd-all.zip),
with public manifest
[`coref-p12d-npc-core-hydrator-f2f366bd-all.manifest.json`](coref-p12d-npc-core-hydrator-f2f366bd-all.manifest.json).
The archive SHA-256 is
`9b1039d70f803aa1b2f9a35a2eab90c9e0f5e9a7a21fec17119b8f9356789a7e`;
the manifest SHA-256 is
`dc217e9b93dbb7301059551a1a06dfaf220195fc239078d619eac5b4913a88df`.
The official verifier passed both archive-only integrity checks and comparison
against the protected original run `b9171182641d401d9f9dc8dd52580036`.
The package contains all 3,263 case identities and outcomes, including the
focused and Smoke cases; it removes XML output and projects the machine-specific
assembly root as declared by the packager.

Focused and Smoke original XML/logs remain in owner-only local evidence
retention; their filtered reports are not included as standalone public reports
because Unity retains the complete outer-suite count in filtered XML. Their
identities and passing results are present in the complete packaged report. A
separate `Smoke` category attempt selected zero cases and is not counted. An
initial focused run on the fresh Unity import passed but reported an unstable
before/after input inventory; it is not counted. The final focused, full and
Smoke runs above all have stable and identical input inventories.

CoreValidation's protected manifest records 371 input files and digest
`852dd392ab9792d6d170ad17652e285c7ff3d21313f9e00a85ececfa2ba94813`.
Its original reports and logs remain private; the runner emits
`simulation-core-validation/v1`, not the separate TRX evidence-context schema,
so no public .NET TRX package or raw-to-public TRX comparison is claimed. Every
one of the 371 recorded input hashes was rechecked against the committed source
after the adapter-only correction; all matched. The later correction restores
only Unity-side `MatchesSelectedInventoryRows`, which is outside the
CoreValidation input set and is covered by the exact-source Unity runs.

One earlier Unity attempt on commit `03751730` returned `NoResultXml` because
the extracted adapter still referenced the removed comparison helper. That
diagnostic is preserved in protected local retention and is not treated as a
passing run. The helper was restored in `f2f366bd`; the focused, full, and Smoke
gates above then passed on the corrected exact source.

## Review and limitations

Independent exact-tip implementation review is pending. Core canonical remains
unchanged at `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; no canonical promotion
or Phase closure is claimed. The broader shared runtime/application, P12
restore publication, detached .NET host and T/T+1/T+2 continuation remain
separate CORE-F obligations.
