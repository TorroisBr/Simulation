# P12-F Daily-v1 owner package implementation candidate

**Status:** Validated implementation candidate awaiting independent exact-tip review and canonical integration.

**Checkpoint:** P12-F — Knowledge, directives, P11 choices, and active commitments for the accepted UnityBootstrap-Daily-v1 profile.

**Canonical base:** codex/phase12/canonical at 0619a33cd4287d89bad80fbe546763aff8f2a75b.

**Implementation base:** b8f008dd6689e6548b53df110a5ae4fc9ba6b288.

**Implementation commit:** 8809be743cbc94155cd85a417e58ec55bd60965d; exact Assets tree: 5dda045428ce7d73e3355cf324b6734a0ad18c34.

## Delivered by this candidate

- Captures detached values and locally stages the five accepted F authorities present in Daily-v1: PoliticalKnowledgeStore, ScheduledDirectiveStore, ActorChoiceStore, TravelPartyStore, and ExpeditionStore.
- Registers the existing Expedition owner as a required Daily-v1 census section and checks exact owner identity, schema, cardinality, and local revision, including an explicit zero-record section.
- Carries detached NPC F rows out of the already validated P12-D paired projection without retaining its source-linked projection or evidence, and verifies TravelParty/Expedition links against the staged D identities.
- Uses the same completed-boundary token, owner-section vector, and temporary DailyCaptureStagingAttempt across C/D/E/F; the F owner values contain no source-domain owner identity.
- Builds only private owner candidates. Directives are reconstructed without dispatch or processing; ActorChoice is limited to terminal history and is not replayed; active travel/expedition commitments are restored without re-planning or re-applying effects.
- Preserves typed unresolved P12-E Knowledge bindings for P12-G.

## Scope limits

This candidate implements owner export and private staging for P12-F only. It does not add save/load, whole-graph validation/publication, continuation parity, capture eligibility, or an active-runtime swap. It does not complete P12-G, make P12-A READY, unblock P13, or close Phase 12. P18 temporal state, P19 module state, P20 shared activities, and populated excluded P8 sections remain outside the accepted Daily-v1 profile.

The candidate preserves the accepted profile, existing domain authority, exact owner-local identity/cardinality/revision, and the reviewed exclusions in PHASE12_F_TECHNICAL_DESIGN.md. No architecture, product, public API, or checkpoint-scope change is proposed.

## Validation

Focused P12-F 23/23, P12-E package regression 6/6, ALL EditMode 2731/2731, official Smoke 5/5, and git diff --check PASS. Exact XML, compressed logs, commands, counts, and hashes are in [P12FDailyV1OwnerPackage/VALIDATION.md](../validation/P12FDailyV1OwnerPackage/VALIDATION.md).

This implementation does not update canonical State because it is still a candidate. P12-B/C/D/E status remains as recorded by the canonical Phase 12 State. P12-G, P12-A, P13, and Phase 12 closure remain gated by their explicit contracts.