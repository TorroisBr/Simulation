# P12-B TravelParty census candidate

**Status:** Implementation candidate; not promoted. P12-B remains incomplete
and P12-A remains `WAIT_DEPENDENCY`.

**Base:** canonical `codex/phase12/canonical` at
`e9ced8e451f42e80ed2132ce494cd5c26e439894`.

**Reviewed design:**
`docs/design/PHASE12_P12B_TRAVEL_PARTY_CENSUS_DESIGN.md` at
`765e346214e128104ce18c967121ef29abb88195`; durable design review branch
`codex/phase12/P12BTravelPartyCensusDesignReview` at
`194661af6a1d7e9c901a17f35ce2dc7ff4d8fdf8`.

## Delivered candidate slice

The candidate adds the fixed passive `p12f.travel-parties` schema-v1 witness
for the exact `TravelPartyStore` installed in bootstrap. The owner now exposes
a local monotone revision. Successful public Add, Complete, and Remove commits
advance it once; no-op, rejected, guard-denied, and saturated operations do not
change the owner or revision. Revision capacity is checked before writes.

A private reentrant store monitor serializes those public mutations and the
compound TravelParty start, daily advance/completion, and Expedition return
windows. Start reserves capacity for Add plus its possible compensating Remove
before travel preparation or non-store effects. Final arrival checks the
completion capacity before advancing any member. Bootstrap publishes a fixed
provider backed by its already-installed store. `ExpeditionSystem` rejects a
split TravelParty owner and holds the same store window across return start,
association, and compensation.

Focused tests cover witness identity and counts, Add/Complete/Remove revision
semantics, capacity saturation, start-event compensation, saturated arrival,
cross-thread blocking for each public mutation, ordinary start/arrival, normal
selected-profile bootstrap identity, and split-store rejection.

## Validation

The implementation is based on the canonical SHA above. The final code-bearing
tip for fresh exact-tip review is
`260a688f7ea1a9f86e9b589788cda2c32357e170` (test tree
`bf95798acdf932a91d54f52c8604ba20928f2cd0`); production code is unchanged from
`8c446ed8920028566b8af84583e7599bc907791c`. The return-association test does
not intercept or pause the production `ExpeditionSystem.TryBeginReturn`
coordinator. Instead, without a supported callback seam or production-only
test hook, it deterministically executes the real public lifecycle transition,
TravelParty start, failed association, and store removal stepwise while holding
the same store mutation window. It asserts only the TravelParty Add+Remove
count/revision result, including the saturation boundary; it does not claim
cross-owner Expedition, NPC, or cost rollback.

Validation ran on the exact test tree including the correction above. Stable
XML/log pairs are retained under
`Library/ValidationResults/P12BTravelPartyCensus/` in this worktree. SHA-256 is
listed for each XML and matching log:

- `TravelPartyCensusTests`: 10/10 — `EditMode-20260930-222850-c0086781402b4eb3a37181343df9e39c.xml` (8584A284F5CBA6780EF82806C3D9942A7C9C57439D9FEF2D82E2D7594A39C155); matching `.log` (0A2DA67FD42ABAA01847151CE5F23CE312482B33E7FAAB08016D0D04FF6BD4BB)
- `GroupTravelTests`: 32/32 — `EditMode-20260930-222907-b00250be692d48e1b4f056c747a8dc80.xml` (BFBD2F3E6B20DE24B92573409B0D627C962A8712B324F6858A139DF25E0E975A); matching `.log` (E682808DB128CAE7F737AED0AD4D187FFAFA8129E65E953986F7B69EC01C7ED9)
- `ExpeditionTests`: 14/14 — `EditMode-20260930-222924-8b70bc29b5e24886a9ae904199467731.xml` (965C8EB2968F971D22235FDABAB47412DDFDD6FAD88634C85A9B7EEC3272C96C); matching `.log` (268E56028726C9630A89DED91E0633FDACBBDC1627E1E3DDB65438BF71C44831)
- `SimulationBootstrapCompositionTests`: 14/14 — `EditMode-20260930-222940-fc562c7b667141d88563aa47f3c98b70.xml` (69E0E64D0D7FF0D811025929880443D2AEB41849632848FB05D8CB24736CD714); matching `.log` (E4B4B54A265E19AD5F5B31B1D11CA58E5235473207F68D9A50AEB9C26C724EA3)
- ALL EditMode: 2061/2061 — `EditMode-20260930-222957-7ae50b5b080745f4a2073ab127b8459e.xml` (EDEDC75CE781CEFAF651B51B1B8B97C2E7A604AC8C37225F762AD755932EF1D8); matching `.log` (66F0CDBA1A2DC6D04036C904DEB4E90A5FC8C1854C5BACB2D0C08CCC56C869E3)
- Complete official `Smoke`: 5/5 — `EditMode-20260930-223034-55b42c9e50894a3da872d12da860b9bf.xml` (38337778D1388E7A5309E56CB3D242F432E78FEA92FDB8F0BDF006E3F8E9418C); matching `.log` (9482ED7B92C32ACFF7B9DEFA32D48409B8CFFC6C40A53F47C263E3B066605FFA)
- `git diff --check`: passed.

`git diff --check`: passed. Independent exact-tip code review is pending. This
branch is not a canonical promotion.

## Scope limits

The passive witness is unsynchronized; it does not establish owner-thread
identity, quiescence, shared-epoch invalidation, a complete profile census, or
capture eligibility. The candidate does not implement P12-F export/hydration,
snapshot or graph validation, or continuation parity. The mutation window only
protects TravelParty membership and revision; it does not make Expedition,
NPC, cost, or runtime state generally thread-safe. Return-association
compensation asserts only TravelParty store count/revision; the existing
Expedition path does not roll back NPC travel fields or costs after a nested
party start has succeeded. Arbitrary same-thread callbacks that re-enter the
store during a compound operation remain outside the supported path.

P12-B remains incomplete, P12-A remains `WAIT_DEPENDENCY`, and no later P12
checkpoint becomes ready solely from this passive owner witness.
