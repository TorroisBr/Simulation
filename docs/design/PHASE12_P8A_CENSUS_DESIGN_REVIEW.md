# P12-B P8-A populated-geography witness design review

**Verdict:** `PASS` — implementation boundary is ready; no actionable findings.

**Reviewed design tip:** `codex/phase12/P12BP8APopulatedWitnessDesign` at
`0baba5cd1981950f9779f6e9772e41c88dc0cfbf`.

**Base and current canonical at review:**
`06145c7cbc258c56cc1be1a24adaa1751d32bc01`.

## Review findings

The design correctly uses separate schema-v1 sections for Hex, Location, and
scale-context cardinality, so a combined count cannot conceal a missing
required kind. Each provider uses the runtime-installed
`SimulationRuntime.SpatialAuthorityStore` exposed by the published bootstrap,
not the genesis source. The selected profile's expected values of one Hex,
one Location, and one scale context at revision 1 follow from the existing
atomic `TryComposeGeography` contract and current bootstrap test. Empty
identity-only state and failed composition remain exact 0/0/0 at revision 0.

The `SpatialAuthorityStore.Revision` is correctly treated as a conservative
shared parent stamp: P8-B passage/crossing changes may advance it while P8-A
cardinality stays unchanged. A future complete census must revalidate all
sections that share that stamp. The design does not claim a synchronized
snapshot.

The boundary is passive P12-B owner-cardinality evidence. It excludes P12-C
export/hydration, runtime registration, shared-epoch wiring,
thread/quiescence enforcement, capture eligibility, and P12-A readiness.
These selected-profile sections are required/populated. No product or
canonical architecture decision is unresolved.

The design is documentation-only, so no Unity tests were required. Its
`git diff --check` passed before submission.
