# CORE-F selected P8-D/P8-E runtime owner identity validation

## Candidate identity and scope

- Candidate commit: `c8747e19a5261dd89a4936c384352d4306de9a51`.
- Candidate Git tree: `6fc835f536ef5cc7c2b1a51f9a3bc1376251fc19`.
- Bounded implementation delta reviewed for validation: `d99ab68703a206f2ad438e9cbdd286cab7fa01c6..c8747e19a5261dd89a4936c384352d4306de9a51`.
- Core canonical remains `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`; this candidate is not canonical and does not complete CORE-F.

The selected Daily-v1 `SimulationRuntime` now retains the exact Core graph's `SpatialRouteKnowledgeStore` and `PersonRoutePlanStore`, plus the exact `SpatialRoutePlanningSystem` and `P8ETravelTransactionCoordinator`, when their bindings match the admitted Person, spatial, route, and passage authorities. The runtime binds those exact owners/components back into the selected graph. Legacy runtimes retain their existing clone/construction path. Core genesis continues to require the new P8-D stores to begin exactly empty; live graph binding permits their legitimate later population. This does not change owner cardinality, P12 profile content, serialization scope, or travel semantics.

The identity regression uses the real `UnityBootstrap-Daily-v1` composition path. The prior fixture created an unpublished legacy runtime without a selected graph, so it could not test the identity contract.

## Validation

| Gate | Result | Run identity |
|---|---:|---|
| CoreValidation cold | 919/919 passed | `6e463d725c4449af9eae4b5db804ed04` |
| CoreValidation warm | 919/919 passed | `6e463d725c4449af9eae4b5db804ed04` |
| Official Unity EditMode | 3276/3276 passed | `aa7afa3298f04778b1aa946325a55e00` |
| Official Unity Smoke | 5/5 passed | `bcf57e241f114809a68d260e9c40af79` |
| `git diff --check` | passed | candidate delta above |

All accepted-run input snapshots were stable. CoreValidation used SDK `9.0.315`, reported no Unity Library in its isolated worktree, and recorded input digest `3d5c029a68e40c760c4aa32b1a1364cb77a42e921740643bbf39e7d6f670b2b4`. Unity used editor `6000.3.9f1`; both full EditMode and Smoke manifests bind to candidate commit/tree above.

The initial Unity import validation run `4dad801cb1244af18c637cf86ee71294` also reported 3276 passing tests, but its input-stability check was false because the first import changed local settings and generated two untracked package `.meta` files. That run is retained but is not used as accepted evidence. The warmed, stable run above passed. Those Unity-generated local files/settings were preserved and excluded from the candidate commit.

## Private original artifact identities

Original manifests, NUnit/TRX files, and logs remain unmodified in protected local evidence storage. This public record contains no machine-specific evidence paths or raw logs.

| Artifact | SHA-256 |
|---|---|
| CoreValidation private manifest | `876270b0e7eea7b2b0df89d3cf0ec4f3147e374f458d6032d79a0b19ab129f52` |
| Core cold TRX | `c7ae006201389c2fa9b808c3e2ca55a16aa3beb6f9ab459aa5af48565adb1fba` |
| Core warm TRX | `f34e9f35747eb211be6afec4199dce2b4688e0e42222c78a924eb53d28e0e1c1` |
| Unity EditMode private manifest | `c73d665d865860df9b5e3937aabbbf56bd23e05ded729f4f9e9aae22d4fcf370` |
| Unity EditMode report | `7473f4f69c8ca577d2047ca3ee93779289096ea69b40c6450c6a40ec405b42cf` |
| Unity EditMode log | `8bde3e256a59f629696871f751c05258ade05100c6cee8bbc5223f69d3f02463` |
| Unity Smoke private manifest | `3e8b52758e11602cc2d5f1431c6ba22148a67afeef83fc483a86b3045139caea` |
| Unity Smoke report | `de89d091498f79df89c0c706628400a01859cc4c293f22565646841b1b0786de` |
| Unity Smoke log | `e91583b3ca934d8c47c1c7cfc76dd0672b7e1224d4c9c49c4481b054a8c49873` |

## Review and limits

Independent exact-tip implementation review remains pending. A review attempt from the available independent worktree was denied access to the E: repository; no review PASS is claimed. The implementation candidate remains unpromoted. This bounded slice does not complete the Core-owned application lifecycle, detached restoration, .NET host, or T/T+1/T+2 continuation.