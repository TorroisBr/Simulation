# P12 Daily-v1 Profile Separation Validation

**Base:** P12 canonical `b9fcb54840ae7c4e68d5f4d1812e5591bb36a948`

**Code candidate:** `75ca59af90d54f3fb307382740ce0ba3fa4e00fa`

**Code tree:** `e3fbbd53689a4dd582e086e8c1677935d21ef78f`

**Unity:** `6000.3.9f1`

**Profile:** `UnityBootstrap-Daily-v1` selects the dedicated P9-B-only
`Simulation-DailyV1.asset`; `Simulation-GeneralTest.asset` remains the
separate P10-A Ruin/LocalTopology profile and is rejected by Daily-v1 before
identity allocation/publication.

**Exact-tree results archive:** `P12DailyProfileSeparation-validation-20261006-final-exact.zip`

**Archive SHA-256:** `A935B91015870763B94582C2EE611D187AAC3BF7614F25DB0DC40A8DF025321A`

**Independent exact-tip review:** PASS; record in `REVIEW.md`.

| Suite | Result | XML | XML SHA-256 | Log | Log SHA-256 |
|---|---:|---|---|---|---|
| Selected Daily-v1 owner/cardinality inventory | 1/1 | `EditMode-20261006-031912-cf2c0b151d57484f83cd44b84556119e.xml` | `119C22B5E7D264EDFB81A64C55EAD7F98B9410E57EE211E28FB049A07EFA0275` | `EditMode-20261006-031912-cf2c0b151d57484f83cd44b84556119e.log` | `23CD4527FCF32D5DFA37A17E64A0FEB21196DA96E9221DD7D7AE30770D0B7177` |
| ALL EditMode | 2384/2384 | `EditMode-20261006-031927-bce66814086344548933cd603db2c8ee.xml` | `07562F812DEF1CA9EF41A5B57DB979CE3CB19CEFC37B92C038F6F4270B5A5008` | `EditMode-20261006-031927-bce66814086344548933cd603db2c8ee.log` | `A97DCB8B4223BCCA015B7EE7C2F9ECC4CA84A6660B0D22877A9C07CEC108DAE3` |
| Official Smoke | 5/5 | `EditMode-20261006-032001-d471d471953c479fb3823da8674a2564.xml` | `FC07F994A2E744D9F12DDAD0A336A652F9E9BA18DF235A5CA3729035C2D9F178` | `EditMode-20261006-032001-d471d471953c479fb3823da8674a2564.log` | `B2137E40393A1DC0DC62C2D834C039CB5DB522C5A61B08D0F71BF329E92C207B` |

All result XML files have zero failures, inconclusive tests, and skipped tests.
The exact-profile test sets the Daily-v1 admission context and verifies the
P9-B manifest/fingerprint and stage order. RuntimeIdentity cardinalities are
10 NPCs, 2 Cities, 2 Locations, 2 Routes, and zero ExplorableSites,
LocalPlaces, LocalConnections, and NotableItems. It checks the exact owner,
schema, cardinality, and revision for the selected City Market and population
sections, PersonStore membership/bindings, P8 authorities, RuntimeIdentity,
SpatialNetwork, NPC Inventory/MoneyAccount/Knowledge/SpatialKnowledge,
RuntimeIdAllocator, and other fixed passive witnesses. The Daily-v1 test also
confirms P10-A LocalTopology is absent; the full suite covers the separate
P10-A behavior and P10-A/P10-B Daily rejection regressions.

`git diff --check` passes for the code candidate. These checks validate the
corrected day-zero profile and listed witnesses. They do not establish
complete evolved-world owner coverage, full shared-epoch coverage, global
quiescence, capture eligibility, export, hydration, P12-A readiness, or P13
readiness. P12-B remains incomplete; P12-A remains `WAIT_DEPENDENCY`; P13
remains blocked.
