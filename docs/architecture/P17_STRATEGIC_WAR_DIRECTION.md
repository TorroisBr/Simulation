# Phase 17 — Strategic War direction and P17-A entry

**Status:** reviewed architecture/planning candidate; not canonical until
promotion. **Base:** `cddedfa41e72d9205d92411b5bedebf11ac0b16a` on
`codex/architecture/world-identity-projection`. **Classification proposed:**
`READY_FOR_BOUNDED_CHECKPOINT` for P17-A scope, followed by bounded technical
design and independent design review before implementation. This record changes
no executable code and creates no Phase 17 implementation authority.

## Product model

War is a persistent strategic context. Distinct participants may pursue
several actual goals through military, economic, political and territorial
facts. Goal satisfaction, interpretation of pressure, participant withdrawal,
ceasefire, concession, peace and War termination are distinct. The domain does
not own a universal `War.Winner` or a causal `WarExhaustion` score. Human-facing
summaries may derive victory/defeat or pressure from cited facts; the summaries
cannot decide the War by themselves.

The three accepted goal families compose within that one model:

| Family | Example factual satisfaction | Authority kept outside War |
|---|---|---|
| Territorial | A participant obtains recognized military control of a specified Location; War may continue. | Military control/occupation authority, separate from ownership, jurisdiction, sovereignty and allegiance. Neither Battle victory nor presence supplies this fact alone. |
| Attrition/capability | A participant observes losses, depleted supply/manpower or other supported capability facts and decides whether to persist or concede. | Battle/manpower, P14/P16 material/logistics and relevant political/economic owners. No universal exhaustion meter. |
| Non-territorial/coercive | A designated opposing force makes one validated withdrawal from a specified Hex. | P16 movement/position and passage truth; the War owns only participant goals and its own lifecycle. |

Goals belong to identified participants, not to War as one global quest or to a
side merely because one participant happens to occupy it in a fixture. A
participant may have multiple goals; several participants on one side may have
different goals. Goal conditions may be achieved, failed, abandoned or revised
under future explicit decisions. An actual goal is distinct from a declared
goal and from another actor's belief about it. The first slice stores only one
actual goal; it neither publishes that goal to every Actor nor implements
strategic Knowledge propagation.

The causal direction remains World Truth → permitted Knowledge → strategic
decision → command/action → execution revalidation → authoritative
consequence. P17-A can use controlled scenario/GM inputs to select and end its
first case; those inputs are captured as external causality. It does not claim
autonomous Faction decision AI, omniscient Faction Knowledge, or a public
command permission model for future player/mod clients. Later actor decisions
need the appropriate perspective and authorization contract.

## Canonical ownership audit

The implementation canon inspected read-only is
`codex/phase16/canonical` at
`75a27d7ac97e66c2762835ccea7950a945c2f20d`. Phase 7 is closed;
`PersistentWarStore` already owns stable `WarId`, sides, operational
`WarParticipantBinding` records to `ArmedForceId`, active/ended lifecycle and
optional `ConflictId`. `PersistentBattleStore` owns Battle identity and its
terminal outcome; `ConflictFoundation` is an abstract lower-level resolver,
not a persistent War outcome owner. `BattleOutcomeApplicationService` does not
end War or transfer military control. The runtime composes/clones the War Store
and binds the mutation guard; a passive P12 War census witness exists.

P16-A is promoted with one selected ArmedForce crossing one validated P8
passage at one bound daily boundary, while atomically debiting finite carried
supply. Its receipt records force, source/destination Hex, operation identity,
boundary/order and relevant content/revision evidence. It is a bounded
operational fact, not War movement authority. P14-B finite-source production
exists separately; P17-A needs no new production, replenishment or market
operation. Faction has stable `FactionId`, but no currently delivered general
force-to-Faction command relation. Current code has no authoritative military
control, occupation, strategic War goal or War termination-reason owner. The
political claim/support stores do not substitute for those missing facts.

The first design must evolve the **existing** `PersistentWarStore`/War record
as the sole War authority. It must not create a second strategic War Store or
reinterpret P7's force binding as a general political participant identity.
One bounded strategic participation relation links each of two existing
`FactionId`s to a distinct existing `WarSideId`; the existing force bindings
identify one operational force on each side. In this selected profile the
target force's side has exactly one Faction participant, so target attribution
is unambiguous. That one-to-one profile is not a permanent side, participant
or force cardinality rule, and it does not grant the Faction command over the
force outside the reviewed scenario. Later multi-participant/coalition work
needs an explicit attribution contract rather than inferring command from a
shared side.

## P17-A — one coercive withdrawal goal and explicit War end

**Selected proving case:** two existing Factions, two P7 War sides, one
existing ArmedForce bound to each side, one existing P8 passage from Hex H to
adjacent Hex J, and P16-A's selected movable force bound to the opposing side.
The War and A's actual goal are established as complete initial World Truth
before the P16 crossing boundary. A's one goal is: the named opposing force
makes one validated withdrawal from H via that selected passage. The goal is
not “A controls H,” “B has surrendered” or “War is over.”

1. At the initial boundary, one active `WarId` owns distinct participant
   identities for Faction A and Faction B, their side links, and A's stable
   actual goal with the target force binding and source Hex H. P7's force
   bindings and P16's initial position/supply remain in their existing owners.
2. At the chosen later daily boundary, P16-A revalidates and commits its only
   crossing for B's selected force from H to J, consuming its carried supply.
   Failed/stale passage or insufficient supply leaves the War goal unfulfilled
   and the P16 state unchanged.
3. A P17 goal observation at a completed boundary reads the authoritative P16
   receipt and the stable War/participant/binding identities. A matching
   crossing after the goal existed makes this *discrete* goal satisfied. Its
   satisfaction can be derived from retained evidence; P17 does not copy force
   position/supply or create a second causal achievement meter. No automatic
   War lifecycle mutation follows.
4. An explicit GM `Declare` War-end input may later assert B's concession for
   this two-participant profile. This is an external assertion, not a simulated
   autonomous Faction decision. The War authority revalidates the active War,
   named participant and accepted logical boundary/order, then records one
   terminal transition with concession reason/provenance. The first rule ends
   this bounded two-participant War on that accepted concession. Goal
   satisfaction is neither necessary nor sufficient for this termination.
   Repeated or stale requests cannot create another terminal outcome.

An optional linked P7 Battle may resolve while this War stays active. Its
outcome and casualties remain Battle/manpower truth and do not by themselves
move B's force, satisfy the selected withdrawal criterion, change military
control, concede, or end War. P17-A does not require a new Battle to be fought
or implement Battle-to-War pressure conversion. This keeps the first proof
small while preserving the observed distinction.

**Why this first:** it uses promoted P7 War identity, sides and force bindings,
P8 factual Hex/passages and P16-A movement/supply receipt. It avoids the
currently missing military-control/occupation owner and avoids a universal
attrition formula or political-government/treaty rules. One operation has a
clear before/after observation and can later be tried in a small Lab scenario.
P16-A's one-force/one-hop profile limits the demonstration only; P17 neither
owns movement nor makes that profile a War-wide rule.

## Entry contract and exclusions

The bounded technical design must close the exact representation of stable
strategic participation and actual goal IDs inside the existing War authority;
composition/clone and deterministic ordering; War registration and goal
validation against Faction, War side, opposing force binding, registered Hex
and P16 selected profile; the derived goal-observation result and evidence
availability; an explicit concession input/authority/terminal reason; stale
rejection and idempotence; immutable capability-oriented War/goal reads; and
the integration sequence with current P12 and military hotspots. These are
technical choices within the selected semantics, not invitations to choose a
different goal, participant model or termination meaning during implementation.
P7's general `TryEnd` cannot remain an unreasoned bypass for a War composed
under the P17-A profile: the technical design must reject that path for such a
War or route it through the same reasoned terminal transition. Existing P7
standalone behavior outside P17-A retains its bounded contract.

Minimum new authoritative state is the War-owned strategic participation
relation, A's stable actual goal declaration/activation boundary, and the
terminal concession reason/actor/causal metadata when the War ends. Existing
War lifecycle, P7 force bindings, P16 position/supply/receipt and P8 passage
truth remain in their owners. Goal satisfaction is a derived assessment from
the retained P16 receipt for this profile. No duplicate control or position
Store, no universal pressure scalar and no new Battle resolution authority are
introduced. A future goal type that cannot derive durable satisfaction from
recoverable evidence needs its own reviewed state/evidence contract.

**Excluded:** tactical AI, autonomous strategic AI, military-control or
occupation mutations, annexation, ownership/jurisdiction changes, treaties,
ceasefire/peace diplomacy, government/Polity framework, Campaign, paid forces,
new P14 economy, logistics beyond P16-A, multi-leg movement, generalized
war-goal engine, public/rumored goals, multi-Faction coalitions, participant
withdrawal/re-entry, War-wide winner, P20 Activity execution and full P12/P13
implementation. No P17 code is approved by this direction candidate.

## Continuation, validation and demonstrability

The War owner evolution passes architecture §92A: stable IDs, coherent committed
transitions, guarded mutation, exact semantic export and private staged
hydration/validation seams, deterministic accepted order, immutable readings,
and clone preserving state. Initial participant/goal/force/passage/content
inputs are part of the initial-world composition. Later external commands
retain payload, authority, logical boundary and order. Goal evaluation must
not depend on a transient diagnostic or selective History. Fork before the
crossing sees an unfulfilled goal; fork after it sees the P16 receipt and
derived satisfaction; fork after the terminal input sees an ended War with its
reason. No retroactive simulated War history is invented.

P16-A's receipt establishes that a crossing committed; it does not by itself
prove the origin/authority of a future Lab or GM request that selected the
crossing. If the first scenario supplies that operation externally in a
durable world, the application boundary must retain the complete causal input
before accepting it. A predetermined internal scenario operation must instead
be reproducible from its effective initial configuration and deterministic
ordering. The technical design must identify which path its first profile
uses and must not claim historical reconstruction from the receipt alone.

P17-A's proving composition is explicitly separate from
`UnityBootstrap-Daily-v1`, which does not admit populated P16-A state. The
existing P12 War census is not evidence that new P17 fields are captured,
hydrated or allowed in that selected save profile. Before P17-A domain
promotion, prove the selected profile excludes this new state or rejects it
fail-closed with a negative admission test; serialize any shared P12 admission
hook with active P12-B work. P17-A does not wait for full P12 closure, and it
cannot claim save/load or P13 fork until those exact profile capabilities exist.

Validation for a future implementation includes distinct strategic
participant/side/force identities; two-side bounded admission; matching versus
stale/wrong-force/wrong-source P16 receipts; no War change from rejected
movement; goal satisfaction without War end; explicit concession with reason,
ordering, single terminal outcome and fail-closed repetition; Battle outcome
without automatic War/control/goal mutation; no mutation of P16, spatial,
Faction, political or Battle owners by War logic; clone/invariant/guard checks;
P7 raw-end bypass rejection for a P17-A War; P12 negative admission; complete
external-causal-input capture or deterministic effective-scenario provenance;
deterministic before/after reconstruction-sensitive state; and affected
regressions. Runtime/War Store, P16 spatial state, P8
passages, command capture and P12 admission are serial integration hotspots.

**Demonstrability: `FOLLOW-UP_DEMONSTRATION`.** A person should be able to
inspect the active War, two participants and A's goal; execute the real P16
crossing; observe force position/supply and the goal becoming satisfied while
the War stays active; then submit the explicit concession and inspect the
terminal reason. A concise table/CLI view suffices. The minimum scenario is
the two-Faction/two-force/one-passage profile above. Current Lab non-Unity
composition and War/P16 factual reads are not delivered, so the domain
checkpoint may promote independently; prefer the human scenario before formal
Phase 17 closure once the Lab path exists. Its host must invoke the real
Simulation authorities and preserve P7's numeric-profile limitation if an
optional Battle is demonstrated. It must not invent a parallel War engine or
use diagnostic snapshots as authoritative external reads. A later handoff
includes `HOW TO TRY IT` when the demonstration exists.

## Follow-on sequence and open gates

- Military-control/occupation owner and one territorial goal, preserving
  jurisdiction/ownership/sovereignty separation, need their own contract.
- Attrition/capability goals can consume P7 casualties/manpower, P14 material,
  P16 supply and later facts through bounded participant Knowledge and
  decisions; no universal causal exhaustion meter.
- Additional coercive goals, public/believed goals, participant changes,
  coalitions and strategic decisions by simulated actors each need their own
  selected consumer and authority boundary.
- Ceasefire, participant withdrawal, surrender/capitulation, negotiated peace
  and termination stay separate. The P17-A concession rule is confined to its
  two-participant proving profile.
- P13 save/fork integration awaits exact chosen-profile continuation and
  recoverable War/P16 causal state. P19 and P20 are not prerequisites for
  P17-A.

No further human product selection is needed to choose the P17-A first
scenario. A bounded technical design and independent design review remain
before implementation. Architecture-canonical promotion remains a separate
human gate.
