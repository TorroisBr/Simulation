# CORE-F Daily-v1 NoSave Genesis Gate

## Scope

This candidate closes the exact-tip review finding on its parent startup-admission checkpoint. `DailyV1Application.TryPrepareInitialGenesis` now requires a successful Core-owned continuation-slot inspection that returned `NoSave` and `exists == false`. An uninspected, failed, present, or otherwise blocked slot cannot reach genesis. The Unity bootstrap regression and Core tests exercise both the direct unproven call and the admitted missing-slot path.

This is a bounded startup-admission correction. It does not complete `DailyV1Application.TryStart`, the detached restore coordinator, descriptor-v2 persistence, a fixed .NET simulation host, separate-process T/T+1/T+2 continuation, or CORE-F. Core canonical remains unchanged.

## Source identity

| Item | Identity |
|---|---|
| Base candidate | `697d60cf02fc79301d2bc5a6cbe97708bb6b2ac8` |
| Implementation commit | `209dd9c120012d248c25c3d410bb7fedffb54f47` |
| Implementation tree | `1c77a1cf559d9399d8affaa427d1d05f0b8a291e` |
| Core canonical | `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea` |

The implementation commit contains only the Core admission guard and its .NET/Unity regression-fixture updates. ProjectSettings changes, generated caches, private evidence, and unrelated files were excluded.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Official CoreValidation, cold | 901/901 passed; zero failed, aborted, skipped, or inconclusive | Run `b5e7d3c0de7c460aabc94d686b49300c`; input digest `efe086433cbc063c4216fa858022e5f89263fc62dda464b4f6596e59d4f0b3d5`; stable inputs; source commit/tree match above; `Library` absent. |
| Official CoreValidation, warm | 901/901 passed; zero failed, aborted, skipped, or inconclusive | Same run and source identity as cold. |
| Full Unity EditMode | 3274/3274 passed; zero failed, skipped, or inconclusive | Run `6fee4f98d19446c59e9863468f5164aa`; stable input snapshot `6650b8c840cac7337c20a919a56921bee76ed44a28b9e2524b58b8345645db64`. |
| Unity Smoke filter | 5/5 passed; zero failed, skipped, or inconclusive | Run `5f1898c3a32c47ddbabe393485b819af`; all five case identities are also present in the full EditMode result package below. |
| `git diff --check` | PASS | Checked before the implementation checkpoint. |

The CoreValidation runner retains its original TRX, manifest, and logs in protected E: evidence storage. The current evidence policy still marks the normalized .NET TRX adapter as not delivered, so these private originals and their hashes are not presented as durable public TRX evidence and do not satisfy a canonical-promotion evidence gate. The CoreValidation result is reported for this candidate only; no promotion is requested.

## Unity result package

The full EditMode report was packaged with the approved Unity evidence tool and independently verified against its private run manifest.

The public package snapshot binds origin `https://github.com/TorroisBr/Simulation`, branch `codex/core/CORE-F-runtime-start-admission-proof`, HEAD `209dd9c120012d248c25c3d410bb7fedffb54f47`, tree `1c77a1cf559d9399d8affaa427d1d05f0b8a291e`, and Unity `6000.3.9f1`.

| Artifact | SHA-256 |
|---|---|
| `core-f-dailyv1-nosave-editmode-final.manifest.json` | `b17b9c32313f1dfbba0fa429b1d342c9eab93395ae4596a225d2fc0ae65740d3` |
| `core-f-dailyv1-nosave-editmode-final.zip` | `5c3e07d48775049b583db2f5c342b6ed270dc7970a374f3131f0d28a6257e0ef` |
| Normalized NUnit XML | `2b0892d6add5969b54a3b46a85fc29541c6c77939cb11854de589726603675ef` |

Verification result: `VALID`, one run, one archive entry, status `Passed`, with the 3274 case identities and counters bound to the package. Original XML and raw logs remain in protected local retention.

## Review status

The independent review of parent `697d60cf` returned `NEEDS_CHANGES`: direct Core genesis preparation could bypass the successful `NoSave` probe. This candidate implements the requested guard and direct-call regression. Exact-tip independent review of this successor is pending. No canonical promotion or Phase closure is claimed.
