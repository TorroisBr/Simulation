# WX-D v2 implementation candidate evidence

**Status:** Candidate assembled and revalidated on the current Phase 12
canonical. Independent exact-tip implementation review is pending.

## Immutable candidate identity

- Simulation canonical base: `e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`
- WX-D code commit: `bc3d4a31e45549fd91cd22a88022c93d39f2707f`
- WX-D code tree: `088f383db5b4b5fc51e8db0f8687c413709a5796`
- Current branch: `codex/wxd/WXDv2ProducerCurrentBase`
- External contract: Simulation-External `origin/main` at
  `0ce8403ba05f778db6850f566a974a4c56cf4edb`, live ref independently checked
  with `git ls-remote` on 2026-10-03.
- External v2 schema source blob:
  `5619013647c31d969a7cd49e0563ff68c78cdde3` at that External tip.

The latest canonical advances from the earlier reviewed design baseline were
replayed and audited in
[`WXD_V2_LATEST_CANONICAL_REVALIDATION.md`](WXD_V2_LATEST_CANONICAL_REVALIDATION.md).
The Event-counter promotion changes P12 runtime/census invalidation hotspots,
but does not change WorldId publication, the FR-B capture/admission contract,
FR-C ownership/results/diagnostics, or the Faction and Person owners. WX-D
does not edit the shared runtime/bootstrap hotspots. Classification remains
`REINTEGRATE + REVALIDATE`; validation below was performed on the replayed
candidate against the current base.

## Implementation scope

The producer emits only schema v2. A narrow Unity host accepts a published
`SimulationBootstrapComposition`, calls its factual-read surface once for
`simulation.faction-truth/v1`, and passes copied facts and coherent boundary
evidence to the embedded producer package. It does not access a Store. Faction
coverage is `INCLUDED` when the complete Faction collection is nonempty and
`KNOWN_EMPTY` when it is empty. People, Cities, Locations, Organizations,
Institutions, Items, HistoricalEvents, and Relationships are empty with
`UNSUPPORTED` coverage. A failed, unavailable, malformed, or incoherent read
produces no artifact.

Mapping follows the promoted design: `WorldId.Value` is the sole `world.id`
source; Faction IDs use `sim:faction:` plus unpadded base64url of strict UTF-8
Faction IDs; only a nonblank source-owned Faction name is emitted. Active
affiliations, membership links, Relationships, Knowledge, support facts,
world name, and unsupported claims are omitted. Serialization is deterministic
UTF-8 JSON and publication is atomic.

## Exact code delta from canonical

`git diff --name-status e76e50bfefc68b827d54ceb74e0b25293e0ad7b8..bc3d4a31e45549fd91cd22a88022c93d39f2707f`
contains only the following WX-D additions and package registration changes:

- `Assets/_Project/Scripts/WorldExchange.meta`
- `Assets/_Project/Scripts/WorldExchange/SimulationWorldExchangeExportResult.cs`
- `Assets/_Project/Scripts/WorldExchange/SimulationWorldExchangeExportResult.cs.meta`
- `Assets/_Project/Scripts/WorldExchange/SimulationWorldExchangeExporter.cs`
- `Assets/_Project/Scripts/WorldExchange/SimulationWorldExchangeExporter.cs.meta`
- `Assets/_Project/Tests/EditMode/Editor/WorldExchange.meta`
- `Assets/_Project/Tests/EditMode/Editor/WorldExchange/WorldExchangeV2ProducerTests.cs`
- `Assets/_Project/Tests/EditMode/Editor/WorldExchange/WorldExchangeV2ProducerTests.cs.meta`
- `Packages/com.simulation.world-exchange-producer.meta`
- `Packages/com.simulation.world-exchange-producer/Runtime.meta`
- `Packages/com.simulation.world-exchange-producer/Runtime/Simulation.WorldExchangeProducer.asmdef`
- `Packages/com.simulation.world-exchange-producer/Runtime/Simulation.WorldExchangeProducer.asmdef.meta`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2Artifact.cs`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2Artifact.cs.meta`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2FilePublisher.cs`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2FilePublisher.cs.meta`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2JsonWriter.cs`
- `Packages/com.simulation.world-exchange-producer/Runtime/WorldExchangeV2JsonWriter.cs.meta`
- `Packages/com.simulation.world-exchange-producer/package.json`
- `Packages/com.simulation.world-exchange-producer/package.json.meta`
- `Packages/manifest.json`
- `Packages/packages-lock.json`
- `docs/design/WXD_V2_CURRENT_BASE_REVALIDATION.md`
- `docs/design/WXD_V2_DESIGN_REVIEW.md`

The code delta is 24 added/modified paths. It makes no changes to
`SimulationRuntime`, bootstrap composition, World Identity, factual stores,
FR-B, FR-C, or Simulation-External. Unity-generated ProjectSettings and
unrelated `.meta` changes in the worktree are unstaged and excluded.

## Validation on the exact code tree

All runs below were executed on code commit
`bc3d4a31e45549fd91cd22a88022c93d39f2707f` / tree
`088f383db5b4b5fc51e8db0f8687c413709a5796`, based on canonical
`e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`. XML/logs are retained beneath
`Library/ValidationResults`; hashes are SHA-256.

| Gate | Result | XML (SHA-256) | Log (SHA-256) |
|---|---:|---|---|
| WX-D `WorldExchangeV2ProducerTests` | 7/7 | `WXD-v2-focused/EditMode-20261003-144049-fee698e02ebb45248ea3590e6d3be62e.xml` — `8B9123172CB15B6A4D91211511AFF950A7F6E69406286BADDF1F4915E726DD08` | `WXD-v2-focused/EditMode-20261003-144049-fee698e02ebb45248ea3590e6d3be62e.log` — `5D71F4021F906167EE2DBB0D6CA48455942018FDBCEA6FC652A2E3897BB80423` |
| `FactualReadFoundationTests` | 9/9 | `WXD-v2-regressions/EditMode-20261003-144302-aade6fde8a4f4b6d81aa3e1c130f3fee.xml` — `AB8899A0CFACB7B860DABAC1D62523D8DCD6FE14F5A039970032A547AD6A6224` | `WXD-v2-regressions/EditMode-20261003-144302-aade6fde8a4f4b6d81aa3e1c130f3fee.log` — `8A9916858FA7DFE8E641D5C2A0E559DBEC2172E43DF2426DF94BC13205861F51` |
| `FactionFactualReaderTests` | 7/7 | `WXD-v2-regressions/EditMode-20261003-144316-f07957a8e1184984b3a066a3441d3304.xml` — `C76FD823F53FAE6CF2ADB566C796B6C3EBCAEFCBD83BD2C2C4E272E5783186EE` | `WXD-v2-regressions/EditMode-20261003-144316-f07957a8e1184984b3a066a3441d3304.log` — `AF8D3139B157068A44681EBD9C408DDE5F30511066AAE0EF59ADEAE37816E9E2` |
| `SimulationBootstrapCompositionTests` | 21/21 | `WXD-v2-regressions/EditMode-20261003-144330-7166bc76f7be4a938c9d3f9d4d63424b.xml` — `95505CEF830FA51AAA8A6BEF06C733FD023AF99BA619E9C99AFA6B7DFD0EED39` | `WXD-v2-regressions/EditMode-20261003-144330-7166bc76f7be4a938c9d3f9d4d63424b.log` — `E2CDD04B2859BC1B4D4174ADA9F970150CDDB7D1543DF7AA9992FB6097FACADA` |
| ALL EditMode | 2212/2212 | `WXD-v2-all-editmode/EditMode-20261003-144346-75874fe543c84e47894eaead6f3c5069.xml` — `A1DB208437AADDC18D7843058D10CF9BC2C3B5F543BB4548026C861D195D540C` | `WXD-v2-all-editmode/EditMode-20261003-144346-75874fe543c84e47894eaead6f3c5069.log` — `44B31A4571EEC2E6431A0992422B1ED9C933A021FC87F770D454DCC33E7F3FE2` |
| Official EditMode Smoke | 5/5 | `WXD-v2-smoke/EditMode-20261003-144418-34ad4f23c17e4e41bd480a7c2872ce16.xml` — `8E98BA12FB4A001F362BB9CAD7F175E116E845462439A165A71C0E5986E88830` | `WXD-v2-smoke/EditMode-20261003-144418-34ad4f23c17e4e41bd480a7c2872ce16.log` — `41703CEADA45F2D8CBEA34F5C2CB5FEA039246C5A3EA1FFE2EA7340833350A05` |

`git diff --check origin/codex/phase12/canonical...HEAD` passes. The current
canonical remote was refreshed and its live ref verified as
`e76e50bfefc68b827d54ceb74e0b25293e0ad7b8` before candidate evidence was
prepared.

## External schema conformance

The actual first-Faction artifact emitted by the Simulation tests is retained
at `Library/ValidationResults/WXD-v2-conformance/first-faction.world.json`
(SHA-256 `AD20EF75B379E4A03E361719ED30A642F28DB54300987B9C0D6280AA579EDE5D`).
The pinned External `validateWorldExchange` function accepted it with
`valid: true`, no issues, `schemaVersion: 2`, and two Factions. Coverage is
`factions: INCLUDED`; the other eight collection statuses are `UNSUPPORTED`
with empty arrays. Simulation-External is clean and unmodified.

## Promotion boundary and remaining review

No numbered Phase canonical ref has been moved or changed by this work. The
candidate is ready for independent exact-tip code review against base
`e76e50bfefc68b827d54ceb74e0b25293e0ad7b8`, code SHA/tree above, and this
candidate documentation commit. Upon review PASS, the review record and
candidate will be durably pushed for Phase Master consumption. Phase Master
retains sole ownership of `codex/phase12/canonical` promotion.
