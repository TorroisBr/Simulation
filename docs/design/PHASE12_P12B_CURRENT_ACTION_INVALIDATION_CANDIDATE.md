# P12-B CurrentAction invalidation candidate

**Status:** Implementation and required validation complete; fresh exact-tip
independent implementation review pending. This bounded owner/operation/epoch
slice does not promote code or change Phase readiness.

## Candidate identity

- P12 canonical base: `90a8a2aef212bec2c68d7224739ce512d44c9fe3`
- Implementation branch: `codex/phase12/P12BCurrentActionInvalidationImplementation`
- Implementation code commit: `48eef6837e925ed8857e1c13bde285979fc5b179`
- Implementation code tree: `736a960c3156c18728ac50f94f14ddafa2111aac`
- Reviewed technical design: `8cf4ba70ef2793deb3a314b70c4c51bcbfdcd675`
- Exact design review record: `10738e460fa2866339473886f116dd2d1635e08f`

The initial implementation commit is `cbd32dd7186c9d1256f241ce3191f6195c2c200d`;
the follow-up adds only the review-requested lifecycle and live ActorChoice
coverage. No production code changed after the initial code review.

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
or truth-rejected SellGoods ActorChoice. Saturation assertions prove Person,
NPC lifecycle, population, action slot, and epoch state remain unchanged on
rejection. ActorChoice assertions prove accepted disposition and installed
action identity, and clear-before-reject behavior.

## Validation on code tree `736a960c3156c18728ac50f94f14ddafa2111aac`

Unity version: `6000.3.9f1`. Each retained result has zero failures,
inconclusive tests, and skipped tests. XML and log SHA-256 values are listed
below. The seven raw Unity logs are retained in
`docs/validation/P12CurrentAction/review-gap-validation-logs.zip`
(SHA-256 `12AD223F433ACAEB22A24138154FA5AE54A0379D2C10655725CA6B1A40C90ADC`);
archive entries use each log's listed filename.

| Gate | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| P12 lifecycle, current-action and ActorChoice focus | 19/19 | `docs/validation/P12CurrentAction/review-gap-focused-final-2/EditMode-20261006-142930-f74794f843624e88a81584d60b7d890a.xml` | `1D546A16C68AE0B3CC5EB7532606D4A4E374811F50E90DC3F130A9F41364BF79` | `docs/validation/P12CurrentAction/review-gap-focused-final-2/EditMode-20261006-142930-f74794f843624e88a81584d60b7d890a.log` | `0EDDFB8627F8510F364C2C11DB3086F90D2A42D9809846DBE026C4594FD7052C` |
| Daily-v1 runtime admission and owner inventory | 37/37 | `docs/validation/P12CurrentAction/revalidation-admission/EditMode-20261006-143021-0c9a6a63e26d48fca2286a26dcffb665.xml` | `B04E620F44A694D175F43CAED3BAD687965DCB0C89CE3FC7151031A590E5E529` | `docs/validation/P12CurrentAction/revalidation-admission/EditMode-20261006-143021-0c9a6a63e26d48fca2286a26dcffb665.log` | `DD110B0E708A7320EFF3A9AD6BEF0EE1353C5744A77113AEBC9A32AB3606E953` |
| Bootstrap composition and Daily-v1/P10 separation | 22/22 | `docs/validation/P12CurrentAction/revalidation-composition/EditMode-20261006-143040-00ce3933a78e42368439f4ad41f47458.xml` | `AB217C2F49A292CA829675672946C339072B6B17EE05F563D801D932ACFCC68B` | `docs/validation/P12CurrentAction/revalidation-composition/EditMode-20261006-143040-00ce3933a78e42368439f4ad41f47458.log` | `9E545FD3FD3950433C452D3B4FCE523C5BBF1B8877867FC0D555F38883020F0D` |
| Crime/Justice regression | 12/12 | `docs/validation/P12CurrentAction/revalidation-crime/EditMode-20261006-143101-7de844a396d84519a739bd07eb8917b5.xml` | `D4C03D027CB5D07248C31AB3076CE9171BEC14B93A2C103DE9C93F359D42C6DA` | `docs/validation/P12CurrentAction/revalidation-crime/EditMode-20261006-143101-7de844a396d84519a739bd07eb8917b5.log` | `912F3DC6E6E86EAD08665B093192E5068530A5C3B657F229FD844DAB34580D93` |
| Solo travel regression | 11/11 | `docs/validation/P12CurrentAction/revalidation-travel/EditMode-20261006-143118-7451257eb0434e6aa87bad0e190650fc.xml` | `F0CE9C52BB0881EA96DF08EF0EE338B49CDDBB8CBC61AFC3083D0D1F9C0DAB8C` | `docs/validation/P12CurrentAction/revalidation-travel/EditMode-20261006-143118-7451257eb0434e6aa87bad0e190650fc.log` | `C814A1C4611542627F5F0BE9ADAB2867794DDEF383FD648F50A9AFCB9DF2C5CB` |
| ALL EditMode | 2394/2394 | `docs/validation/P12CurrentAction/revalidation-all-editmode/EditMode-20261006-143138-3615f161c48b4b369b17df662ac56226.xml` | `DF7B40F226161554F20B7C49FC89D6108449A308675F1A3A13F0361529ED5947` | `docs/validation/P12CurrentAction/revalidation-all-editmode/EditMode-20261006-143138-3615f161c48b4b369b17df662ac56226.log` | `05760D31287EAB0F4C84A7106F2DD630D547C144D66AA7F8080B6DC4A32A03FB` |
| Official Smoke | 5/5 | `docs/validation/P12CurrentAction/revalidation-official-smoke/EditMode-20261006-143217-4d7bdee3e63e4b93a0b63015c2fee75c.xml` | `C2ED883B76E7973966FB6AE920DF632E151DF1A4BC443C291A3FA8D505EED9DA` | `docs/validation/P12CurrentAction/revalidation-official-smoke/EditMode-20261006-143217-4d7bdee3e63e4b93a0b63015c2fee75c.log` | `5CCD94E83BA4DE487D9ED1D6955598773CD604EEEAEAABAF2B3718D8A4147219` |

`git diff --check` passed after validation. The selected profile is the
P9-B-only `UnityBootstrap-Daily-v1`; P10-A Ruin/LocalTopology remains outside
the profile. These tests do not imply complete P12-B owner/operation/shared-
epoch coverage, quiescence, capture eligibility, export, hydration, P12-A
readiness, P13 readiness, or Phase 12 closure.

P12-B remains `INCOMPLETE`; P12-A remains `WAIT_DEPENDENCY`; P13 remains
blocked.
