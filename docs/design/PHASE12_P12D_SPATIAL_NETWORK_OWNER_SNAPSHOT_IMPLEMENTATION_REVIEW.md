# P12-D SpatialNetwork owner snapshot — exact-tip implementation review

**Verdict: `VALIDATED_CANDIDATE`**

## Review boundary and exact revisions

- P12 canonical base and current remote canonical at review: `ad4b20c42a25c9fd453c98695a79ce59490bf4fe`.
- Candidate ref: `codex/phase12/P12DSpatialNetworkOwnerSnapshot`.
- Exact candidate tip reviewed: `f6f5ff2507200d72e5c4194eae60cbc21559e65c` (tree `26555b96272a2c838083ce52871e069d99baa3d0`).
- Code commit: `39ad61d60907154c4af52ea1bf01bfe94322d90f` (tree `26cc3aa9a63de72c1cfd6e911249ec423c9f9f34`; validated `Assets` tree `1e00c94447bf202acf7517b14a5aed6043ae4eec`).
- Current P12-D technical design candidate: `0735103009b0161b3175349e9c78db241913de74`; exact design review PASS is recorded at `b9a0fd1b5941f0b7615a72acec39fd22e6c9ee0e`.
- Validation record: [`../validation/P12DSpatialNetworkOwnerSnapshot/VALIDATION.md`](../validation/P12DSpatialNetworkOwnerSnapshot/VALIDATION.md).

The candidate changes only `SpatialRuntime.cs`, `SpatialNetworkCensusTests.cs`, and the bounded validation evidence. The move from `5be03aa` to the reviewed tip `f6f5ff2` changed only `VALIDATION.md` to correct the two supplemental source hashes. The code commit and validated `Assets` tree are unchanged. No `SimulationRuntime`, bootstrap, owner-capture integration, gameplay, P12-A, P13, or canonical State code changed.

## Implementation findings

- The schema-v1 owner snapshot contains copied location runtime IDs, copied immutable route records, and the exact local revision. Collections are detached read-only copies; route records are copied rather than retaining caller-owned records. Capture preserves the existing location iteration sequence and route-list order.
- The staged factory rejects unsupported schema, missing collections, negative or impossible revision (`revision < location count + route count`), empty/null or duplicate IDs across location and route kinds, null/malformed routes, absent endpoints, self-routes, and non-positive stored durations. Valid normalized travel days are installed unchanged.
- Before registering anything, the factory validates the complete owner graph, checks every ID with the existing `RuntimeIdentityRegistry.IsRuntimeIdAvailable`, and checks that the shared registry census revision can accommodate the full identity count. It then registers the exact saved location and route IDs through the registry's typed `RegisterLocation` and `RegisterRoute` paths. It does not allocate replacement IDs or modify `RuntimeIdAllocator` state.
- Parallel routes with distinct IDs and the same endpoints remain separate records in their original order and are inserted into the derived outgoing-route index in that order. Reverse and different-duration routes retain their own endpoint identities and durations. No endpoint-pair deduplication is introduced.
- The factory constructs only unpublished local network state, rebuilds only `outgoingRoutes`, restores the owner-local revision directly, and assigns the output network only on success. Malformed state, registry collisions, and registry capacity exhaustion return no staged network and leave the supplied existing registry/owner unchanged in the tested rejection cases. The documented caller contract requires the supplied registry itself to be unpublished and discarded if a later staging operation fails.
- Tests cover empty and populated graphs; exact ID, endpoint, route and outgoing order; parallel routes; detached read-only values; revision gaps and subsequent increment; malformed snapshot rejection; and registry collision/capacity rejection without partial registration.
- Capture itself does not enforce the P12-B completed-boundary token, owner-thread/quiescence, or exact revision vector. That is an explicit later D composition responsibility in the reviewed design, and this candidate does not claim it. The slice also does not integrate site/city references, P8 geography, P10 LocalTopology, runtime/bootstrap publication, profile persistence, P12-A, P13, or Phase closure.

No semantic or scope conflict was found against the reviewed D owner boundary or the current P12 State, which identifies this isolated `SpatialNetworkRuntime` owner slice as ready and retains Genealogy as a separate promoted owner. Existing P12-B spatial owner and RuntimeIdentityRegistry census/revision evidence remains the authority for capture eligibility; this snapshot preserves only its domain owner's exact local revision and makes no broader census or shared-epoch claim.

## Validation evidence

The committed XML result roots and summaries independently parse as:

- `SpatialNetworkCensusTests`: 11/11 PASS.
- ALL EditMode: 2544/2544 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check` from the exact canonical base to the candidate: PASS.

The XML SHA-256 values match the manifest. The retained gzip logs decompress to the manifest's recorded raw-log hashes. The corrected source SHA-256 values match the exact Git blob bytes at code commit `39ad61d`; they are independent of this review checkout's CRLF conversion. The docs-only correction does not change the code or any validation artifact. Validation is therefore bound to the reviewed code commit and `Assets` tree. Tests were not rerun during this review.

## Result and integration constraints

This is a `VALIDATED_CANDIDATE` for the isolated legacy SpatialNetwork owner snapshot/staging slice only. It does not complete P12-D, authorize a whole-profile integration claim, make P12-A ready, unblock P13, or close Phase 12. Later integration must use the P12-B capture token and owner/revision vector, provide an unpublished staged identity registry, and preserve the current single-owner P8/P10 boundaries.
