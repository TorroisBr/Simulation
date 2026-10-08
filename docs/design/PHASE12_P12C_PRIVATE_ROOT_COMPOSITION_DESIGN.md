# P12-C Private Root Composition Design

**Status:** Narrow implementation contract for the remaining P12-C composition obligation. The P12-C capability scope is already accepted; this document does not add a checkpoint or require another scope-acceptance gate.

**Baseline:** `codex/phase12/canonical` at `6886f5876c756a7abb86c541f6d783da885941d0`. The promoted leaf implementations and their exact-tree review/validation evidence remain authoritative.

## Objective

Use the existing owner snapshots and private hydrators to validate and stage one coherent `UnityBootstrap-Daily-v1` continuation root. The leaf snapshots already cover the exact `RuntimeIdAllocator` families, `SimulationRecordSequence`, P8-A geography, the P9-B manifest/lineage, and the deterministic-random provider root. The missing behavior is a single private composition boundary that cross-checks the saved owner references and returns all staged roots together or none.

This is an in-memory P12-C staging result over the existing owner DTOs. It adds no serialized P12 envelope, runtime capture hook, publication path, whole-graph validator, or profile-wide hydration flow.

## Composition contract

Add a same-assembly internal stager that accepts the five existing snapshot values:

1. `RuntimeIdAllocatorSnapshot`;
2. `SimulationRecordSequenceSnapshot`;
3. `P12CSpatialAuthoritySnapshot`;
4. `P12CP9GenesisManifestSnapshot`;
5. `DeterministicRandomRootSnapshot`.

On success it returns one private staged-root bundle containing freshly staged owner instances. It returns no bundle on any rejection. It never mutates the inputs, active runtime, or source owners and never allocates a domain identity, advances a sequence, reads authored assets, runs a P9 stage, or consumes a random draw.

Before returning the bundle, the stager must:

- privately stage each owner through its existing owner factory, preserving each factory's schema, exact-family, local-cardinality, anchor, lineage, fingerprint, provider, and value checks;
- require both spatial and manifest snapshots to identify `UnityBootstrapDailyV1`;
- require the P8 snapshot's P9 contract identity and schema to equal the selected P9 identity and schema in the validated manifest snapshot;
- require the validated P9 `OutputOwners` list to identify `SpatialAuthorityStore` exactly once, while preserving the existing distinction between `OutputOwners` and canonical provenance records;
- compare the complete P8 facts with the retained P9-B authored-output provenance: require exactly one record of each `authored-hex`, `authored-location`, and `authored-scale` tag, and require that sole record to exactly match every corresponding P8 value. A matching record plus any duplicate/contradictory record is invalid. Use the existing producer's invariant-culture, length-prefixed record framing so values containing delimiters remain unambiguous;
- require the deterministic-random root seed to equal the P9 manifest's recorded effective seed;
- retain exact allocator counters and record-sequence next value from their owner stagers, without deriving counters from present entities or filling gaps;
- expose the staged bundle only after every owner and cross-owner check succeeds.

P8-A authored `HexId`/`LocationId`, P9 stage identities, and generated runtime IDs remain distinct identity domains. The selected P8 IDs are not values allocated by `RuntimeIdAllocator`. This slice can validate exact family membership, family uniqueness, positive/non-exhausted next values, sequence schema/value, P8's one-Hex/one-Location anchor, and the recorded P9 references. It does not claim global uniqueness against P12-D/E/F entity stores or maximum-sequence ordering against records that those later owners have not yet supplied. P12-G retains whole-graph uniqueness and publication checks.

## Tests and validation

Add focused EditMode coverage for:

- a coherent set of snapshots stages all five owners and preserves exact values;
- wrong P8 admission profile, P8/P9 contract or schema disagreement, and missing/duplicate P9 `SpatialAuthorityStore` output-owner reference reject the whole set;
- individually valid but disagreeing P8/P9 geography pairs reject the whole set when the Hex ID, coordinates, convention/order, terrain ID/revision, Location ID/anchor, or any scale field differs from retained P9 provenance;
- duplicate authored geography provenance tags reject the whole set even when one record matches P8 and another contradicts it;
- a deterministic seed different from the P9 manifest rejects the whole set;
- null owner snapshots, unsupported allocator/sequence schemas, duplicate or missing allocator families, invalid next values, malformed P8 cardinality/anchor, and P9 lineage/fingerprint corruption reject the whole set;
- every rejection returns a null bundle, and independently held source owners retain their exact values;
- the stager does not resolve assets, execute genesis, publish a runtime, allocate identities/sequences, or consume random draws.

Retain the already-passing exact-tree leaf suites. For the integrated implementation tree, run the new focused suite, the affected P12-C owner suites, ALL EditMode, official Smoke, applicable `SimulationRuntime` LongRun validation, and `git diff --check`. Record the exact code tree and raw test/log hashes, then obtain independent exact-tip implementation review before canonical promotion.

## Ownership and exclusions

Own only a new P12-C stager and focused test file; do not edit `SimulationRuntime`, `SimulationBootstrapComposition`, P8/P9/RNG/allocator owner implementations, Daily-v1 assets, ProjectSettings, or existing unrelated metadata. This finishes only the accepted P12-C root-composition obligation. It does not make P12-D/E ready before canonical promotion, complete P12-C until exact-tree review/validation and canonical State recording, make P12-A ready, or change P12-F/G or P13 dependencies.
