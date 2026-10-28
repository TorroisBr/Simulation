# CORE-F Restored Boundary Admission — Independent Review

Date: 2026-10-28

## Reviewed candidate

- Candidate: `b463c439f1c6f14480bffa46b45dafa87d1a4f2a`
- Candidate tree: `69aaf9491024689af12989c51679434dc0b13a09`
- Base: `f8071d9670d76df94ed79112b40590150b4e9000`
- Scope: move restored continuation token installation and factual-read publication into one Core boundary-state admission, remove the token-bearing Unity host publication port, and give an unpublished restored candidate its evidence boundary without binding application advancement authority before normal publication.

## Verdict

**PASS — no actionable implementation findings.** The independent reviewer inspected the complete eight-file candidate diff and confirmed that the compatibility admission path remains, Core compares the prepared owner vector and epoch before token creation, and the final checks retain the exact admitted owner-section reference and epoch. Failed admission leaves the sequence, token, and factual-read latch unchanged; success installs the token and latch together. The source session is not mutated by candidate certification.

The reviewer could inspect the pushed GitHub diff but could not access the E: checkout or protected validation manifests, and therefore did not independently rehash the root tree or validation artifacts. The Master verified the local candidate tree and source identities, and checked each changed source/test file against the corresponding official-run input inventory.

## Validation evidence

Official runner results for the candidate source/test inputs:

- CoreValidation: `915/915` cold and warm; run `417d0c952b6f4cef895cec429c156686`.
- Exact restored-continuation regression on the pushed commit: `1/1`; Unity run `e706e0cb886243ec89a37124077f0ad9`.
- P12-D owner-census regression: `13/13`; Unity run `d71e26c82f524be0bd69602aa1960523`.
- Full Unity EditMode: `3275/3275`; run `6fa48fe56d434babb6169d791e4854d7`.
- Official Smoke: `5/5`; run `da443bd422bf453a80828a4ebfe9803c`.
- `git diff --check`: PASS.

Core and full Unity/Smoke evidence inputs were stable during their runs. The full Unity/Smoke manifests were captured before the implementation commit while the eight implementation/test files were the candidate worktree changes; after commit, all eight file hashes were verified against the corresponding manifest entries and match the committed candidate. The protected ProjectSettings changes remained outside the commit. Original reports, logs, and manifests remain in protected E: evidence storage; this review record does not copy or rewrite them.

## Limits

This is a bounded CORE-F implementation checkpoint only. It does not promote Core canonical, close CORE-F, or claim the remaining detached-host/T+1/T+2 package complete.
