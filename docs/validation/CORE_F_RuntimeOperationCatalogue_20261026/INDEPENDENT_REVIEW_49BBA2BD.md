# Independent Review — Core Runtime Operation Catalogue

- Verdict: PASS — no actionable findings.
- Reviewed: 2026-10-26.
- Candidate commit: `49bba2bd9b1ef98d90a011f600539e460fd33e2d`.
- Candidate tree: `e3ccb01e8e66e1a2f3830c3ab813b5586a81897f`.
- Candidate parent: `ce4ef0625b0b8074b5c2e25b4475285b832c05b0`.
- Candidate branch: `codex/core/CORE-F-shared-daily-v1-app-host`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

## Review findings

Core now defines all selected-runtime census operation identities and returns the ordered expected-operation catalogue. Unity registers that catalogue while retaining the existing owner census providers, protocol enforcement, and mutation paths.

The catalogue preserves the complete prior operation set and registration order: the membership-only profile remains one operation; runtime admission enables the same bootstrap, daily, parentage, economy, owner-commit, and six population-lifecycle identities; Travel Party and solo travel identities retain their prior conditional inclusion. The tests verify all eight feature-flag combinations for exact cardinality and uniqueness, plus the full ordered 24-identity selected-runtime catalogue.

All Unity call sites now reference Core-owned identities. Existing rejection of an unregistered operation remains covered. The selected 89-family owner census, provider registration, mixed-roster fallback, and mutation semantics are unchanged. No action, owner, or capture scope was added or removed.

## Exact-tip validation evidence

Private owner-only E: evidence packages are retained by run ID; manifests record exact inputs and original artifact hashes.

- CoreValidation run `d982d5fba2bb4e82b92b901f8462e0e0`: cold **881/881**, warm **881/881**, zero failures. Manifest SHA-256: `5c62e96178b89360ce1e9d8670e4c8bd283aeb1cafe9d5585b0abd55c5f124fd`.
- Unity EditMode run `e172a533500e401a8346e2496bd79022`: **3271/3271**, zero failures, stable input. XML SHA-256: `679e90bfd75a53df8e104045f611887e2c8d1b221cedbf545e5e7f1b19bff23e`; log SHA-256: `5a92879472a061e54bc71aad9ef59bde4c0acdbf6b3c159f640fa23226050859`; manifest SHA-256: `0065c9635ccad5561536289313c93f5d355dcc3cd4f7fe2584c47f8915ea83f4`.
- Official Smoke run `f71becfd88ee48d6b7eda1efe79c60cb`: **5/5**, zero failures, stable input. XML SHA-256: `1520fd00c5f71632fff527e4533bf49751daea98e62037242590890cd5a8c9b3`; log SHA-256: `684c230e8f2e0c43843bc19dc84ed7aece9f4b9414c611ea4d93d180a4fadc73`; manifest SHA-256: `2752d409a846f93946d29f8b147f0713a5cf6728470152d1f11872c4bc28fa34`.
- `git diff --check`: PASS.

An earlier Unity attempt did not produce test XML because a replacement typo prevented compilation. That source typo was corrected before the successful full EditMode and Smoke runs above; it is not reported as a test failure.

## Scope and limitations

This promotes operation-catalogue identity and registration-order ownership into Core only as a candidate checkpoint. Unity remains responsible for the census provider set and exact 89-family evidence. The change does not complete the shared runtime or `DailyV1Application.TryStart`, move the owner census implementation, complete detached restoration, or close CORE-F. Core canonical remains unchanged.
