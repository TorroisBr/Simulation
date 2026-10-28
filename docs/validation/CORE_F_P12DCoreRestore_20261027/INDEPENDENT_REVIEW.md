# Independent review — P12-D Core restore staging

## Verdict

**PASS — VALIDATED_CANDIDATE** for the bounded P12-D restore change in implementation commit `aa0295514c33b14ae89604e594f48f3bd293c77e`, tree `3c3e020816b051121125bc9817dc9357ded4cc5a`.

- Base: `86bd97a2100c221d882320ed9c7d240452413d6b`
- Core canonical at review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate branch: `codex/core/CORE-F-initial-restore-orchestration`
- Findings: no actionable defects.

## Scope reviewed

Reviewed only the seven-file P12-D restore change. Core stages the decoded spatial, Person, Genealogy, empty ExplorableSite and selected City/NPC owners under the application restore authority. The Unity layer wraps the exact staged spatial owner and projects the same selected owner instances. NPC P12 row conversion now resides in Core, and TravelParty IDs are derived from the decoded P12-F snapshot.

The production continuation and replacement callers pass the active `DailyV1Application` and its restore authority. Core builds a private staged result; the Unity identity registry is also private to staging. P12-G retains merged-graph validation, target-vector recapture, restored-boundary admission and publication. A failed stage does not publish the candidate or change the active session.

The accepted Daily-v1 empty-site boundary remains explicit: the snapshot and staged site owner must have zero rows and revision zero. No LocalTopology or other topology composition is introduced. Snapshot row order and exact runtime identity values are preserved; the Core row conversion checks the D/F RuntimeId match and copies the captured fields and commercial-share receipt values.

## Validation evidence

All runs report stable inputs. For the pre-commit validation runs, their source HEAD was the candidate's direct parent; hashes for every changed file applicable to each runner match the candidate's source bytes.

| Gate | Result | Evidence |
|---|---:|---|
| CoreValidation cold and warm | 903/903 each | Run `be9f2ae52038496f90d54658b39aec6f`; stable input digest `af76acbfa6ee5ce56746001e26fd9b58ae7cf4b418c2814833f507ccb1f12e89`; manifest SHA-256 `00a82e87b7c7c1cf30e119a74246783de1a579f0441e60adae16cfcfbc3c65cc`; cold TRX SHA-256 `773ae45d139e667032bbcbda6ab09705ee42636e7ddccaf30c3ae0b65583d35d`; warm TRX SHA-256 `4c427a198f866a8b59ad080b88a6d0f128931b1e5a2f075e87c1de71477ba5e7`. |
| Focused P12-G owner-vector regression | 16/16 | Run `f722e1197180461597ea5b5ee4ad6c0f`; input stable before/after at `b8eac97d7027e0cfc6959ef7010fe6682c7964af0860f5e2935e27e0de709f8c`; XML SHA-256 `a9b2139cf1caf99326d79edd008c68e56e37c8e4dac8d6e5d48d2bb701eb44d1`. |
| Full Unity EditMode | 3274/3274 | Run `bc26d6e6fa6b4061bfbe9ae65a122ebe`; public package archive SHA-256 `83fca5e9f4bbe152932efe5e982533b82266706c8b3d813a262cb64da6dd615a`; package manifest SHA-256 `e3085bd4254d0556aaddf7ff8d027a20122c657c63ed2d60a0f639010956030b`; original XML SHA-256 `8d55f1cb0baff9b76d5988e47a001dfacca52c91cbe53586ab895992655b1683`; normalized XML SHA-256 `97ba63c78d9ba181d5cf743cd10eecc0ce6659b139b107c05f9c6c15535acff6`. Archive and manifest hashes verify, and all changed source/test hashes match the stable input snapshot. |
| Official Smoke selection | 5/5 | Run `ad4ab8434fe04d0c872fb5cc7c88f51b`; stable input hash above; original XML SHA-256 `d04798a8569a3fb391ada297eda55dd506d3c052319925f3fd68459be5abb075`; log SHA-256 `aa0059209fb9a2750945a962d9b5d24b3121b906c07085adf251553de88707cc`. |
| `git diff --check` | PASS | Exact candidate commit. |

The focused and full Unity runner input hashes match. The CoreValidation manifest records the candidate's direct parent as HEAD with a stable working-tree input digest; all changed Core source and test files covered by that runner match the manifest. Unity's stable input snapshot likewise matches every changed Unity source/test file.

The filtered Smoke XML is retained privately, but its package was rejected because the filtered report retains the unfiltered suite `testcasecount` metadata. No normalized Smoke package is claimed. The full EditMode package remains independently verifiable. CoreValidation originals remain in protected local retention; the separate source-context adapter required for public DotNetTrx packaging was unavailable, so no public DotNetTrx package is claimed.

## Limits

This review validates only the bounded P12-D restore slice. It does not claim completion of CORE-F, P12-A readiness, or any canonical promotion. Existing unrelated ProjectSettings changes, untracked metadata and protected evidence were not modified.
