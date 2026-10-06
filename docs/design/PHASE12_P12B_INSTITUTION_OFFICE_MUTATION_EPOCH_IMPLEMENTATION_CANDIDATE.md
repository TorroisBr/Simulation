# P12-B Institution/Office Mutation Epoch Implementation Candidate

Base: current `codex/phase12/canonical` at
`0daa72addc1f186d23713adf75f6c2f83a5aff9b`.

Design authority: `PHASE12_P12B_INSTITUTION_OFFICE_MUTATION_EPOCH_DESIGN.md`,
exact-tip reviewed PASS at `235d15e5c78a198de5f79f53fe36346dc942d95b`.

Validation: [P12-B Institution/Office validation](../validation/P12BInstitutionOfficeEpoch/VALIDATION.md).

## Delivered behavior

The runtime census registers four Required P12-E sections over the exact
installed owners:

- `p12e.institution.records` over the installed `InstitutionStore`;
- `p12e.office.records`, `p12e.office.incumbencies`, and
  `p12e.office.tenures` over the installed `OfficeStore`.

The selected Daily-v1 baseline is exact zero for all four sections. All three
Office witnesses share their owner identity and revision. The section inventory
increases from 235 to 239; this is a bounded addition, not a complete owner
inventory.

The sealed protocol declares `p12.institution-office.owner-commit`. Before
any covered owner mutator runs, the runtime checks its owner thread, validates
the exact changed-section baselines, validates mutation-epoch capacity, and
enters that operation. On success it preserves the existing
`PoliticalWorldRevision` update, then reports one logical commit to the shared
P12 mutation epoch:

| Runtime method | Reported sections |
| --- | --- |
| `TryRegisterInstitution` | Institution records |
| `TryRegisterOffice` | Office records, incumbencies, tenures |
| `TryAssignIncumbent` with explicit start day | Office records, incumbencies, tenures |
| `TryAssignIncumbent` convenience overload | Office records, incumbencies, tenures |
| `TryVacateOffice` | Office records, incumbencies, tenures |
| `TryApplyInstitutionalVacancyRecognition` | Office records, incumbencies, tenures |

Pre-commit admission failure maps to the existing
`InstitutionFoundationFailureCode.RuntimeFaulted` for the foundation APIs. The
vacancy-recognition result enum appends `RuntimeFaulted = 11`, preserving all
existing numeric values. Domain rejection leaves owner revisions and the P12
epoch unchanged. If post-commit census notification fails, the protocol faults
closed while the API continues to report the already-committed domain write as
successful.

Non-P12 runtime behavior is unchanged. Clone-time owner reconstruction remains
before the census baseline. This candidate does not add P8/spatial owner
registration or wire unrelated political mutation paths.

## Exact-tree evidence

The corrected implementation source commit `95fac36e02299e7683d9bcb33bccf581c1429e5d` uses the accepted `p12.institution-office.owner-commit` protocol ID. Its focused owner/operation suite passed 4/4, ALL EditMode passed 2410/2410, official Smoke passed 5/5, and `git diff --check` passed. The full-suite run confirms the selected Daily-v1 composition test checks the protocol ID and 239-section owner inventory. Exact XML/log hashes and source-file hashes are recorded in the validation report.

The initial validation evidence for implementation commit `1048073d03fd92767d0ae135800073da8bd60053` is retained as historical evidence only; that code used an operation ID inconsistent with the accepted design and its results are superseded by the corrected-tree runs.

## Scope limits

This candidate promotes no Phase status by itself. P12-B remains incomplete;
P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked. It makes no claim of
complete owner coverage, complete shared-epoch coverage, global quiescence,
capture eligibility, export, hydration, P12-A readiness, P13 readiness, or
Phase 12 closure.
