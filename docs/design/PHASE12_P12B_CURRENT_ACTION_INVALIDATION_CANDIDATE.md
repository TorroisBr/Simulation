# P12-B CurrentAction invalidation candidate

**Status:** `VALIDATED_CANDIDATE`. Exact-tip independent implementation review
passed and is recorded in
[`PHASE12_P12B_CURRENT_ACTION_INVALIDATION_REVIEW.md`](PHASE12_P12B_CURRENT_ACTION_INVALIDATION_REVIEW.md).
This bounded owner/operation/epoch slice does not itself promote code or change
Phase readiness.

## Candidate identity

- P12 canonical base: `90a8a2aef212bec2c68d7224739ce512d44c9fe3`
- Implementation branch: `codex/phase12/P12BCurrentActionInvalidationImplementation`
- Implementation code commit: `f4a235c17d65841585ae9f6c56f65ca8eb310ca6`
- Implementation code tree: `92a59220bdefc8e17e7fea6490f87f15646b9d6f`
- Reviewed technical design: `8cf4ba70ef2793deb3a314b70c4c51bcbfdcd675`
- Exact design review record: `10738e460fa2866339473886f116dd2d1635e08f`
- Exact-tip implementation review: `PHASE12_P12B_CURRENT_ACTION_INVALIDATION_REVIEW.md`

The initial implementation commit is `cbd32dd7186c9d1256f241ce3191f6195c2c200d`;
follow-up commits add test coverage only. No production code changed after the
initial implementation.

## Bounded behavior

- A rostered `NpcRuntime` owns `p12b.npc-current-action/<RuntimeId>`, cardinality
  0 or 1, and the non-serialized local `CurrentActionRevision`.
- The paired action definition/runtime references must be consistent. A mutable
  action runtime belongs to at most one NPC slot; installed action writes use
  the selected Daily-v1 owner-thread operation boundary and shared census epoch.
- Effective installed-action field changes advance the owner revision once;
  no-op writes and detached stale references do not. Death clears a present
  action atomically with the existing Person/population/NPC lifecycle owners.
- No new gameplay behavior, operation family, P12 export/hydration, or whole-NPC
  composite revision is introduced.

The follow-up tests cover direct Person death with empty/present actions and
saturated preflight; legacy resident death with empty/present actions and
saturated preflight; Person-backed resident death with empty/present actions
and saturated preflight; and live Person bind/materialize followed by accepted
or truth-rejected SellGoods ActorChoice. Additional tests cover roster removal
and same-RuntimeId replacement without rebinding the old owner/action; replace
the mutable action runtime while retaining the exact shared `NpcActionData`
instance; and prove that all five installed-action mutators reject writes
outside the admitted boundary without changing owner revision or epoch.
Saturation assertions prove Person,
NPC lifecycle, population, action slot, and epoch state remain unchanged on
rejection. ActorChoice assertions prove accepted disposition and installed
action identity, and clear-before-reject behavior.

## Validation on code tree `92a59220bdefc8e17e7fea6490f87f15646b9d6f`

Unity version: `6000.3.9f1`. Each retained result has zero failures,
inconclusive tests, and skipped tests. XML and log SHA-256 values are listed
below. The seven raw Unity logs are retained in
`docs/validation/P12CurrentAction/review-gap-coverage-fix-validation-logs.zip`
(SHA-256 `499B46A5DEB9B96F482A8361CE3B2B4D275082D8F9AC1A46EB3842122204CBBA`);
archive entries use each log's listed filename.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| P12 lifecycle, current-action and ActorChoice focus | 20/20 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-focused/EditMode-20261006-144208-aaf295f0b5ce442a989a4f8f90234e17.xml` | `67BC9C882F0B37BF16D04B6D783947735C76D668BDE8129867A10C8BC6E4E703` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-focused/EditMode-20261006-144208-aaf295f0b5ce442a989a4f8f90234e17.log` | `D9C6B092ED2C4B1FB84290ABE550FC764FBA9E5BA550F37A2E418C6D7722910B` |
| Daily-v1 runtime admission and owner inventory | 37/37 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-admission/EditMode-20261006-144306-66c90f47c477491588437b5699f278e8.xml` | `9AF21E3DDFA5A6B34B00836F35B4FCF81696CFC1AF49280478E71D53EE41B1C2` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-admission/EditMode-20261006-144306-66c90f47c477491588437b5699f278e8.log` | `177C93859B51BC8206E60CAF82C703AE73508AA97F37D79C585FFEA222FC0006` |
| Bootstrap composition and Daily-v1/P10 separation | 22/22 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-composition/EditMode-20261006-144315-5f3dcd22c22f438a909a423d8c791717.xml` | `9BA7B10D90AC26CC444E84589ECE81674C804DADD4399F484824D6A5D89052BB` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-composition/EditMode-20261006-144315-5f3dcd22c22f438a909a423d8c791717.log` | `312A542895A2AD096501D3D95A59C685AB0B0BE848855A4D7CB01D80456A05CD` |
| Crime/Justice regression | 12/12 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-crime/EditMode-20261006-144325-b343d2e0753c41c5b33984d6274f6249.xml` | `B511A3A7ED6BFDDFFECA9C03C0078E4A2439F8FE83C1D1466CF222351A4DC041` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-crime/EditMode-20261006-144325-b343d2e0753c41c5b33984d6274f6249.log` | `0D7EED9A812FED0B57FD0F26C68D11AD28F576C1BA8DCF7468F71498F43DC7F9` |
| Solo travel regression | 11/11 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-travel/EditMode-20261006-144334-8584f18665624111965862be0edea0fc.xml` | `78912067C81BE78ACFC7DBFAAD3A228F8E634525EFFF81D0291F9B3CDA6223DE` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-travel/EditMode-20261006-144334-8584f18665624111965862be0edea0fc.log` | `8A30AC2B6F3EC9A8E662BD45CE9BC1940C0DDC373A682F433483620F4B8DFB96` |
| ALL EditMode | 2395/2395 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-all-editmode/EditMode-20261006-144343-58181107a62945d39d3a6ea999b22bd5.xml` | `4B8008AF9240EF13F3CF3F07E4FA39C1CC61EA4CECD74B8B929FFA6156E4F46F` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-all-editmode/EditMode-20261006-144343-58181107a62945d39d3a6ea999b22bd5.log` | `675D8CA304CBA59808E7C4A75247C81223C2D3B627080DDDB0D152A99F8B5323` |
| Official Smoke | 5/5 | `docs/validation/P12CurrentAction/review-gap-coverage-fix-official-smoke/EditMode-20261006-144412-185d0778786b48ada804004c645b33f4.xml` | `219E8283A725B2243D843FABC2F9FE016777A212A82B517E951163E6DDE72FB7` | `docs/validation/P12CurrentAction/review-gap-coverage-fix-official-smoke/EditMode-20261006-144412-185d0778786b48ada804004c645b33f4.log` | `159C076E42EC6EC2A905244D06C67C1C1669977810526BC357EA19D3229FF757` |

`git diff --check` passed after validation. The selected profile is the
P9-B-only `UnityBootstrap-Daily-v1`; P10-A Ruin/LocalTopology remains outside
the profile. These tests do not imply complete P12-B owner/operation/shared-
epoch coverage, quiescence, capture eligibility, export, hydration, P12-A
readiness, P13 readiness, or Phase 12 closure.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked.
