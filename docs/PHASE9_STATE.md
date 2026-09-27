# Phase 9 — P9-B PROMOTED; FORMAL PHASE-CLOSURE RECORD PENDING

P9-A remains promoted and scope-closed within its approved authored-bootstrap-
only scope. Phase 9 is reopened for the separately user-approved P9-B
authored-geography capability below. Independent design review passed on the
exact P9-B contract at `a30db22f9d8e3117a668c463a70d4100149387bd`; an isolated
implementation candidate proceeded. The implementation and required
validation are complete on `00395ef80cfa2364d34ed2170e0735d3a4b1513d`.
Following exact-tip integration review and user approval, P9-B was promoted to
`codex/phase9/canonical` at `d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`.
Phase 9 remains open only for its separate formal closure record.

## Current authority and candidate

- Current canonical architecture baseline: `c285466c355103d3637ac165246591b72eb7bda0`.
- Upstream canonical baseline: `codex/phase8/canonical` at
  `470667d37863384edadb3d93ef64d8004aff46a3`.
- Promoted P9 branch: `codex/phase9/canonical`.
- Source integration branch: `codex/phase9/GenesisAuthoredBootstrapIntegration`.
- P9-A candidate integration record: `12a1e0dfb8b856525a84a9f8373711e9584f95a9`.
- P9-A implementation candidate: `974a8d72a158962619d7ba8aa1ccf854eeadd47e`.
- P9-A code promotion commit: `43f08b3dfbf042380c2f8a8b037bbf3ebd309ccb` on `codex/phase9/canonical`.
- **P9-A status: PROMOTED and scope-closed.** Its original accepted scope and historical closure record remain intact. P9-B is a separately approved checkpoint, not an expansion or rewrite of P9-A.
- **P9-B status: PROMOTED.** Design, code, integration review, required validation, and user-approved canonical promotion are complete at `d9a62d7`.
- **Phase 9 status: PHASE-CLOSURE RECORD PENDING.** P9-A remains scope-closed; P9-B delivers the separately approved authored-geography source. P10 owns the next local Ruin/topology capability; public mod loading, retrofit and runtime expansion remain deferred consumers.

## Historical P9-A phase-closure review

- Independent closure review: **PASS** for this State/Brief closure candidate,
  against exact canonical baseline
  `988b6f5d14e12359e93464bae5e0048ca970ad86`.
- The review confirmed that P9-A satisfies the approved initial-world objective
  for the selected existing authored Unity bootstrap profile, with no other
  checkpoint approved at that time. This historical P9-A closure review does
  not review P9-B, whose separate design review is recorded below.
- The closure boundary preserves the approved limits: no new procedural
  terrain, settlements, population, local topology, generated backstory,
  runtime expansion or new gameplay. P9-A's existing authored content and
  deterministic pre-start pipeline remain canonical.
- Required implementation, integration and compatibility evidence is recorded
  below and in `docs/design/PHASE9A_AUTHORED_BOOTSTRAP_CHECKPOINT.md`.
- Historical P9-A State marker: **CLOSED**. Phase 9 is now reopened for the
  separately approved P9-B implementation path; no P9-A runtime files or
  behavior changed.

## P9-A — Authored Bootstrap Genesis v1

P9-A proves the dependency-aware deterministic pre-start pipeline against the
existing authored Unity bootstrap. Its scope, stage contract, selected-input
inventory, compatibility identity, provenance, atomic publication boundary,
reconstruction inventory and acceptance criteria are defined in
`docs/design/PHASE9A_AUTHORED_BOOTSTRAP_CHECKPOINT.md`. The approved first
delivery adds no generated terrain, settlement, population, local topology,
pre-simulation backstory, runtime expansion or new gameplay. P19 loader/public
API mechanics remain deferred. P9-A has no P18, P19 or P20 capability
dependency; the built-in profile creates no timed activities or
multi-participant state.

Current extensibility constraints are review constraints: explicit stage
identities, versions, inputs/outputs, declared dependencies, deterministic
ordering, compatibility and provenance, with outputs published through their
owning domain authorities. The checkpoint adds no speculative extension
registry or adversarial security layer.

### Candidate ancestry and review

- The implementation candidate was refreshed to include the then-approved
  Phase 8 State-only closure and subsequently integrated from current Phase 8
  canonical `470667d`; the architecture contract remains `c285466`.
- Independent checkpoint-contract review: **PASS** for the reviewed contract
  recorded at `f14586b`.
- Independent implementation review: **PASS** on exact code candidate
  `974a8d72a158962619d7ba8aa1ccf854eeadd47e`.
- Independent integration evidence review: **PASS** on the candidate record
  before this Phase State was added.
- P8 closure State wording change `77f3e1a → 470667d` was classified
  `UPSTREAM_IRRELEVANT` to P9 runtime behavior. No P9 runtime contract changed.

### Integration validation

The integration branch passed the following gates on the P9-A candidate:

| Gate | Result |
|---|---:|
| `SimulationBootstrapCompositionTests` | 10/10 passed |
| `SpatialRoutePlanningTests` | 20/20 passed |
| All EditMode tests | 1708/1708 passed |
| Complete official `Smoke` filter | 5/5 passed |
| `git diff --check` | Passed |

P9-A and the P11 Actor Choice candidate were also checked together
on the validation-only ref `codex/phase9/P11CompatibilityValidation` at
`004c6f99e72c63dfd374fca9197ba7ac0818ebca`. That ref passed Actor Action
Choice 6/6, Actor Choice 24/24, P9 bootstrap 10/10, all EditMode 1738/1738,
complete Smoke 5/5, and `git diff --check`. This is compatibility evidence.
P11 Actor Choice was subsequently promoted to `codex/phase11/canonical` at
`0803670cfa2c39163b54ff46a21daa06df5a16f6`; its post-promotion changes are
State-only, so the tested P11 runtime remains the promoted implementation.

No long-run suite was required: P9-A introduces no daily-loop behavior.

## Dependency and downstream status

- P9-A's selected authored profile does not consume new P8-owned geography,
  route, travel, or presence facts. P8's promoted baseline is recorded for
  ancestry and regression compatibility, not as an invented P9-A capability
  dependency.
- P9-A satisfies the authored-bootstrap genesis prerequisite. The approved P10
  Ruin profile consumes P9-B's promoted authored P8-A `LocationId` source at
  `d9a62d7` and P8-C's promoted Ruin/site anchor capability. Its remaining
  capability/design gate is the LocationId-neutral LocalTopology owner/migration
  seam; this does not reopen P8 or add P8-E, P18, P19 or P20 dependencies.
- P11 Actor Choice is independently canonical at `0803670`; the combined
  compatibility validation confirms the promoted runtime composition with P9-A.
- P18, P19 and P20 work remains governed by the explicit dependency edges in
  the Roadmap and Phase Briefs; none is pulled forward by P9-A.

Promotion record: the approved integration candidate was promoted to
`codex/phase9/canonical` at `43f08b3`; local and remote refs were verified.
P9-A's phase-closure marker remains historical evidence for that bounded
delivery; P9-B reopens Phase 9 only for the additional geography source
capability described next.

## P9-B — Authored Geography Source for Local Authoring v1 (PROMOTED)

The user approved a separate upstream authored geography prerequisite for
P10: one bounded P8-A `Hex` and one `Location` anchored to it, composed by the
selected authored Unity genesis profile. P8-A continues to own geographic
facts and authority; P9 owns the selected authored profile inputs, deterministic
stage composition, provenance and atomic pre-boundary publication. P8 remains
closed. P9-A's promoted behavior, accepted inputs, tests, and authored-bootstrap
scope are unchanged; P9-B adds a distinct selected profile capability.

The approved implementation contract is in
`docs/design/PHASE9B_AUTHORED_GEOGRAPHY_CHECKPOINT.md`. It consumes canonical
P8-A authority and the promoted P9-A genesis handoff, calls
`SpatialAuthorityStore.TryComposeGeography` with one explicit
`SpatialGeographyDefinition`, and includes that authority in the existing
atomic P9 publication boundary. It must not create a parallel geography
authority or mint a Location in P10. The chosen authored Unity profile carries
exactly one stable Hex and one stable Location/AnchorHexId, including P8-A's
coordinate convention, terrain definition/revision pair, and scale value/unit
with stable scale provenance. Values absent from existing accepted content
remain fixture-level authoring details and must not be generalized into
universal terrain or scale rules.

The new built-in stage has stable identity, version, declared inputs/outputs,
dependency, canonical contribution order, fingerprint inclusion, and
reconstruction records. It composes geography only into an otherwise empty
P8-A authority and completes before first simulated boundary through the
existing genesis handoff. Validation occurs before publication; there is no
partial profile publication. The stage's selected geography facts are
available to downstream P10, while P10 owns only its bounded Ruin identity
binding and finite LocalTopology capability.

P9-B expressly excludes procedural terrain, settlement/population/backstory,
City/Market, Ruin/site binding, local topology, travel/passages, Knowledge,
activities, runtime expansion, code-mod loader/API, P18/P20 gameplay and
security/attack concerns. Multi-participant activity alignment remains a
present-day review constraint but adds no P9-B facts or dependency.

**Dependency edges:** P9-B requires promoted P8-A geography authority and the
P9-A genesis pipeline/handoff. P10's Ruin profile requires P9-B's authored
`LocationId` source and the separately promoted P8-C Ruin/site owner and
local-topology capability. These are capability-specific edges; P8-E travel
and P18/P19/P20 do not become blanket prerequisites.

**Design review:** Independent review **PASS** on exact contract tip
`a30db22f9d8e3117a668c463a70d4100149387bd`. Scope is user-approved and an
isolated implementation candidate proceeded under the execution model.

**Implementation candidate:** `codex/phase9/P9BImplementation` at
`00395ef80cfa2364d34ed2170e0735d3a4b1513d`. Independent code review:
**PASS** against base `641eece1e7878267c2c0401a51b87c250e4a7e76`. The review
verified that the selected profile uses the distinct physical-scale identity
`world-scale/Simulation-GeneralTest/v1`, separate from the axial coordinate
convention `axial-hex-v1`. A non-gating provenance NIT remains: the generic
`output-owner` fingerprint record is absent for `SpatialAuthorityStore`, while
the manifest's declared `OutputOwners` and the P9-B `stage-output` fingerprint
both include the spatial authority. No required P9-B provenance is missing;
this is a non-blocking fingerprint-record asymmetry.

**Implementation validation:** on exact code tip `00395ef`,
`SimulationBootstrapCompositionTests` 14/14, `SpatialGeographyTests` 13/13,
ALL EditMode 1712/1712, and the complete official Smoke filter 5/5 passed;
`git diff --check` passed. The selected `Simulation-GeneralTest` profile
publishes exactly one authored P8-A Hex and anchored Location before day one.

**Integration candidate:** `codex/phase9/P9BIntegration` fast-forwarded from
P9 canonical `96f2c1aaf742f313bbb9643e5f5b3d844c402c78` to the reviewed code
tip. Independent integration review **PASS** on exact tip
`d9a62d7c6bea242653c2d68cc0a70911bb5ed1bf`; the review confirmed only the
State-record correction followed the validated code and that the candidate
descends from the P9 canonical base. The user approved promotion, and local and
remote `codex/phase9/canonical` refs were verified at that tip. P9-B is
promoted; P9-A's historical closure and scope remain unchanged. A separate
formal Phase 9 closure record remains pending.
