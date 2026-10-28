# CORE-F Selected NPC Death Owner Boundary — Validation

This is a bounded candidate checkpoint. It moves the selected NPC lifecycle
and current-action owner commit into the existing Core owners. Unity keeps the
P12 admission scope and publishes the resulting legacy object projection. The
larger Person/City death orchestration remains outside this slice; this record
does not claim a detached host, P12 readiness, or CORE-F completion.

## Implementation and source identity

- Validation worktree base: `04f998f10457b11d88b434b798a8b4308354705a`
  (tree `05c73df3a766e689d58b31c3901b69adcc9467d2`).
- The candidate source and test edits were present in the worktree during each
  validation run. The official runners reported stable inputs for the full
  Unity run. Private manifests bind the changed source files by relative path
  and SHA-256; no production or test source changed after the full run.
- CoreValidation input digest:
  `eca959891e26908a4c432f19ea256604b340230cc9bd3335abbe917a46eab5af`.
- Unity full EditMode input digest:
  `4683f3ccd668c79f535f845a185d92cde53e293c0c43aeea6d3d4b9d83923332`.
- Exact changed-file hashes are recorded in the runner's private manifests;
  validation evidence is tied to the files listed in the candidate diff.

## Results

| Gate | Result | Evidence |
| --- | --- | --- |
| Official CoreValidation cold and warm | 838/838 each, PASS | Run `2c00e059595f45199ab52c5edfa5656b`; private manifest SHA-256 `88b77bb88c70e9dc26637610764d0a150d039a36c09e747270cd9cfd9c5976c6` |
| P12 population lifecycle invalidation | 20/20, PASS | Run `d62ddb0616cc487d9b0ad028d4cb215b`; private run-manifest SHA-256 `4e35752ed7287d12f11e8e9b8255a82c1328be314fcec7bfd494a371b87b104a` |
| Selected runtime/Unity composition | 39/39, PASS | Run `cf3af5aa772e4fc3b1f253f3fbbc4a7e`; private run-manifest SHA-256 `01f3d53e7cd0c580628dcdfeb25b7f02145669f2684b8bed2981ea5c6386d52c` |
| Full Unity EditMode | 3271/3271, PASS; zero failed, skipped, or inconclusive | Run `47f767f0ff8d4d0aa267502737a588b6`; original XML SHA-256 `c597693f507462f4505c1bb958b7c5fddb85902365117d90953fb07b379d0243`; raw log SHA-256 `2cabba038b36caaf449bfd71baac1d2c95e4c51691573a53e9e2035e01dd4e7a` |
| Required daily LongRun cases | 7/7, PASS; included in full EditMode | Exact identities are in the full report; all seven `SimulationRuntimeLongRunTests` cases passed |
| Official filtered Smoke | 5/5, PASS; also present in full EditMode | EditMode filter `Smoke`, run `a46150e266df4207b1789a6178cfd05a`; original XML SHA-256 `0e62f1cca318655e86254c4f3c78929bf6a9bf06a4aeb5763135bbaea5e11927` |
| `git diff --check` | PASS | Checked against the candidate diff |

The normalized full EditMode archive is
`Evidence/coref-selected-person-death-editmode-20261026.zip`; SHA-256
`d7f0dcb14dd5f84d70b7ed9584857424c841ab6f91a510d2d83958db4879ef07`.
Its public manifest SHA-256 is
`5dc9721a6d2a9085d7e6332269082a32727d27a3238ecedf24356a4c30976f61`.
The official packager's `Verify` action passed and bound the normalized
archive to the private Unity run manifest. Original XML and logs remain in the
protected E: evidence store; the package contains normalized test results only.

The PlayMode Smoke filter in the generic runner discovered zero cases and is
not counted as evidence. The project Smoke cases are in the EditMode assembly;
the correct EditMode `Smoke` filter passed all five, and the full EditMode
report independently contains those same five passing identities.

## Scope limits

- Selected NPC Core life/action owner preflight and commit are covered,
  including revision exhaustion and stale action identity.
- Unity projection and existing P12 regressions passed. This slice does not
  move Person death validation, City population mutation, or the full
  cross-owner death operation into Core.
- P12 remains within its existing scope. No capture, restore, host, or phase
  readiness claim is made.
