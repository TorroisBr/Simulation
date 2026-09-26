# P9-A — Authored Bootstrap Genesis v1

**Status:** `READY_FOR_IMPLEMENTATION`. Independent checkpoint-contract
review passed on the contract recorded at `f14586b`. This record is on a
candidate branch and does not claim canonical delivery or authorize promotion.

## Baseline and authority

- Canonical base: `codex/phase8/canonical` at
  `77f3e1a47a1e007492a794ea777d681a21a36d09`.
- Architecture baseline: `c285466c355103d3637ac165246591b72eb7bda0`.
- P8-E canonical promotion: `d95b60d174cb0b17df09e2775b3cbd134c74b21f`.
- Current impact constraints: both
  `architecture/INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
  `architecture/MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md`.
- Refreshed P9 entry, checkpoint-contract, and technical-design proposals:
  `codex/phase9/GenesisPipelineTechnicalDesign` at `a06db83`; independent
  refreshed proposal review `PASS` at `4895cf91b8205d3ba75db8f2d9e3da8e76804fde`.

This checkpoint translates those reviewed proposals and the user's approved
scope into a bounded implementation contract. Architecture remains controlling.

## Scope and exclusions

Replace the hand-ordered initialization of the existing
`TesteSimulacao.InitializeSimulation` authored bootstrap with a deterministic,
dependency-aware pre-start pipeline. This is a proving profile for the
pipeline. Its world/population extent comes from the current authored
`SimulationConfigData` and referenced content.

The profile preserves existing authored city population aggregates, market
stock/liquidity, production inputs, NPC runtime state, selected initial
Knowledge, warrants, and scheduled directives through their current domain
owners. It introduces no generated terrain, settlements, population, local
topology, pre-simulation backstory, P8 spatial facts without explicit authored
P8 identities/provenance, P8-E travel state, P18 timed activities, P20 shared
participation, runtime expansion, or new gameplay. Bootstrap NPCs remain
`NpcRuntime`-only; no `PersonId` is inferred from a definition or runtime ID.

P19's loader, public API, packaging, module lifecycle, and runtime installation
are deferred. Current extensibility requirements constrain the pipeline's
semantic seams, typed stage inputs/outputs, stable identities/versions,
ordering, compatibility, provenance, and domain ownership. No speculative
extension registry or security layer is part of P9-A.

## Fixed stage contract

The built-in stages and versions are:

1. `p9.genesis.resolve-profile/v1`
2. `p9.genesis.authored-world/v1`
3. `p9.genesis.authored-actors/v1`
4. `p9.genesis.validate-profile/v1`
5. `p9.genesis.publish/v1`

The fixed dependency edges are:

```text
resolve-profile → authored-world → authored-actors → validate-profile → publish
       └────────────────────────→ authored-actors
```

The implementation may use a small built-in stage manifest and deterministic
topological resolver. It must not add a public provider registry, contributor
loader, or general-purpose rules framework. Stable stage IDs break ties among
independent stages. Authored definition collections use their accepted stable
keys; order-sensitive authored lists preserve and fingerprint their source
order. Runtime allocation, asset discovery, dictionary order, and registration
order cannot determine semantic output or stage order. Genesis v1 performs no
authoritative random draws; the currently selected seed is provenance only.

## Profile inventory and row rules

The selected `SimulationConfigData` is required. Its normalized collection
properties define the selected rows: a null optional collection is treated as
empty only where the existing getter explicitly normalizes it; an empty list
is valid and creates no outputs. Every present row is selected and must either
resolve completely or reject the whole candidate. No malformed row may be
silently skipped to publish a partial profile.

- City rows must be non-null and uniquely identified by their stable authored
  definition identity. Preserve `initialPopulation`, markets, liquidity,
  population-economy inputs, production inputs, and authored connections as
  consumed by the existing owner constructors/systems. Every selected
  connection resolves to exactly one selected destination. Invalid/self
  connections and exact duplicate authored route keys
  `(origin DefinitionId, destination DefinitionId, travelDays)` reject the
  candidate rather than being omitted. Same endpoints with different authored
  travel-days remain separate route choices supported by the route collection.
- Explorable-site rows must be non-null, resolve a unique site definition and
  exactly one selected anchor city, and preserve the authored travel-days
  value. Duplicate site or route output identity rejects the candidate.
- NPC rows must be non-null, resolve a unique authored `NpcData` definition,
  and preserve starting-city, initial-money, ordered inventory, and ordered
  initial-known-site inputs. No stable Person identity is added in P9-A.
- Configured actions, statuses, jobs, items, warrants, directives and
  knowledge references are validated against the selected definitions and
  domain semantics before publication. Each selected warrant/directive must
  resolve its required owner references. Multiple warrant rows with the same
  target/city are valid many-to-one inputs: preserve their authored order and
  verify the owner record reflects the domain's ordered accumulation semantics.
  Do not reject that supported multiplicity as a duplicate output key. The
  current ordered warrant/directive list positions are causal input and remain
  ordered.
- Optional list-valued fields that the existing model normalizes to empty
  produce no rows when null/empty. Required object references for a selected
  row cannot be null. Invalid ranges, unresolved references, duplicate
  semantic output keys, and owner invariant failures reject the candidate.

Output completeness is checked against this normalized, validated input
inventory and owning-domain invariants, not against incidental host counts.
The profile does not require a nonzero count for an optional collection.
Existing domain-specific rules continue to decide whether an individual field
or action is valid; genesis adds whole-profile completeness and failure
atomicity, not adversarial-input protection.

## Compatibility identity and provenance

The compatibility identity is the explicit, versioned contract key
`unity-authored-bootstrap/genesis-v1`, with manifest schema version `1` and
the fixed stage identities/versions above. This key is always available in
Unity Editor and Player. Any code or rule change that can alter this profile's
authoritative outputs, canonicalization, stage behavior, or compatibility
must change the relevant contract/stage version. A repository commit SHA is
not required as a runtime input because the host has no source-revision
metadata path. `Application.buildGUID`, when present in a Player, may be kept
as supplemental execution provenance; its absence in Editor does not create an
unknown compatibility identity and it does not replace the contract key.

The versioned manifest records profile/schema/contract identity; effective
configuration and calendar values; stable identities and consumed values for
referenced authored definitions; ordered stage identities, versions and
dependencies; output owners and causal inputs; the current seed value/source;
and the first actually simulated boundary. Fingerprint canonical UTF-8 records
using length-prefixed fields and SHA-256, invariant scalar formatting and
ordinal ordering for unordered keyed inputs. Preserve the accepted authored
order for order-sensitive lists and include each position. Logging, display
names, rendering and incidental host discovery order are excluded. Exact
fingerprint mismatch means incompatible; no migration is implied.

## Atomic publication boundary

All candidate authorities, stores, indexes, systems and the manifest are built
privately and validated before host publication. The host exposes one
immutable `SimulationBootstrapComposition` (or equivalent) through one
private composition reference. Every public host accessor/API that exposes the
initial runtime or its authorities reads through that published composition;
candidate-owned members are not reachable through the host before publication.
After all stage, owner, cross-reference, profile-completeness, and
pre-first-boundary checks pass, the host performs exactly one reference
assignment. Any prior failure leaves the host unbound and creates no simulated
history. Normal runtime authorities apply after publication, and the first
actually simulated boundary is declared only after successful publication.

## Closure and validation evidence

The candidate must prove:

1. Identical compatible authored inputs and contract versions yield equal
   canonical owner state and manifest fingerprint.
2. Reordering unordered host collections leaves output stable; order-sensitive
   authored lists preserve order and affect the fingerprint.
3. Missing/duplicate/incompatible stage declarations, cycles, unresolved
   selected references, invalid required rows, duplicate output identity, and
   domain/cross-reference validation failures reject before host publication.
4. Exact output inventory covers authored aggregate population, city markets
   and liquidity, production inputs, NPCs/inventory, initial Knowledge,
   warrants and scheduled directives.
5. Injected stage, validation, and publication failures leave the host
   unbound, with no candidate stores reachable and no history before the first
   simulated boundary.
6. After publication, normal runtime mutation and daily execution remain
   available through the ordinary authorities.

Run targeted genesis/bootstrap tests, affected P8/E/P11 and domain regressions,
all required EditMode and official complete Smoke gates, plus `git diff
--check` before canonical promotion. Reassess long-run testing if implementation
adds daily-loop behavior; P9-A itself must not introduce new autonomous work.

## Dependency classification

- **Implementation dependency:** canonical P8 A–E are available at the base;
  the bounded profile does not create P8-owned travel/spatial facts. P8
  dependencies are conditional only if the actual selected authored profile
  introduces an explicit P8-owned output; it must then use that promoted
  owner/API and add its relevant regression coverage. P9-A selects no such
  output, so there is no P8 capability edge for this checkpoint.
- **No dependency:** P18, P19, P20. This profile creates no timed activity,
  participant, or mod-installed state. Existing scheduled directives retain
  their current semantics.
- **Downstream:** P10 implementation consumes promoted P9 genesis and the
  relevant P8-C local/anchor capability. P10 may retain its already reviewed
  design work in parallel, but its code integration waits for P9 promotion.
- The checkpoint does not satisfy any canonical capability until separately
  reviewed, validated, approved, and promoted.
