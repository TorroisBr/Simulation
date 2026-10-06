# P12-B CurrentAction invalidation — implementation review

**Verdict: `VALIDATED_CANDIDATE`**

## Exact candidate reviewed

- Canonical branch/base: `codex/phase12/canonical` at
  `90a8a2aef212bec2c68d7224739ce512d44c9fe3`
- Current canonical at review: `90a8a2aef212bec2c68d7224739ce512d44c9fe3`
- Candidate branch: `codex/phase12/P12BCurrentActionInvalidationImplementation`
- Code commit: `f4a235c17d65841585ae9f6c56f65ca8eb310ca6`
- Code tree: `92a59220bdefc8e17e7fea6490f87f15646b9d6f`
- Candidate/evidence tip: `168517d4482068db38463a0e3bb1888fda2d4349`
- Architecture baseline: `e16796014d348e3b59da7ed848101c4c03926ba5`
- Reviewed design: `8cf4ba70ef2793deb3a314b70c4c51bcbfdcd675`
- Design review: `10738e460fa2866339473886f116dd2d1635e08f`

The candidate/evidence tip is a descendant of the exact code commit. The
implementation diff was reviewed against the named canonical base. The review
confirmed that the latest P12 canonical and architecture references match the
candidate assumptions and found no later code or tree change after validation.

## Review findings

The implementation provides the bounded per-rostered-NPC current-action owner
section, exact owner identity and cardinality 0 or 1, a non-serialized local
revision, admitted installed-action writes, and action clearing inside the
existing covered Person/population lifecycle operations. Mutable action runtime
ownership is exclusive to one NPC slot. Effective writes notify the owner and
shared census epoch; rejected/no-op and stale detached writes do not publish a
change.

The previous review gaps are closed by tests that:

- unregister an NPC with an installed action, verify its section is withdrawn,
  and register a different NPC with the same RuntimeId without rebinding the
  prior NPC or mutable action;
- replace the mutable `NpcActionRuntime` while preserving the same exact
  `NpcActionData` object; and
- attempt all five installed-action field mutators outside the admitted
  boundary and verify values, local revision, and shared epoch remain unchanged.

Earlier follow-up coverage remains present for direct Person death, legacy and
Person-backed resident death with empty/present action slots and saturated
preflight, plus accepted and current-truth-rejected Local SellGoods ActorChoice
flows. The candidate does not introduce a new action family or gameplay rule.

No production code changed after the initial implementation commit
`cbd32dd7186c9d1256f241ce3191f6195c2c200d`; later implementation commits are
test-only. I found no remaining actionable code, temporal identity/cardinality,
scope, or revalidation issue.

## Validation evidence

All results below are retained in the P12 candidate evidence commit
`168517d4482068db38463a0e3bb1888fda2d4349` and correspond to reviewed code
tree `92a59220bdefc8e17e7fea6490f87f15646b9d6f`.

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| Focused P12 lifecycle/current-action/ActorChoice | 20/20 | `67BC9C882F0B37BF16D04B6D783947735C76D668BDE8129867A10C8BC6E4E703` | `D9C6B092ED2C4B1FB84290ABE550FC764FBA9E5BA550F37A2E418C6D7722910B` |
| Daily-v1 admission and owner inventory | 37/37 | `9AF21E3DDFA5A6B34B00836F35B4FCF81696CFC1AF49280478E71D53EE41B1C2` | `177C93859B51BC8206E60CAF82C703AE73508AA97F37D79C585FFEA222FC0006` |
| Bootstrap composition and Daily-v1/P10 separation | 22/22 | `9BA7B10D90AC26CC444E84589ECE81674C804DADD4399F484824D6A5D89052BB` | `312A542895A2AD096501D3D95A59C685AB0B0BE848855A4D7CB01D80456A05CD` |
| Crime/Justice regression | 12/12 | `B511A3A7ED6BFDDFFECA9C03C0078E4A2439F8FE83C1D1466CF222351A4DC041` | `0D7EED9A812FED0B57FD0F26C68D11AD28F576C1BA8DCF7468F71498F43DC7F9` |
| Solo travel regression | 11/11 | `78912067C81BE78ACFC7DBFAAD3A228F8E634525EFFF81D0291F9B3CDA6223DE` | `8A30AC2B6F3EC9A8E662BD45CE9BC1940C0DDC373A682F433483620F4B8DFB96` |
| ALL EditMode | 2395/2395 | `4B8008AF9240EF13F3CF3F07E4FA39C1CC61EA4CECD74B8B929FFA6156E4F46F` | `675D8CA304CBA59808E7C4A75247C81223C2D3B627080DDDB0D152A99F8B5323` |
| Official Smoke | 5/5 | `219E8283A725B2243D843FABC2F9FE016777A212A82B517E951163E6DDE72FB7` | `159C076E42EC6EC2A905244D06C67C1C1669977810526BC357EA19D3229FF757` |

The seven raw logs are archived in
`docs/validation/P12CurrentAction/review-gap-coverage-fix-validation-logs.zip`
(SHA-256
`499B46A5DEB9B96F482A8361CE3B2B4D275082D8F9AC1A46EB3842122204CBBA`).
`git diff --check` passed on the exact base-to-code diff. Result paths and
artifact hashes are also listed in
[`PHASE12_P12B_CURRENT_ACTION_INVALIDATION_CANDIDATE.md`](PHASE12_P12B_CURRENT_ACTION_INVALIDATION_CANDIDATE.md).

## Scope and integration constraints

This review validates only the selected `UnityBootstrap-Daily-v1` current-action
owner and the specifically covered mutation paths. It does not establish
complete P12-B owner or shared-epoch coverage, global quiescence, capture
eligibility, export, hydration, P12-A readiness, P13 readiness, or Phase 12
closure. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked. P10-A Ruin/LocalTopology remains a separate proving profile and is
excluded from Daily-v1.

No product or canonical architecture decision remains unresolved for this
bounded slice. Subject to the normal refreshed canonical promotion preflight,
this candidate is eligible for autonomous bounded promotion.
