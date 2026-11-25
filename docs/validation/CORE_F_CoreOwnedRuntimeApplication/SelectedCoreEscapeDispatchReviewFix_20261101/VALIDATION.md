# Selected Core Escape Dispatch Review Fix

## Candidate identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `6d7d5ffd96fd4152bbfd2daf6181eff4f8961c0c`
- Candidate tree: `ca9c3da4fac5fd6e21572dc4a8bee1af4ed011f5`
- Immediate parent: `934104f5174fd7e6283cac29be928d9a446d03e8`
- Full review base: `1255ed9a7e9f44191bead09d78482a1072bc97e6`
- Core canonical during validation/review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

The implementation commit `934104f` adds projectionless Core Hide/Escape dispatch and P12-compatible reconstruction of the exact Unity action projection. Commit `6d7d5ff` adds only the review-requested Escape assertions. The final candidate diff from the review base contains only `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs`.

## Validation

The official Unity runner recorded stable input snapshots for all successful runs. The validated source-file SHA-256 values were:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `ce98e045da5f958a816fed0c41dbab7c57baa9f0647fa07da29aa5ca468f8a9c` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `7469984778fbfe6bbd8d31aa9d56cbd9ba43ecaa699a81ded4c142ca9a276ae9` |

| Gate | Run ID | Result | Original XML SHA-256 |
| --- | --- | --- | --- |
| Focused projectionless Escape | `d0695d02976b4ab1a30133be7351abc3` | 1/1 passed | `840cf3bcb2d95f6d25c4090df9a99254dba48be8a681aabdb10d5f54733b5697` |
| Full EditMode | `d2cabe1abf4b442a8c21e1c42626f24b` | 3286/3286 passed | `548cbc2a7c7b50ca9cc6794ad86def977f14a30c7303b53e4156c640cce05ff6` |
| Official Smoke filter | `b6eae17ccaf54d16b8e7937e979b2350` | 5/5 passed | `406e11fad0e7f30bc291a7568a23deb929d7ea67b0b4b0bfea41a1969d48ef03` |

The full EditMode report contains all five Smoke cases, each passed. The separate official `Smoke` filter also passed 5/5. Its original filtered XML and log remain in the protected local evidence store; the public package contains the complete unfiltered EditMode report because the evidence packager rejects the filtered report's enclosing suite counts. No XML was edited or reinterpreted.

The complete EditMode XML was packaged by the approved Unity evidence packager and verified against its protected run manifest. The package contains only normalized test-result XML; original XML and raw logs remain protected.

- Package manifest: [`UnityEvidence/coref-escape-dispatch-reviewfix-editmode-20261101.manifest.json`](UnityEvidence/coref-escape-dispatch-reviewfix-editmode-20261101.manifest.json)
- Package archive: [`UnityEvidence/coref-escape-dispatch-reviewfix-editmode-20261101.zip`](UnityEvidence/coref-escape-dispatch-reviewfix-editmode-20261101.zip)
- Archive SHA-256: `29dba16f707f58b1dc901f098e331283bf3c714885480a5e0912a6564f2ccf76`
- Archive size: 174399 bytes
- Approved package verification: `VALID`, 1 run, 1 archive entry
- `git diff --check`: passed for the candidate implementation and review-fix commits

The runner captured HEAD `934104f` while the final test-file bytes were present as a local change. Those exact bytes were hashed in the stable input snapshot and are the bytes committed in `6d7d5ff`; the production file hash is unchanged from `934104f`. The independent reviewer verified both hashes against the candidate. Unrelated local changes were preserved and excluded from the candidate commit.

## Scope limits

This checkpoint establishes only the selected Core Hide/Escape dispatch adapter behavior and its P12 compatibility checks. It does not complete the Core-owned Daily-v1 runtime/application, detached .NET host, or CORE-F, and it does not promote Core canonical.
