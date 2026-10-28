# CORE-F P12-D City Core-owner Hydration validation

## Candidate and scope

- Repository: `https://github.com/TorroisBr/Simulation`.
- Candidate branch: `codex/core/CORE-F-p12d-core-hydration`.
- Code candidate: `05316bbefa1eb87ad40bda85aa90537616c2fffd`.
- Code tree: `6efccc9a1cb68346b9114b530529d15fb59e3299`.
- Code base: `509caeeb74eda2f13a6401d64e961ecf15870373`.
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`.

The selected P12-D City row-to-Core-owner boundary now resolves canonical City and Item definition nodes and creates the exact Core owner bundle in `Simulation.Core`. Unity resolves its location host object, then builds `CityRuntime` and membership projections from those returned owners. The nonselected legacy staging path and the persisted P12-D schema remain unchanged. This is a bounded restore-source integration; it is not P12-D or CORE-F completion and makes no detached-host or continuation-parity claim.

## Validation results

| Gate | Result | Run |
|---|---:|---|
| Official CoreValidation cold | 743/743 PASS | `dec268ac4fd546f79e1d82693d1eab9b` |
| Official CoreValidation warm | 743/743 PASS | `dec268ac4fd546f79e1d82693d1eab9b` |
| Focused Unity `P12DCityRootOwnerSnapshotTests` | 18/18 PASS | `6d30ba905c1c4eb09c657bd066c6b750` |
| P12-D Unity regression filter | 68/68 PASS | `e8e7a5c4107e4f6792881990d00078e1` |
| Full Unity EditMode | 3263/3263 PASS | `2061ae9d32454a7ca939d2c924bbe5b3` |
| Official Smoke filter | 5/5 PASS | `af89b6ef46d24858b3f3fb58874d55ab` |
| `git diff --check` | PASS | code candidate `05316bbe` |

CoreValidation's manifest records 373 source/input files and digest `58ab881613b19d2d2f38b97606e61a21f675352c01bda6b8800be9cf04fc64ff`. The run passed with no failed, skipped, or unexecuted cases. It ran from a checkout containing a Unity `Library`, so it does not establish Library independence. The runner manifest records the pre-commit source commit plus the dirty source snapshot; all 373 recorded input hashes were rechecked against the committed candidate after the commit and matched. The CoreValidation original manifest/TRX-like records and logs remain private; this runner emits `simulation-core-validation/v1`, so no public TRX package is claimed.

All Unity runs used Unity `6000.3.9f1` on the exact code commit/tree in a separate E: validation worktree with its own Library. The two preserved ProjectSettings inputs were hash-matched to the candidate before the run. All runs reported stable inputs: 1,878 files, digest `14916f88563b0fec63308b8e716fdb40a302a0a9e56f4465dab42b914dc62db5`. The focused, P12-D, and Smoke case identities and passing outcomes were all present in the full EditMode report.

## Result package and privacy

The complete normalized full EditMode report is packaged as
[`CORE-F-P12D-CityCoreHydrator-20261024.zip`](CORE-F-P12D-CityCoreHydrator-20261024.zip)
with public manifest
[`CORE-F-P12D-CityCoreHydrator-20261024.manifest.json`](CORE-F-P12D-CityCoreHydrator-20261024.manifest.json).

- Archive SHA-256: `56b813b5cb81ea54d350553e076418a53dedd23025a5003d489873d6910e4d0e` (172,262 bytes).
- Manifest SHA-256: `a038c0ed2c01492df3a7ecb585fb8234f815c98e216101fecfcac99d32c46fcf`.
- Official package verification against the protected original full-suite run: PASS (`runs=1`, `entries=1`).
- The packaged XML contains 3,263 passed cases, zero other outcomes, and no XML output payload. The public manifest contains no C: or E: workstation paths.

The original full-suite XML and log remain in private evidence retention; the 237 MB log is not in Git. Filtered XML and logs also remain private. Their test identities and passing results were verified in the complete full-suite report; no filtered reports are published separately.

## Review status and limitations

Independent exact-tip implementation review passed with no actionable findings. See [the durable review record](../../../reviews/CORE_F_P12D_CITY_CORE_OWNER_HYDRATION_REVIEW_05316BB.md), commit `b715fbad4a4871709b7827d1fdfd877f748dd485`. Core canonical remains unchanged. No canonical promotion, P12-D completion, full restore-graph publication, P12-G completion, detached .NET host, or T/T+1/T+2 parity is claimed.
