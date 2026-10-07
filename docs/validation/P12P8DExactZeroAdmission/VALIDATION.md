# P12-B P8-D exact-zero census validation

**Canonical base:** `e5405cf897c30224f86ce605a9efe6777f93749a`
**Base tree:** `fe631443c806e9cde4a40df186d1c2b57247b578`
**Implementation code:** `7e827b3fe4b8effefd682838c6be575d25eab501`
**Implementation tree:** `127edf616d99bca0614041b54f72ae5addeb26b2`
**Unity:** `6000.3.9f1`
**Results archive:** [`P12P8DExactZero-validation-20261007.zip`](P12P8DExactZero-validation-20261007.zip)
**Archive SHA-256:** `F7B0F110D8F83D865DD73BD6F1CB0961A9083F581325A8EDB68328A9E8447617`

| Gate | Result | XML SHA-256 | Log SHA-256 |
|---|---:|---|---|
| Current canonical Daily-v1 selected bootstrap/admission and owner/cardinality inventory, before P8-D registration | 1/1 | `FB5589D32FFCEC3082605CFF3D8A5722BBED865E0089FB6EFAFDFE46AE577A91` | `6F6103B29237D9CC605E6A51EB892D0A4F33B3E5627DC9637176542D68669052` |
| Focused `SimulationBootstrapCompositionTests` on implementation tree | 24/24 | `64B545457AA68B762551AE3779DD490C7E7CCC19A29DA910DC60C07302B14B5B` | `31E9C0E5CC5009DC9A2D2E5F3C2E66438FA9C27ECF921B902525AF4D70923F07` |
| Initial ALL EditMode attempt, diagnostic only; superseded after fixing the stale assertion | 2433/2434 | `7B10B231DE52EB8CC1DA8763AF2A96E760084DB6E2856A85E2A1D754056AC4D9` | `D01E51A3B0553E3CFFE5A8D52D97D7D6F9666FD109347C2347D17670828BEC86` |
| ALL EditMode on implementation tree | 2434/2434 | `4231BEE5200A11A94AD291E995898A65B86FD7CEA712F36F3A11EE3674993491` | `F106101524031E9041ED1396547CE0F1A48BD8C728073EA15A2C2BF953184B1B` |
| Official EditMode Smoke on implementation tree | 5/5 | `04F354D24B291F9D378DFF6594D10C37C951E492127C1A6A8D395C0970627B8E` | `4A1D51B39EC8B2B6BE9AFE64166B8004D4F8E18B1DE92A4751D80E394C28B291` |
| `git diff --check` | PASS | — | — |

The initial ALL EditMode attempt reported 2433/2434 because
`PropertyEstateMutationEpochTests` still expected 258 sections. That test
expectation was updated to 260; the exact implementation tree then passed the
full suite at 2434/2434. The diagnostic XML/log are retained in the archive.

The baseline profile test executed the actual dedicated
`Simulation-DailyV1.asset` under the `UnityBootstrap-Daily-v1` admission
context. It verified the P9-B manifest/fingerprint, P10-A absence, 10 NPCs,
2 Cities, 2 Locations, 2 Routes, zero site/local-topology identities, and
exact owner/schema/cardinality/revision witnesses across the then-current
258-section partial inventory. The implementation composition test verifies
the resulting 260-section partial inventory and the registered P8-D providers
against the exact installed owners, including zero count/revision and empty
route-plan history.

These results do not establish complete owner/operation/shared-epoch coverage,
global quiescence, capture eligibility, export, hydration, P12-A readiness,
P12-B completion, or P13 readiness.
