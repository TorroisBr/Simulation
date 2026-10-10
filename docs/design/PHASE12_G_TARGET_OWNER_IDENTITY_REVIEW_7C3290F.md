# P12-G target-owner identity candidate review

## Verdict

**VALIDATED_CANDIDATE** — independent exact-tip review passed. This record
reviews the candidate and its evidence; it is not a canonical promotion or a
Phase 12 closure record.

## Reviewed identity

- Canonical base: `39474ce14ba30e643a453061d7a6eda26d15021e`.
- Candidate documentation/evidence tip: `fbc3301cd4a7f4d4e7a7bfdf2dbf70a83940b900`.
- Code commit: `7c3290f73a63e9a2fe1f08631fc9de1a98cf7fa6`.
- Code Git tree: `f58c6fab4a11b703b54d41e895db43896e3b1834`.
- `Assets` tree: `7f47a3fdea897ac608d52f1995d498854afbedeb`.
- Exact candidate branch matched the reviewed candidate; the candidate is a clean descendant of the stated canonical base.

## Findings

The increment is limited to test code and validation evidence. It adds an
exact target section-to-owner identity map for the successful restored graph
and exercises a substituted empty Expedition owner. The rejection happens
before publication; the existing shared harness checks preservation of the
active source session/token/health/graph, deterministic continuation against a
control, and successful retry. Production scripts, domain behavior, profile
membership, and persistence contracts are unchanged.

The target map derives its expected cardinality from the completed source
boundary. This is correct for the fixture: registering the P11 history Person
produces 300 rows, while the day-zero inventory fixture has 299. The candidate
does not encode a global fixed count.

Independent review found no defect requiring changes. The review verified the
exact candidate/base/tree identities, scope of the test diff, target-owner
identity and rejection assertions, validation manifest and artifact hashes,
`git diff --check`, and the protected/unrelated-file boundary.

## Validation evidence reviewed

The validation manifest is
`docs/validation/P12GGraphRejectionCoverage/P12GTargetOwnerIdentity-20261010-VALIDATION.md`.
It records final exact-tree results:

- Focused `SimulationRuntimeAdmissionTests`: 122/122 passed.
- ALL EditMode: 2796/2796 passed.
- Official Smoke: 5/5 passed.
- `git diff --check`: passed against the canonical base.
- XML and compressed/raw log hashes are recorded in the manifest; compressed
  logs were round-trip checked against the recorded raw hashes.

No validation rerun was needed for this review record because the reviewed code
and tree are unchanged.

## Scope boundary

This is one bounded P12-G rejection-coverage increment. It does not complete
the P12-G §6 matrix, all restore-boundary failure injection, causal no-replay,
or every multi-boundary continuation case. P12-G remains `WAIT_DEPENDENCY`;
P12-A remains `WAIT_DEPENDENCY`; P13 remains `BLOCKED`; Phase 12 remains `OPEN`.
No general serialization, capture eligibility, P12-A readiness, P13
readiness, or Phase 12 closure is claimed.