# Selected Core FleeCity Dispatch

## Candidate identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate commit: `b21c11f5f8dc31773232b6082db711a386222578`
- Candidate tree: `40f2b7e7910fc6b656006645bdb0edbd9584cdbd`
- Immediate parent and validation HEAD: `4c99e7ef82869fd4349b28f511fd868862076185`
- Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote candidate branch was fast-forwarded to `b21c11f5f8dc31773232b6082db711a386222578`.

The candidate changes only `SimulationRuntime.cs` and `SimulationRuntimeAdmissionTests.cs`. It reconstructs the Unity compatibility view for an admitted projectionless FleeCity action from the exact Core payload and exact city owner/projection, then uses the existing Core action dispatch and P12 TravelPlan commit boundary. It does not move the actor, debit the account, or add a second action authority.

## Source identity

The official full EditMode run captured stable inputs (`inputStable: true`) at its parent HEAD before the implementation commit. The committed candidate files have the same SHA-256 values as those exact validation inputs:

| File | SHA-256 |
| --- | --- |
| `Assets/_Project/Scripts/SimulationRuntime.cs` | `f0ff7bec22482eda39b4e6cf4f0cf449a033dfdcffa4421eb354958c8aefb5c9` |
| `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs` | `3dc87e300d79c2cd9b861e8838b5231a0478f40fd9574ca8652221ae2161723d` |

## Validation

All runs used the official Unity evidence runner, Unity `6000.3.9f1`, and stable input snapshots.

| Gate | Run ID | Result | Original XML SHA-256 |
| --- | --- | --- | --- |
| Focused projectionless FleeCity | `9c4cd6a5037643aebdc79cfa8725dffb` | 1/1 passed | `98b4c25196e7ff15759f773237e98a6df729a6407b9a27c561d02eaf80eaaa8e` |
| Full EditMode | `f22c74db10864b51b28c83cf991f881c` | 3287/3287 passed | `d40ef11b29365a566071033027e70b53ca5dd0bee60886df79e3f9921c453484` |
| Official Smoke filter | `0133cb9b12a343cc8bbb9ce2e9cde548` | 5/5 passed | `269cf7bbe10ba2dc482eb9918a41288872d46db41304350902ab584c05adc9d6` |

The full EditMode report contains the Smoke cases. The separate official Smoke filter also passed. Filtered reports and raw logs remain in the protected local evidence store; the public package contains the complete unfiltered EditMode report because the package contract requires coherent enclosing-suite counts.

The full EditMode report was packaged and verified by the approved Unity evidence packager:

- Package manifest: [`UnityEvidence/coref-flee-city-editmode-20261101.manifest.json`](UnityEvidence/coref-flee-city-editmode-20261101.manifest.json)
- Package archive: [`UnityEvidence/coref-flee-city-editmode-20261101.zip`](UnityEvidence/coref-flee-city-editmode-20261101.zip)
- Archive SHA-256: `b13173f885a5c67b7b5513a796a6deb887c026376999ba5c1c7e4a40d6d7dbe9`
- Archive size: 173965 bytes
- Package verification: `VALID`, 1 run, 1 archive entry
- `git diff --check`: passed before candidate commit

The package contains normalized NUnit result XML only. Original reports and raw logs remain unchanged in private evidence storage. No Core source changed in this slice, so CoreValidation was not rerun.

## Scope limits

This checkpoint covers only projectionless selected FleeCity dispatch and its P12-compatible action projection and pending TravelPlan publication. Core continues to own the selected-action eligibility, deterministic roll, travel planning, authoritative TravelPlan mutation, and P12 operation. It does not complete the shared Daily-v1 runtime/application, detached .NET host, or CORE-F, and it does not promote Core canonical.
