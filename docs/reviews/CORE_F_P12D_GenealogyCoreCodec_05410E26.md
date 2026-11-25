# CORE-F P12-D Genealogy Core Codec — Exact-tip Review

**Verdict:** PASS — independently reviewed candidate.

## Reviewed identities

- Candidate evidence tip: 8726139962fdb1afb68e31433aae3083ff3042a7
- Implementation commit: 05410e2653d64dc36a8b8553c8c91906b7ddcf33
- Reviewed implementation tree: 6e0c55c46a5a85bf1aba2baadc89accdce8cfe2e
- Implementation base: ad0faf05751c9dcc038dc4c4f21c9450423dd6c4
- Core canonical at review: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Review result

The exact base-to-candidate implementation diff was reviewed against the P12-D design and current Phase 12 contract. The move places the Genealogy census provider, snapshot wrapper, and P12-A wire codec in Simulation.Core; it does not add or duplicate domain behavior. The moved codec and provider preserve their source behavior. The snapshot change only clarifies its authority boundary.

The relocated .meta GUIDs match the original files. Explicit Core and .NET test compile lists include the moved sources and focused tests. The Unity-facing callers retain the required internal access through the existing Assembly-CSharp and Assembly-CSharp-Editor friend-assembly declarations.

No actionable defects were found in section/schema identity, alias policy, revision preservation, deterministic ordering, limits, cycle rejection, decode validation, or fail-closed outputs. No ProjectSettings or unrelated user files are part of the implementation diff. The public validation manifest contains no local workstation paths.

## Validation evidence inspected

The public validation record and manifest agree on:

- Unity EditMode: 3264/3264 passed.
- Official Smoke: 5/5 passed.
- XML-only archive: 172,346 bytes; declared SHA-256 c07539ba037e62215c207d183e919395de458bdcb28d2b2f8059db06c8aab622.
- Public manifest SHA-256: 778d69d069a794ab6293a2f475b55b3eb5bc5616e52e9f2fca2c7516c085f5a1.

The public source-input hashes for the moved Unity sources agree with the published manifest under the candidate Windows checkout representation. Tests were not rerun for this review record.

## Evidence limits

The private CoreValidation TRX and manifest were not accessible to this independent reviewer, so the reported 790/790 result and 434-input crosswalk could not be independently parsed. The public record explicitly does not claim a public .NET evidence package. The XML ZIP bytes were also unavailable to the reviewer for direct rehashing; the recorded size and SHA agree between the public validation record and manifest, and the Git blob size matches the declared size. These limits are retained rather than treated as independently reverified evidence.

## Disposition

This is a review record for the exact implementation commit and tree above. It does not promote the candidate or close CORE-F/P12-D.