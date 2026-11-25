# CORE-F selected Core Steal dispatch review

**Status:** `VALIDATED_CANDIDATE` for this bounded Steal dispatch slice only. This does not approve the complete CORE-F application/host package and does not authorize Core canonical promotion.

## Exact identity

- Repository: `TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`.
- Reviewed candidate commit: `a8a65d3dff1cf02713a5fbfea92171ad7f3998cf`.
- Reviewed candidate tree: `97a3a7a78ae76847e981a2a01f2c831523f136d9`.
- Exact parent/base: `7559de3026580278798b365a8b1c4d90b8eb8605`.
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.
- Exact-tip independent review: `PASS`.
- Reviewer: independent read-only review agent, not the implementation author.

This record and the corresponding update to the validation summary are documentation-only after the reviewed candidate. The production and test files remain exactly those reviewed and validated above.

## Scope reviewed

For an exact Core-owned Steal action slot, the Unity runtime adapter may reconstruct a compatibility `NpcActionRuntime` projection only from the admitted action payload, exact graph-owned target owner, and unique exact Unity runtime projection. Core remains responsible for selected action execution, deterministic RNG, money transfer, theft outcome, Justice write, and P12 mutation publication. Projection reconstruction does not create a second action authority or publish an owner mutation.

The test establishes a matching initial action slot/projection before the census baseline, then removes only the Unity projection fields while asserting the exact Core payload remains installed. Missing and duplicate target projections reject through `TryExecuteInstalledAction`; the stage's failure occurs before action roll. A counting random source is passed to the Core stage in each negative case, which verifies zero draws and unchanged owner revisions and P12 mutation epoch. The successful case verifies exact payload projection, deterministic stream identity, money revisions, theft/Person linkage, Justice revision, P12 epoch advancement, and final census.

The first review of the prior candidate identified a non-tautological RNG evidence gap: the test asserted zero draws on a random source not passed to execution. That finding is resolved on the reviewed tip by exercising the full Core stage in both rejection cases. No production defect was found.

## Validation evidence

The independent review verified that the focused, full EditMode, and official Smoke manifests report stable input snapshots and that their XML/log hashes match the validation record.

- Focused Steal test: `1/1 PASS`.
- Full Unity EditMode: `3289/3289 PASS`.
- Official EditMode `Smoke`: `5/5 PASS`.
- `git diff --check`: `PASS`.
- Full-run source snapshot SHA-256: `a4b24191c1c6b7bfcb98253843ee246071504e7a561f572c38383a8deca90b9d`.
- Full-run XML-only evidence archive: SHA-256 `ccfe11690a0bb2290084fe357e6e48413213593d1be98dafa02dc530a61bac52`.
- Official package verification: `PASS`; the archive contains one normalized full-suite XML report and no local Windows home path.

Run identities, source hashes, report hashes, package manifest, diagnostic dispositions, and the evidence-package limitations are recorded in [`Selected Core Steal validation`](../validation/CORE_F_CoreOwnedRuntimeApplication/SelectedCoreStealDispatch_20261101/VALIDATION.md).

## Review verdict and integration limits

The exact-tip review found no actionable correctness or test-integrity findings for this bounded slice. The review verifies compatibility projection reconstruction and the selected Core dispatch path only. It does not claim complete Core-owned daily execution, full SimulationRuntime extraction, `DailyV1Application.TryStart`, detached persistence/restoration, a fixed .NET host, separate-process T/T+1/T+2 continuation, package-level CORE-F validation, or canonical promotion. Those requirements remain open.

Any subsequent change to the reviewed production/test code requires proportional revalidation and a new exact-tip review of the resulting candidate.