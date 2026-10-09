# P12-E FactionStore Owner Snapshot Design Review

**Result:** `VALIDATED_CANDIDATE` — independent exact-tip design review PASS.

**Reviewed candidate:** branch `codex/phase12/P12EFactionOwnerSnapshotDesignA768F2D`, commit `927ecdb16ff3e420fa63ef8621472d1178780270`, Git tree `130da8fc418f790cd6d602521e41aa34c1ef57fb`. The candidate adds only `docs/design/PHASE12_P12E_FACTION_OWNER_SNAPSHOT_DESIGN_A768F2D.md` (blob `7ade046406a2ac6c2c504f477810043c22589a4e`).

**Actual base / current canonical at review:** `codex/phase12/canonical` `a768f2d9eca161f5cff059a782737412f43b2861`, tree `7ba0866058bc5618473237b4af6322d964352961`. Candidate is a direct child of this base and a clean fast-forward. Refreshed origin confirmed these exact tips immediately before review-record creation.

**Reviewed contract inputs:** architecture `docs/SIMULATION_ARCHITECTURE.md` blob `4a3c73c4428ba7bc43c28f617e243e4cd54078fa`; generic P12-E design `docs/design/PHASE12_E_TECHNICAL_DESIGN.md` blob `15aaee09d3295cff81a48e166b620c89f5156346`; accepted P12 Brief `31d9e1b4df41fc994f0a747274e35c4ef40b6e3a`; State `c5f259e033f3c05c9df72d9fde42e4016a257e0d`; owner inventory `0757cd0c39e7c1e53ae0c0ae99fe191151ef5dc3`. The accepted checkpoint decomposition and current P12 State keep E bounded to exact owner export/private staging and keep P12-A, P12-G, and Phase closure separate.

## Review

The proposed contract matches the current `FactionStore` authority and the generic P12-E design:

- It preserves the two schema-v1 census identities exactly (`p12e.faction.records` and `p12e.faction.affiliations`), their required-empty semantics, exact `FactionStore` identity, separate row cardinalities, and shared local revision. The census provider source at the base confirms these exact constants and owner/revision witnesses.
- It captures all immutable `FactionRecord` fields and all affiliation tenure fields, including the exact supplied `AffiliationId.Value`, nullable end day/reason, and the derived active status. It retains all terminal tenures, so LeaveAndRejoin sequence reconstruction uses the same historical-row count. It does not infer policy or end reason.
- It names the supported live facade writes and the existing owner-local revision boundary. It distinguishes constructor/source-store registration from the private runtime owner, whose read API returns copied lists and whose constructor clones the supplied store.
- It stages against the P12-D `PersonStore`, validates faction/person references, dates, enums, identities, row counts and revisions, rebuilds only the owner-derived active index, restores the exact revision without replaying writes, and rejects the whole private candidate on failure. It does not bind or publish the staged owner.
- It stays within the accepted Faction affiliation semantics in architecture §§26–27 and the generic P12-E owner boundary. Political support/knowledge, P12-G publication, capture eligibility, and other excluded gameplay/phase scopes remain outside this slice.

### U+001F active-pair key

The source at the reviewed base forms active-pair keys as `FactionId.Value + U+001F + PersonId.Value`; both typed ID constructors accept U+001F. The same key is used for active lookups, mutation admission, clone/index behavior, and diagnostic duplicate-active-pair validation. Therefore the design's instruction to reject two distinct typed pairs that alias this key preserves the current owner's representable state/index behavior; it does not claim that either ID character is invalid or redefine affiliation policy. The delimiter collision is an existing owner limitation, not a new persistence semantic. The implementation must add an explicit delimiter-alias fixture: a single affiliation with U+001F inside either typed ID round-trips with exact IDs, while two distinct active typed pairs whose concatenated keys collide are rejected atomically rather than merged, overwritten, or misindexed. No key-encoding or broader Faction behavior change is authorized by this owner-snapshot design.

## Findings and evidence boundary

No blocking design findings. The alias fixture above is a specific implementation-validation obligation implied by the contract. No Unity or implementation tests were run because this review is documentation-only. The candidate diff passes `git diff --check`; its diff contains no executable files. This review does not approve implementation beyond the accepted P12-E capability work, promote any checkpoint, establish P12-A readiness, or claim complete E owner coverage or P12-G publication.


