# P9-B — Authored Geography Source for Local Authoring v1

**Status: PROMOTED.** The user-approved contract passed independent design
review at `a30db22f9d8e3117a668c463a70d4100149387bd`. Its implementation at
`00395ef80cfa2364d34ed2170e0735d3a4b1513d` passed independent code and
integration review, required validation, and user-approved canonical
promotion. P9-B code is integrated at `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`;
the P9-B promotion-record State/status tip, before Phase 9 closure finalization,
is `14a2e8ee01e49f1ffdcecf8fd64d274c728ead44`.
Exact validation and promotion evidence is recorded in `../PHASE9_STATE.md`.
P9-B is a separate capability after promoted P9-A and does not amend P9-A's
accepted contract, code, or historical closure evidence.

## Baseline and ownership

- Architecture: `c285466c355103d3637ac165246591b72eb7bda0`.
- P8 capability baseline: `codex/phase8/canonical` at
  `470667d37863384edadb3d93ef64d8004aff46a3`.
- P9-A capability baseline: `codex/phase9/canonical` at
  `43f08b3dfbf042380c2f8a8b037bbf3ebd309ccb` (State-only closure history is
  retained by later canonical commits).
- Alignment constraints: `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md`
  and `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.

P8-A owns Hex, Location, terrain-reference and world-scale facts and the
`SpatialAuthorityStore` that validates, clones, diagnoses and reconstructs
them. P9-B owns the authored selected-profile input and its deterministic
genesis-stage composition/provenance. It must use the existing authority and
publication handoff. P8 remains closed; P9-A remains promoted and scope-closed.
P9-B adds no P8-C Ruin/site binding or LocalTopology facts. P8-C supplies the
promoted Ruin/site anchor contract; the separately approved P10-A profile owns
the bounded LocalTopology owner/migration seam.

## Objective and exact profile scope

Extend the existing authored Unity bootstrap as a separate versioned profile
capability that composes exactly one stable geographic Hex and exactly one
stable Location with that Location's single `AnchorHexId` referring to the
Hex. This gives downstream P10 a real P8-owned `LocationId` to which its
bounded Ruin profile can bind. P10 may not mint this Location or establish
regional geography itself.

The profile input must carry all P8-A reconstruction facts:

- stable `HexId` and one axial `(q,r)` `HexCoordinate` under
  `axial-hex-v1`, canonical order `q-then-r`;
- stable `TerrainDefinitionId` and `AuthoredRevisionToken` pair;
- stable `LocationId` and exactly one `AnchorHexId` resolving to the selected
  Hex;
- exactly one `SpatialWorldScaleContext`: resolved convention identity,
  source identity and version, positive distance-per-neighbor-step, and unit.

Existing P8-A content does not prescribe production terrain identity/revision
or a production physical-scale value/unit. Therefore the contract requires
explicit authored values with provenance but does not declare new universal
values or meanings. A tiny example such as coordinate `(0,0)`, one stable
fixture terrain reference, and `1` `unit` per neighbor step may appear only in
tests or explanatory examples; those fixture values are not the selected
production world defaults. No terrain catalog lookup is introduced.

## Genesis stage, identity and atomic publication

P9-A's existing authored-world/actor/validation/publication meanings remain
intact. P9-B adds one built-in stage with a stable identity/version (proposed
`p9.genesis.authored-geography/v1`) and declared typed inputs/outputs. It
depends on profile resolution and authored-world input resolution, and must
complete before actor-dependent final validation and publish. The accepted
stage graph must declare this dependency explicitly; no runtime registration
order, asset discovery, dictionary order or renderer state determines it.

The stage builds one `SpatialGeographyDefinition` and composes it by calling
`SpatialAuthorityStore.TryComposeGeography` on the selected world authority.
Composition occurs only while that authority is empty, so it uses P8-A's
atomic validation-and-compose operation rather than incremental registration
or a second geography authority. The composed authority is exposed only as
part of P9's existing `SimulationBootstrapComposition`/genesis handoff. All
stages and world authorities are validated before the handoff; no partially
published bootstrap is allowed. The geography exists before the first
simulated boundary and no work is added to `AdvanceDay`.

P9-A's `unity-authored-bootstrap/genesis-v1` identity and its promoted
compatibility record remain historical and unchanged. The P9-B profile must
have its own explicit versioned contract identity/fingerprint schema (and
reconstruction compatibility identity) so its additional P8 facts cannot be
silently represented as the old P9-A output. The exact accepted spelling is a
technical detail for review; it must be stable, included in the manifest and
not confuse P9-A reconstruction with the new geography-bearing profile.

The profile fingerprint/provenance inventory includes the geography-stage
identity and version; the selected Hex and Location IDs; axial coordinate and
convention; both terrain-reference components; scale identity, source/version,
numeric value and unit; stage input/output identity; and the deterministic
stage order/dependency records. Reconstruction must recover these inputs and
the resulting P8 authority state. Canonical serialization follows stable
semantic identity/coordinate ordering, never authored asset discovery order.
The stage performs no random draws; if a future extension adds random work it
needs an isolated, versioned context and is outside this proposal.

## Explicit exclusions and alignment

This checkpoint adds no procedural terrain, additional Hexes or Locations,
City/Market, settlement or population generation, backstory, P8-C Ruin/site
binding, LocalTopology, regional passage/route/travel, Knowledge, activities,
runtime expansion, code-mod API/loader, retrofit, P18/P20 gameplay, or
security/anti-tamper behavior. P9-B is an authored source/composition seam,
not a new geography generator.

Intraday/extensibility constraints apply to stable stage identity/version,
explicit dependencies and typed domain-owned output; P9-B creates no timed
work and does not wait for P18. Multi-participant alignment is a current review
constraint only: this geography stage creates no activity, actor, participant,
role, reservation or cardinality facts and adds no P20 dependency.

## Dependencies and downstream edges

```text
P8-A promoted geography authority ─┐
                                   ├→ P9-B authored geography profile ─┐
P9-A promoted genesis pipeline ────┘                                   ├→ P10-A Ruin + LocalTopology
P8-C promoted Ruin/site anchor contract ───────────────────────────────┘   (P10-A owns topology seam)
```

P9-B has no dependency on P8-B/D/E or P18/P19/P20. P10-A consumes the P9-B
authored `LocationId` and P8-C's promoted Ruin/site anchor contract, then owns
the Ruin binding and bounded LocationId-neutral LocalTopology seam. P9-B
contains no Ruin identity or LocalTopology facts. Its presence does not turn
P9-A's authored-only scope into a procedural-content promise.

## Implementation acceptance gates (delivered)

An implementation candidate must prove, at minimum:

1. One and only one stable geographic Hex and one anchored Location are
   composed by the new profile; identity-only legacy P9-A contexts remain
   distinguishable and P9-A's prior profile contract remains reconstructible.
2. P8-A required scale and terrain provenance survive clone, snapshots,
   canonical serialization, diff and invariant validation; cardinality and
   anchor relation are checked through the owning authority.
3. Reordered unrelated authored assets yield the same semantic output and
   fingerprint; changing each selected geography fact changes provenance and
   reconstruction projection.
4. A missing scale, incomplete terrain pair, duplicate identity, invalid
   anchor or nonempty target geography authority cannot partially publish the
   candidate; failure occurs before the first simulated boundary.
5. The authority is available via the normal bootstrap handoff and no parallel
   geography owner or daily-loop mutation is introduced.
6. P10-A can consume the resulting `LocationId` and P8-C anchor contract.
   P10-A owns the Ruin binding and LocationId-neutral LocalTopology seam;
   P8-C's delivered City/Site anchor behavior remains compatible.

Implementation, code/integration review, focused and full validation, and
canonical promotion are complete as recorded in `../PHASE9_STATE.md`. The P9-B
capability is available to P10 at its canonical code tip. This checkpoint
record defines the delivered bounded scope; it does not itself close Phase 9.
