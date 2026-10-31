# CORE-F City Census Restored-Image Admission — B23FBB5

## Decision

**VALIDATED CANDIDATE** — bounded P12 owner-census composition repair only.

This record applies to code candidate `b23fbb5f60a9d8107a5fc4fb082c56751632f60d`,
tree `51f7cc0b3b1d5519c136764410c58863b94a3ae9`, based on
`6d03cde94097b742d2e126410e9e1123565eb463`. Current Core canonical at review
time was `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; the candidate contains it
as an ancestor. Candidate branch `codex/core/CORE-F-core-owned-runtime-application`
was pushed to the same tip. No canonical promotion was performed.

## Scope reviewed

The candidate composes the exact selected Core-owned City census rows in the
restored-image target-vector admission path before NPC Knowledge/Plan rebinding
and the P12-F structural completeness check. It complements the earlier
completion-boundary and final recapture insertions in the same candidate line.

The independent exact-tip code review passed with no actionable findings. It
verified exact Required role, schema, owner-instance identity, cardinality and
revision checks; stale rows remain available for rejection; mixed-roster City
rows remain intact; partial omission fails closed; and candidate failure occurs
before target boundary admission or publication. The resulting vector is checked
against the selected census count `61 + 22*N + U + P + 4*C`.

This candidate does not claim P12 completion, CORE-F completion, a Core-owned
`DailyV1Application.TryStart`, detached .NET runtime execution, or separate
process continuation.

## Exact-tip validation

| Gate | Run identity | Result |
|---|---|---|
| CoreValidation, clean committed candidate | `e32e5f32923947f58fb1475d0e3e2aea` | 984/984 cold and 984/984 warm; runner PASS; inputs stable. The run used a Unity worktree containing its `Library`, so it makes no Library-independence claim. |
| Unity EditMode, full suite | `8f52f10dec3f461f9bdb2374dce2a0db` | 3,284/3,284 PASS; no skips; runner PASS; inputs stable; exact candidate commit/tree. |
| Unity Smoke filter | `d917ac4903ba43c387ae7256f615475d` | 5/5 PASS; runner PASS; inputs stable; exact candidate commit/tree. These five cases are also present and passing in the full EditMode report. |
| `git diff --check` | candidate and validation worktrees | PASS. |

The exact-source Unity package is
[`coref-city-census-admission-b23fbb5.zip`](../validation/CORE_F_CoreOwnedRuntimeApplication/CityCensusAdmission_b23fbb5/coref-city-census-admission-b23fbb5.zip),
with manifest
[`coref-city-census-admission-b23fbb5.manifest.json`](../validation/CORE_F_CoreOwnedRuntimeApplication/CityCensusAdmission_b23fbb5/coref-city-census-admission-b23fbb5.manifest.json).
The archive passed the official package verification; SHA-256:
`ae78114f1493ba689116b8aad09f3605b1ec9a793b30d26a7afbfe1e901bf195`.
Protected original Unity XML and logs remain in the local evidence store under
their run identities. CoreValidation's protected manifest and cold/warm TRX
reports remain preserved under run identity `e32e5f32923947f58fb1475d0e3e2aea`.

The evidence packager rejected a separate public package of the filtered Smoke
XML because its suite-level `testcasecount` did not match its filtered
descendants. The original filtered report remains preserved; the full
EditMode package independently contains all five passing Smoke case identities.
No evidence tool or validation requirement was weakened.

The official Unity runner normalized line endings in
`ProjectSettings/EditorBuildSettings.asset` and
`ProjectSettings/ShaderGraphSettings.asset` in its disposable validation
worktrees. Git reports no content diff for either file under the repository's
line-ending rules. The active candidate worktree's unrelated ProjectSettings
changes were not staged or included.

## Integration constraints

- Keep this as an additive candidate checkpoint; do not infer Phase or CORE-F closure.
- Preserve the 89-family census contract, mixed-roster fallback, stale-token rejection,
  owner identity, revision and mutation-epoch checks.
- Continue with the Core-owned selected action/runtime integration and detached host
  only under their already approved contracts.
- Revalidate against current Core canonical if the candidate code tree changes.
