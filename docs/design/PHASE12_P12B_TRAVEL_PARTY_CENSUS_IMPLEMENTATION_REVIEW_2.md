# P12-B TravelParty census implementation review

**Result: PASS — independent exact-tip implementation review.**

**Candidate branch:** `codex/phase12/P12BTravelPartyCensusImplementation`
at final evidence tip `b78a271f9c552b40bade1a45168388eafa670f59`.

**Reviewed code/test tip:** `260a688f7ea1a9f86e9b589788cda2c32357e170`,
tree `34571165d55af36a60f64650dce1968802cfd500`.

**Base:** `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`. The current local and remote
canonical and candidate refs were verified before review. Production code is
unchanged from prior reviewed code tip `8c446ed8920028566b8af84583e7599bc907791c`;
this test tip replaces the prior scheduler-timed return race with a deterministic
stepwise test and adds retained exact-tip evidence.

## Review findings

The complete code/test diff against the exact canonical base was reviewed.
The fixed `p12f.travel-parties` provider retains the exact composed
`TravelPartyStore`, reports its active-party cardinality and store-owned
revision, and is exposed from bootstrap alongside the same store used by
`GroupTravel`. Add, Complete, and Remove serialize validation, overflow
preflight, owner changes and revision increments. No-op, rejected, guard-denied
and saturated operations leave the witness unchanged. Group start preflights
capacity for Add plus one possible compensation before prep/effects; arrival
preflights completion capacity before final member progress. Expedition rejects
a split-store composition and its production return method holds that same
reentrant owner window over the return flow and compensating Remove.

The amended saturation-boundary test is deterministic. While holding the
actual private reentrant TravelParty store window, it runs the real travel
preparation and start path, observes the Add at `long.MaxValue - 1`, performs
the real public Expedition lifecycle transition that makes association
ineligible, invokes the real private association operation, then executes the
same store Remove compensation before releasing the window. It asserts the
bounded owner result: count returns to zero and revision reaches exactly
`long.MaxValue`. It does not invoke `ExpeditionSystem.TryBeginReturn` as one
opaque coordinator and makes no cross-owner rollback claim. This is sufficient
for the reviewed Add+Remove owner-capacity/count/revision contract without
adding a production test hook: the test exercises the operation boundary and
the real monitor, while code inspection independently confirms the production
coordinator holds that monitor across the same sequence and calls the tested
store operations.

The supplemental cases now cover invalid/null and missing-ID no-ops,
duplicate-party and overlapping-member rejection, guard-denied Add, exact
owner/cardinality/revision at saturation, Complete's unchanged lifecycle flag
when saturated, and restored member balances after event-record compensation.
The evidence doc pins the validation to the reviewed test tree and records
SHA-256 for every XML/log pair. I recomputed the hashes and inspected each XML:

| Suite | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| TravelPartyCensusTests | 10/10 | `8584A284F5CBA6780EF82806C3D9942A7C9C57439D9FEF2D82E2D7594A39C155` | `0A2DA67FD42ABAA01847151CE5F23CE312482B33E7FAAB08016D0D04FF6BD4BB` |
| GroupTravelTests | 32/32 | `BFBD2F3E6B20DE24B92573409B0D627C962A8712B324F6858A139DF25E0E975A` | `E682808DB128CAE7F737AED0AD4D187FFAFA8129E65E953986F7B69EC01C7ED9` |
| ExpeditionTests | 14/14 | `965C8EB2968F971D22235FDABAB47412DDFDD6FAD88634C85A9B7EEC3272C96C` | `268E56028726C9630A89DED91E0633FDACBBDC1627E1E3DDB65438BF71C44831` |
| SimulationBootstrapCompositionTests | 14/14 | `69E0E64D0D7FF0D811025929880443D2AEB41849632848FB05D8CB24736CD714` | `E4B4B54A265E19AD5F5B31B1D11CA58E5235473207F68D9A50AEB9C26C724EA3` |
| ALL EditMode | 2061/2061 | `EDEDC75CE781CEFAF651B51B1B8B97C2E7A604AC8C37225F762AD755932EF1D8` | `66F0CDBA1A2DC6D04036C904DEB4E90A5FC8C1854C5BACB2D0C08CCC56C869E3` |
| Complete Smoke | 5/5 | `38337778D1388E7A5309E56CB3D242F432E78FEA92FDB8F0BDF006E3F8E9418C` | `9482ED7B92C32ACFF7B9DEFA32D48409B8CFFC6C40A53F47C263E3B066605FFA` |

The Expedition XML contains the amended compensation test as Passed. All six
logs point to the candidate worktree. The full EditMode XML reports 2061/2061
passed, and the complete Smoke filter reports 5/5. The tests were not rerun for
this review. `git diff --check` passed on the exact code/test diff.

## Architecture and alignment revalidation

The `INTRADAY_EXTENSIBILITY_ALIGNMENT.md` and
`MULTIPARTICIPANT_ACTIVITY_ALIGNMENT.md` constraints remain satisfied:

- Stable TravelParty instance identity is kept distinct from member
  `NpcRuntimeId` identities. The witness counts active party instances, not
  their participants, and performs no reconstruction or per-actor derivation.
- The existing member list supports multiple travelers and escorts. This work
  establishes no permanent Activity-to-Actor cardinality, NPC-owned authority,
  generic formation, or participant policy.
- The candidate leaves P18 scheduler, advance lease, temporal event identity,
  and P18-to-P12 ownership handoff untouched. It does not claim intraday
  execution or use the local party monitor as a runtime-wide owner-thread or
  quiescence proof.
- The slice remains a passive P12-B owner witness with its documented limits:
  no shared epoch, complete profile inventory, global owner-thread/quiescence,
  capture eligibility, export/hydration, or Phase readiness.

## Scope and review boundary

No blocker remains for this bounded candidate. This review does not promote
code or change Phase status. P12-B remains incomplete and P12-A remains
`WAIT_DEPENDENCY`; canonical promotion remains a separate human gate.
