# P12-B FactionStore census and mutation validation

**Status:** PASS
**Canonical base:** `54e95a325812baa8db2fe233d3dd566be93f7fa2`
**Reviewed design:** `f4c1218a8f9969f186fe32076d1470a8bcfbd246`
**Implementation code commit:** `8bb6c61e8bd636b4b99a85d7bc96ad379f10c04d`
**Implementation code tree:** `286743c7ac4c45fc0cea812749ce5d1e14e0151b`

Validation ran against the implementation above. The only subsequent change
to the checkout before these retained runs removed trailing whitespace from
the design-review record; no executable source or test changed. The current
candidate must still pass exact-tip independent review and `git diff --check`.

## Test results

| Suite | Result | XML artifact |
|---|---:|---|
| `P12FactionStoreCensusTests` | 5/5 | [`EditMode-20261006-225701-62a56b7398784e98baac3278136e55bb.xml`](Raw/EditMode-20261006-225701-62a56b7398784e98baac3278136e55bb.xml) |
| `FactionFoundationTests` | 14/14 | [`EditMode-20261006-225711-fb83d3f5479a40668042495ef7ae6955.xml`](Raw/EditMode-20261006-225711-fb83d3f5479a40668042495ef7ae6955.xml) |
| `FactionFactualReaderTests` | 7/7 | [`EditMode-20261006-225721-b033aa16955043f2a80e538d388861ff.xml`](Raw/EditMode-20261006-225721-b033aa16955043f2a80e538d388861ff.xml) |
| `SimulationRuntimeAdmissionTests` | 50/50 | [`EditMode-20261006-225731-247e8586367c4404aacc1e44f7482642.xml`](Raw/EditMode-20261006-225731-247e8586367c4404aacc1e44f7482642.xml) |
| `SimulationBootstrapCompositionTests` | 24/24 | [`EditMode-20261006-225741-83c5381cd85147efa7c96f7458ae466d.xml`](Raw/EditMode-20261006-225741-83c5381cd85147efa7c96f7458ae466d.xml) |
| ALL EditMode | 2422/2422 | [`EditMode-20261006-225751-4e4eed9eccac432aa7b91323a09d1366.xml`](Raw/EditMode-20261006-225751-4e4eed9eccac432aa7b91323a09d1366.xml) |
| Official Smoke (`EditMode -TestFilter Smoke`) | 5/5 | [`EditMode-20261006-225822-24177c3aa61a4ad98cea22c920f57276.xml`](Raw/EditMode-20261006-225822-24177c3aa61a4ad98cea22c920f57276.xml) |

The Unity logs are retained in [`Unity-logs.zip`](Unity-logs.zip), SHA-256
`59C7C7160105CB2AB871FFB58B6E6C25EA4910065F323522985B4C4C19D3B6AC`.
Each XML and log was emitted by `Tools/UnityValidation/Invoke-UnityValidation.ps1`.

## XML SHA-256

| XML artifact | SHA-256 |
|---|---|
| `EditMode-20261006-225701-62a56b7398784e98baac3278136e55bb.xml` | `C928D4A43735E3D0EE8D460E332BF17F5A6C827134455073C8790085C7F972BD` |
| `EditMode-20261006-225711-fb83d3f5479a40668042495ef7ae6955.xml` | `A44229279C83887AA57197678F38E03D90BEA5B3C74432F069D011BB26112A39` |
| `EditMode-20261006-225721-b033aa16955043f2a80e538d388861ff.xml` | `49B7A1B3DA941177A4C13103F75ABB477A0FEEA6286B9A610623217AA73A5D6B` |
| `EditMode-20261006-225731-247e8586367c4404aacc1e44f7482642.xml` | `7A822D5599A2E3E12DBACD07F76EC34A331795AFF36EC81E890E495E384D1B64` |
| `EditMode-20261006-225741-83c5381cd85147efa7c96f7458ae466d.xml` | `5EBC54437D225CD0269416DCDDC27638F72D434A17F64A0F623210FB971533EB` |
| `EditMode-20261006-225751-4e4eed9eccac432aa7b91323a09d1366.xml` | `4BB6B41139856D1F3F9186FF7BDFB7907B8D4F4D284D61E71D53AD6C0A0F5377` |
| `EditMode-20261006-225822-24177c3aa61a4ad98cea22c920f57276.xml` | `520418015C1DC1CF7900A0955D58EBF555A79769FC30B8E0FED72884A756FA1F` |

## Scope retained

The tests verify the two Required FactionStore sections, exact installed owner
identity, separate faction and affiliation cardinalities, shared local
revision, one mutation epoch per successful facade commit, and no epoch or
owner change for rejected commits. They also cover off-owner-thread admission,
stale baselines, epoch exhaustion, and the 253-to-255 selected Daily-v1 section
inventory.

This result does not claim complete P12-B owner or writer coverage, complete
shared-epoch coverage, global quiescence, capture eligibility, export,
hydration, P12-A readiness, P13 readiness, or Phase 12 closure.
