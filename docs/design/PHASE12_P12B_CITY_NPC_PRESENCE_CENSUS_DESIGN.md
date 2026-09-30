# P12-B City NPC-presence projection census design

**Status:** Bounded technical contract for an accepted P12-B owner-census work
package. It is not an implementation, new checkpoint ID, P12-B completion, or
P12-A readiness claim.

**Evidence base:** P12 canonical `f7a66963ff6f44ee6116c0b37fd7374bf34ace5a`.
The roster-following SpatialKnowledge family was promoted at `0021b0a` and is
the stable NPC membership boundary. The City projection remains a separate
partial owner witness; it is not yet reconciled by that family.

## Owner and witness

For each `CityRuntime` installed in the runtime's fixed `Cities` composition,
publish one schema-v1 passive section bound to that exact City object. Use a
stable City RuntimeId-based section identity. Report:

- cardinality: the live number of NPCs in the City's ImportantNpcs projection;
- revision: a City-owned monotone revision for actual projection-membership
  changes.

Each provider also holds the runtime's live read-only NPC roster and validates
both directions before issuing a witness. Every projected City member must be
present exactly once in that roster, point back to the exact City object and
its exact Location object, and have a unique RuntimeId. Every rostered NPC
whose `CurrentCity` is that City must appear exactly once and point to the
City's exact Location. Duplicate, omitted, or non-reciprocal membership makes
the provider fail closed instead of issuing a witness.

The revision belongs to the City projection owner. It does not replace
`NpcRuntime.CurrentCity` or claim that the City list is the sole location
truth. The witness must compare the City projection to the corresponding NPC
presence facts as an invariant check.

## Mutation contract

`CityRuntime` becomes the only mutator of its ImportantNpcs collection. Expose
a live read-only view and route every supported addition/removal through
City-owned methods. `NpcRuntime.SetCurrentPresence` currently adds directly to
the public list; move that write behind the City owner path. Preserve existing
alive, travel, location, and projection acceptance rules.

Increment the revision exactly once when a membership actually changes. No-op
or rejected operations leave it unchanged. A move between Cities changes two
owners and increments each changed City once. Avoid duplicate increments when
`AddImportantNpc` delegates presence assignment back to the City projection.
Preflight all affected City revision capacity before a cross-City transition;
revision saturation must fail before the projection or NPC presence is
partially changed.

The implementation must account for construction/bootstrap, materialization,
presence changes, travel departure, arrival, and cancellation. A mutable raw
`List<NpcRuntime>` escape is not compatible with an owner revision because it
allows writes that bypass the stamp.

Single-person travel must check capacity for its actual origin-City membership
removal before charging or changing travel state. Travel-party start must
preflight the actual City owners from which members will be removed and the
resolved origin-City destination used by rollback, reserving capacity for all
possible compensation additions before changing any participant. Members share
an origin Location, but their current City membership need not be assumed to
match that resolved origin City.
Travel-party arrival must preflight the aggregate destination-City additions
before advancing any member on the final day. A saturation rejection leaves
the group's members, costs, and party/event state untouched.

## Required evidence

- The selected authored bootstrap test observes every installed City's exact
  owner identity, membership count, and revision, and verifies each projected
  NPC's reciprocal `CurrentCity` and location relationship. It also verifies
  the reverse direction: every rostered NPC whose `CurrentCity` is that City
  appears exactly once in the City's projection.
- Add/remove tests prove one revision increment per actual owner change and no
  increment on duplicate, missing, rejected, dead, traveling, or no-op paths.
- Same-City and cross-City presence transitions, travel departure/arrival,
  cancellation, construction, and materialization preserve existing behavior
  and leave duplicate-free, reciprocal projections.
- Before any presence or travel operation changes NPC state, it preflights the
  revision capacity of every City membership it will add or remove. This
  includes departure, arrival, cancellation, materialization, and cross-City
  movement. If a required City revision is saturated, the operation fails
  before either the NPC's presence/travel state or any City's membership is
  partially changed. The revision never wraps or silently permits an
  unobservable membership change.
- No public mutable collection path remains; update source callers to use the
  City-owned methods.

## Limits and dependencies

The witness is passive owner evidence. This slice does not connect City writes
to the global P12-B mutation epoch, establish cross-owner transaction
quiescence, inventory every City/NPC field, provide export/hydration, or grant
capture eligibility. Any unsupported or missed revision change must make the
later census assessment fail closed. If City sections are admitted to the
active protocol, every observed City revision change must be included in the
same outer reconciliation or make census assessment fail closed; do not run a
second reconciliation or epoch advance. The protocol still assumes fixed City
composition through `SimulationRuntime.Cities`, whose read-only view currently
wraps a mutable backing list. This contract does not solve composition sealing
or drift detection.
