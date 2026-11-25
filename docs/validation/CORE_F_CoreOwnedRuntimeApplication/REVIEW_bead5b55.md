# Independent exact-tip review — explicit Core active-session projection

## Reviewed identity

- Candidate commit: `bead5b55383e73944b1d8b49623285fe7ebf5e13`
- Candidate tree: `fb5234bae45680a41519c4f1652b63c1c6ed04b7`
- Review base: `e48d43d0693e7df537f396ee31e85495cdefbd36`
- Verdict: **PASS**

## Review result

The review verified that a selected Unity session projection now requires an explicit `DailyV1ActiveRuntimeSession`, and that the supplied session matches the exact runtime execution-boundary state and selected definitions object. Genesis composition supplies its Core-created candidate; the restore coordinator and restore test fixture explicitly create and pass the candidate for the exact staged runtime. The new regression rejects implicit projection-side session creation. Existing restore continuation assertions remain intact and were not weakened.

One non-blocking wording note remains: a constructor diagnostic refers to a “genesis session” while that constructor also accepts restored sessions. No behavioral or contract finding was raised.

## Evidence review

The reviewer independently compared all 1,858 captured Unity inputs against the candidate worktree: zero missing files and zero hash mismatches. Private manifests show stable input digest `975bd0b0b40eb66d06b0763e26a9bc9d14132730dda4ed294035b6f3d2e0750a` for the two focused runs, Smoke, and full EditMode. Results are 1/1, 1/1, 5/5, and 3259/3259 respectively. The official evidence package hashes match `Evidence_core-session-explicit-final-20261024/VALIDATION.md`; its archive contains the normalized full-suite report. The reviewer confirmed no workstation paths or credential patterns in public evidence and `git diff --check` passes.

Protected ProjectSettings edits and unrelated private evidence are not in the reviewed commit.

## Scope boundary

This PASS applies only to the reviewed candidate commit and validation package. It does not complete the shared Core application lifecycle, detached P12 restoration, descriptor-v2 integration, .NET host, separate-process T/T+1/T+2 continuation, or CORE-F. It is not Core canonical promotion.
