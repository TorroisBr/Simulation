# CORE-F P12-D Political Knowledge Core — Independent Review

## Verdict

PASS — validated candidate. This record attests the independent review result; it does not promote Core canonical, close P12-D, or complete CORE-F.

## Reviewed identities

- Implementation base: 0314023dd211eeca96e29c350e027b019652382a
- Implementation commit: 09a94f5fbe5013bdf9198f7b7a9079a56f450dab
- Implementation tree: 29bb5e659419533b25c59cf30652eb8518514511
- Validation-record tip: 0d83092f578ec197048252df0aadc5d3635d6be4
- Final docs-only applicability tip: ecea24650649202e4690a1c0b455d82fc76d8e6c
- Final candidate tree: 5948d0cc6e48fd04136108148c1265bb8271b213
- Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

Independent reviewer: /root/p12d_stage_review. The implementation diff was reviewed from the exact base through 09a94f5. The reviewer then checked both docs-only successors for exact-tip applicability; the checksum correction at ecea246 changed only VALIDATION.md.

## Findings and disposition

The reviewer found no actionable implementation defects. All eleven relocated source blobs and eleven relocated .meta blobs match their original base blobs. The census provider observes the same PoliticalKnowledgeStore instance. Core and test compile lists include the moved sources and focused tests. The review found the P12 section/schema/role/alias checks, ordered unique owner keys, revision preservation, detached copies, capture-token/vector checks, and fail-closed staging intact. No assertions were weakened and no unrelated ProjectSettings or user files entered the implementation diff.

Four focused Core tests cover owner identity/key/revision roundtrip, invalid/null rows with no partial output, revision/cardinality rejection, and empty-image staging against fresh roots.

## Validation evidence reviewed

The reviewer verified that the public Unity result-package ZIP size and SHA-256 match the public manifest and found no local workstation paths in the manifest. The validation record identifies Unity EditMode 3264/3264 and Smoke 5/5; it discloses that the filtered Smoke package was rejected due to an ancestor suite count mismatch, and that the five exact Smoke cases were cross-checked in the accepted full EditMode package.

The validation input inventories were rehashed after the code commit with zero missing or changed inputs. The docs-only checksum successor distinguishes the committed Git/LF manifest-content hash from the Windows/CRLF checkout representation hash. No executable input changed, so the reviewer did not rerun tests.

## Evidence limits

The reviewer could not access the protected CoreValidation TRX, raw logs, or private manifest, and therefore did not independently parse CoreValidation results. The reviewer independently checked the Unity ZIP's declared size and SHA against the public manifest, but could not decompress the ZIP in that review environment. These are retained as limitations; the record does not claim otherwise. The approved .NET TRX evidence adapter has not been delivered, so no permanent public .NET result package is claimed.

## Scope

This checkpoint moves the selected Political Knowledge/Faction/Property P12 sources and corresponding image codec/census provider into Simulation.Core. The Political Decision census provider remains Unity-owned in this slice. This work does not complete P12-D, P12-B, P12-A, CORE-F, detached application/restore, or the .NET host.
