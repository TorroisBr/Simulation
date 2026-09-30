# P12-B Per-NPC Knowledge Census Design Review

**Result:** PASS — independent exact-tip design review.

**Reviewed proposal:** `codex/phase12/P12BNpcKnowledgeCensusDesign` at
`3856432da1a14b8aec4fd92c1562aaebda107524`.

**Base:** P12 canonical `e9ced8e451f42e80ed2132ce494cd5c26e439894`.

The review confirmed the proposal covers the four selected-profile per-NPC
Knowledge owners: ExplorableSite, LocalTopology, AdventureSiteIntel, and
CommercialKnowledge. The ten new section IDs per NPC are independently
counted and identity-bound. Existing SpatialKnowledge remains a separate
two-section family, for twelve per-NPC Knowledge sections together; Inventory
remains a separate family. The proposal preserves those existing families and
prevents section collisions.

Owner acquisition is fail-closed through non-materializing references. The
Commercial census snapshot reads market, liquidity, and share-receipt backing
lists together with the owner revision without using lazy public list getters.
A null owner or any null backing list fails without creating state or changing
revision; the proposed tests verify this behavior. Owner-local revisions for
the other three owners advance on successful insertion/replacement, preserve
no-op behavior at saturation, and reject changing writes before mutation when
the revision is exhausted. Commercial's existing batch and receipt revision
semantics are preserved.

The mutation map covers the direct record APIs and relevant domain/bootstrap,
command, expedition, sharing, and conditional P18 prepared-install paths. It
keeps list cardinalities separate across LocalTopology and Adventure fanout,
does not claim cross-owner rollback, and requires tests to pin the bounded
sequence and saturated-owner behavior. Typed roster reconciliation stages all
candidate providers before publication, validates exact owner/NPC identity,
and handles additions, removals, and same-ID replacement without partial
publication or implicit owner creation.

P18 local-observation receipts are explicitly excluded as not composed for
`UnityBootstrap-Daily-v1`; they are not represented as empty. P18 Commercial
child installs contribute only to the already-composed Commercial owner when
that execution path is present. This preserves the accepted profile's
no-intraday-state boundary and makes no temporal reconstruction claim. The
proposal introduces no P20 activity or participant assumptions and does not
conflate per-NPC Knowledge with Activity identity/cardinality.

The selected-profile roster count is derived from the live roster. The
ten-NPC authored proving profile and separate SpatialKnowledge baseline are
used as evidence, while provider cardinalities for Commercial Knowledge are
read from live owners rather than hard-coded. The proposal retains P12-B's
remaining owner census, committed-write invalidation, owner-thread/quiescence,
and exact-zero gaps, as well as export/staged-hydration and P12-A gates. It
claims no capture eligibility, shared epoch, P12-A readiness, P12-B closure,
or implementation delivery.

The full docs-only diff was reviewed against the current P12 State, owner
inventory/blocker map, accepted profile and both alignment records. The
candidate's `git diff --check` passed. No Unity tests were run because this is
a design-only candidate; its document makes no implementation or test claim.
