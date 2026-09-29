# P12-C RuntimeIdAllocator passive census design

**Status:** Bounded technical design for the accepted P12-C causal-root census capability. Independent design review pending.

**Design base:** `codex/phase12/canonical` at
`69f456d5e3c6d6f7e4b85b36e98968ced0549bf3`.

**Authority:** The accepted P12-B–P12-G capability decomposition already
includes exact P12-C allocator evidence. This design adds no checkpoint,
product behavior, profile scope, or implementation authorization. It does not
make P12-B complete or P12-A ready.

## Source findings

`RuntimeIdAllocator` in `Assets/_Project/Scripts/RuntimeIdentity.cs` owns
fourteen independent private `next*Sequence` counters. All start at 1. Every
typed allocation uses the shared `Allocate` helper, which throws before
mutation when that counter is already `long.MaxValue`; a successful call
increments only its own counter.

The selected `UnityBootstrap-Daily-v1` startup creates one allocator in
`TesteSimulacao.InitializeSimulation` and passes that instance to the runtime
systems and both recorders. `SimulationBootstrapComposition` currently does
not publish a witness for it. `WorldObserverDemoBootstrap` and other separate
allocators are outside this profile.

The blocker-resolution inventory records selected-profile day-zero next
values of 11 for NPC, 3 for City, Location and Route, and 1 for the other ten
families. Initialization emits no domain events or decisions, so Event and
Decision also remain at 1 at this boundary.

## Bounded witness contract

Publish fourteen fixed schema-v1 sections from the exact allocator instance
used to construct the selected runtime:

| Counter family | Section ID |
|---|---|
| NPC | `p12c.runtime-id-allocator.npcs` |
| City | `p12c.runtime-id-allocator.cities` |
| Location | `p12c.runtime-id-allocator.locations` |
| Route | `p12c.runtime-id-allocator.routes` |
| Event | `p12c.runtime-id-allocator.events` |
| Directive | `p12c.runtime-id-allocator.directives` |
| Decision | `p12c.runtime-id-allocator.decisions` |
| Travel party | `p12c.runtime-id-allocator.travel-parties` |
| Organization | `p12c.runtime-id-allocator.organizations` |
| Explorable site | `p12c.runtime-id-allocator.explorable-sites` |
| Expedition | `p12c.runtime-id-allocator.expeditions` |
| Local place | `p12c.runtime-id-allocator.local-places` |
| Local connection | `p12c.runtime-id-allocator.local-connections` |
| Notable item | `p12c.runtime-id-allocator.notable-items` |

For every section:

- schema version is 1;
- cardinality is 1, meaning that the typed next-value counter exists;
- revision is exactly that counter's `next*Sequence - 1`;
- all fourteen sections share one stable opaque reference-identity token
  owned by the same `RuntimeIdAllocator` instance.

Do not derive revision from registry membership or successful object
registration. A successfully allocated ID remains consumed if a later
registration/install step fails, and its counter revision reflects that gap.
An exhausted allocation throws before changing the counter, so that
namespace's revision stays unchanged. Each namespace advances independently.

Use the existing `IOwnerSectionCensusProvider` and
`OwnerSectionCensusWitness` contracts. Add only a fixed provider family and a
read-only composition property, wired to the selected bootstrap's existing
allocator. Do not expose a general provider-registration hook or an allocator
mutation handle through `SimulationBootstrapComposition`.

## Proving evidence

The owner tests should cover all fourteen section IDs, schema versions,
cardinality 1, shared stable identity, repeated-read stability, and exact
revision behavior for each matching allocation method. At least one
successful allocation per family must advance only its own revision. Cover
that allocation consumed before later registration failure does not roll
back, and that an exhausted counter preserves its revision.

The selected-profile bootstrap test should read all fourteen providers from
the published composition and verify the day-zero revisions: NPC 10; City,
Location and Route 2 each; and Event, Directive, Decision, Travel party,
Organization, Explorable site, Expedition, Local place, Local connection and
Notable item 0 each. All fourteen cardinalities are 1 and all identities are
the same stable allocator token.

## Limits retained

This is passive live counter evidence, not allocator snapshot/export or
staged hydration. It does not connect allocations to the shared mutation
epoch, prove owner-thread affinity or quiescence, atomically capture all
sections, issue capture eligibility, or change ID allocation behavior. The
provider set is only evidence for the selected profile's allocator; it does
not claim that the complete C/D/E/F owner census is present. P12-B remains
incomplete and P12-A remains `WAIT_DEPENDENCY`.
