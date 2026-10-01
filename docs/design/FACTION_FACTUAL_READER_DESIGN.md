# Faction Factual Reader — bounded technical design (FR-C)

**Canonical planning status:** WAIT_DEPENDENCY on promoted FR-B capability; independent design review at `96062630aed6e1fbb616a48d97290c331aed50c6` found no separate semantic blocker. Design promotion is not FR-B capability promotion.

**Architecture base:** `f27954af6d880df123736027fd55e86874d6de68`.
**Owner:** Simulation factual projection of the existing `FactionStore`; no new faction gameplay.

## Why Faction and what it returns

`FactionStore` is the canonical factual owner of `FactionRecord` and `FactionAffiliationRecord`, with stable `FactionId`, active-state semantics, sorted enumeration, a revision, and registration checks against `PersonStore`. This is a smaller real authority than City/Person/Location projection, whose complete public cohorts and joins remain unresolved. `PoliticalKnowledgeStore`, political support, actor beliefs, and display curation are excluded.

Register capability `simulation.faction-truth/v1` with two immutable copied result collections: `FactionFact` (`FactionId`, optional nonblank source `DisplayName`, `CreatedAbsoluteDay`, `MembershipPolicy`, `ExpulsionAllowed`) and `ActiveFactionAffiliationFact` (`FactionAffiliationId`, `FactionId`, `PersonId`, `JoinedAbsoluteDay`). Return current active affiliation facts only; ended records remain historical owner records and are not current membership. Preserve the source ID values as typed Simulation IDs inside the reader. No generic `Organization`, ideology, faction location, member count, inferred name, or external `memberIds` appears in this domain API. `DisplayName` is absent when blank/whitespace; no ID-based label is written into truth.

`PersonId` in a returned affiliation means the source `PersonStore` currently verifies the endpoint, **not** that this reader has approved a Person projection cohort or may emit a public Person reference. If an active endpoint cannot be verified in the same read, the entire Faction capability is `Unavailable` with a diagnostic; do not drop that affiliation or claim an empty membership. A duplicate active faction/person pair, absent Faction endpoint, invalid ID, or impossible affiliation state also fails the capability. Individual `FactionId` lookup returns `Absent` only when the store is authoritatively read and no record exists; collection `Present([])` proves the registered FactionStore is empty.

## Read and ordering boundary

FR-C executes only inside FR-B `TryCaptureCoherent`. It copies `FactionStore.Factions` and `FactionStore.Affiliations`, filters `IsActive`, verifies each Person endpoint against `PersonStore`, and then releases all Store references before returning. Its evidence covers `FactionStore.Revision`, `PersonStore.Revision`, and the logical boundary before/after. Factions sort by `FactionId.Value` ordinal; affiliations sort by `FactionId.Value`, `PersonId.Value`, `JoinedAbsoluteDay`, then `AffiliationId.Value`, all ordinal/numeric. No dictionary iteration, locale collation, or unstable presentation sorting. A changed revision/boundary rejects the entire capture. Current state is never presented as all historical factions or all-time membership.

The adapter to World Exchange may project a faction name and membership only under that contract's reference and coverage rules. This reader does not decide those rules. In particular, an exported `memberIds` list cannot reference absent `people` rows; FR-C alone cannot make Person projection complete.

## Dependencies, failure, validation

FR-B is a **HARD_DEPENDENCY** for implementation. WI-A, P12-A/B/C, P13, P19 and Simulation-External are **NO DEPENDENCY** for this reader's domain meaning; P12/P13 have **ARCHITECTURAL_ALIGNMENT** if later persisted/reconstructed faction state is projected. P18 contributes the completed-boundary contract via FR-B. P12-B remains a shared `SimulationRuntime` hotspot, so implementation uses an isolated worktree and serial integration.

Expected implementation files: `Assets/_Project/Scripts/FactualRead/FactionFactualReader.cs` plus immutable fact contracts and `.meta`; a narrow registration in the FR-B runtime facade; focused EditMode factual-reader tests. `FactionStore` and `PersonStore` should remain unchanged unless a separately reviewed defect blocks the reader. Do not expose Store records by reference or consume P12 snapshots.

Tests: source ID/name/policy/day fidelity; blank name absent; empty Store known-empty; active affiliation included and ended excluded; Person endpoint verified but not exported as an external reference; invalid/missing endpoint fails without partial result; deterministic order across insertion orders and locale; changed Faction or Person revision and changed logical boundary reject the whole coherent capture; mutations to returned collections cannot change truth; Knowledge/support mutations do not become Faction facts. No invented presentation label is allowed.

**Completion:** one complete, current, deterministic factual Faction capability that remains useful without World Exchange and makes no claim about unsupported Person/City/Organization coverage.
