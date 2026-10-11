# P12-G successful directive effect parity — validation

Date: 2026-10-11

## Candidate and scope

- Canonical base: `b30a89686752777ad8c7e5f9019e3166136055eb`
- Code candidate: `3c20b93b084777d2fe57709efebb66e2f83cd084`
- Candidate Git tree: `f6a24951b2aca4df57121a6f9cd75ce939a4d737`
- Candidate Assets tree: `ebad02a9bf540a8ca16e12c5cc614c2a226fa903`
- Candidate code diff: two files, `P12CrimeSocialAppraisalCensus.cs` and `SimulationRuntimeAdmissionTests.cs`

The candidate adds one normal-flow successful `ScheduledDirective/RequestAction/EscapePrison` continuation-parity case. Jobson first commits a successful Steal action, producing populated Crime/Social appraisal owners and a warrant; a normal Guard Arrest action produces a populated sentence; restore occurs at the completed day-2 boundary; on day 3 the scheduled EscapePrison action succeeds and applies its bounty penalty once; day 4 confirms no replay. The assertions compare owner projections, continuation roots, directive state/revision, custody/sentence state, and source/restored parity across the tested boundaries.

The required production compatibility correction is limited to restored-candidate Crime/Social census registration. Fresh bootstrap still requires exact empty cardinality/revision. Restored continuation accepts populated witnesses only when cardinality and revision are nonnegative, while preserving exact section IDs, schema versions, owner-instance identities, and the remaining census contracts. This follows the existing P12-E owner snapshot contract for post-publication supported Crime/Social writes. The fixture injects no warrant, arrest, sentence, directive result, or other gameplay state.

This is one bounded P12-G parity slice. It does not claim complete P12-G, universal replay parity, full owner/epoch coverage, runtime-wide quiescence, capture eligibility, P12-A readiness, export/hydration, P13 readiness, or Phase closure.

## Final validation

All listed runs are from the final candidate code tree. XML reports have zero failures and skips.

| Gate | Result | XML SHA-256 | Compressed log SHA-256 | Decompressed log SHA-256 |
|---|---:|---|---|---|
| Focused successful directive case | 1/1 | `8C881320ABFD402F216AB2CC956A89C4CD7369934326920F13F6B9FC193CEC1C` | `503AEFF1D9ED3236CAA5037EEEE979E589C7BDA6BD6BED82EA14B545C6A75BFD` | `B3D87275ACD9F8FA577E8A39005A351E2139F95A761A504D4B90AA3F6A410108` |
| Crime/Social invalidation regression | 12/12 | `03260778FA7833BCCD15E1236F458024D58A22C8EA7F0A1960F77637131A5396` | `8025AD69F502D4262CB16D8797A0B76BB26692E2939D6324898DD3D50CCC5747` | `B96B668B8F9DDEF87005367B2D33D9FF5430976D7415331F197E6853C8480F6A` |
| P12-E Crime/Social snapshot regression | 6/6 | `CC91E62A39C854A2EC710185DB81BC1A272D9393FBF403D249F2425303CBB621` | `F55BE815D312E1E0076C39139075550D6B1E3C18A046EC80B28FA5814CD9E4CA` | `FEC6B317F02D971DA82FDFB157324957817ACDDDFE3D6858A35ED9CA8AC74735` |
| ALL EditMode | 2866/2866 | `17AF23035296F40BA645788CE641C906EAD8EC1041E2AB123D99C4606A9BFC1D` | `C7F1D58FE2E8212119AD442404DF847D16A483944F1D8E004CF0E34B7D325F4D` | `C67C9CEC2FBD7858C0F686058CCD045AE9645FC6A01931EF2A50E26D032524D1` |
| Official Smoke | 5/5 | `DA67AC58ECB08933EF99356BAF11477F4583D57B8A97496ABB903BEAD77CDD95` | `80EB54E5D6CAA619BA1722DC98C48D9C66B484A13E733196F42E709DF3F8D3F2` | `505B0FD8C226E1AE3D7A0542C7D9F82C28F9CA0D8B6CF2A6E35BB9145F4EE912` |

Artifact paths under this directory preserve the XML and compressed logs for those five final runs. `git diff --check b30a89686752777ad8c7e5f9019e3166136055eb..3c20b93b084777d2fe57709efebb66e2f83cd084` passed. Earlier exploratory runs are excluded from this final evidence set.

## State boundary

This validation does not itself promote the candidate or change canonical State. P12-G and P12-A remain `WAIT_DEPENDENCY`; P12-B through P12-F remain promoted within their recorded scopes; P13 remains `BLOCKED`; Phase 12 remains open.
