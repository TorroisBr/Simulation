# Core-owned selected City census rebind — validation

Date: 2026-10-30

Candidate branch: codex/core/CORE-F-core-owned-runtime-application

Pre-checkpoint HEAD: 7d78d673c9bc88b87f8b28f5ec657efde28367c1

Core canonical: a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea

## Scope

This bounded change rebinds the four selected City owner-section census
families from the host snapshot to the exact DailyV1CityRuntimeOwners graph
when the entire City roster is selected:

- important NPC presence;
- Market stock rows;
- aggregate population;
- population operation receipts.

The rebind checks family shape, section identity, Required role, schema,
exact owner instance, cardinality and local revision. Aggregate population
and operation receipts must bind to the same exact population owner. The
aggregate census remains a singleton even when its current value is zero.
For mixed host composition, the complete host vector is preserved in its
original order; Core does not stitch a selected subset to compatibility rows.
Malformed or incomplete selected shapes fail closed. Stale host facts remain
available to the existing downstream stale-token classification.

This does not move the complete 89-family census, host-thread protocol,
mutation-epoch capture, or completed-boundary token publication into Core.
It does not complete DailyV1Application.TryStart, detached restoration, the
independent .NET host, or CORE-F, and it does not promote Core canonical.

## Changed-source identities

The exact changed files used for the validation runs were:

| File | SHA-256 |
|---|---|
| Packages/com.simulation.core/Runtime/Application/DailyV1RuntimeAdvanceStage.cs | ecf64a29cd5116c88db212d4c0308ea1dce1b5106daee02aa636b2f8380c2251 |
| tests/Simulation.Core.Tests/DailyV1CityEconomicOwnerCoreTests.cs | 63a9e95ddcdfe9aa979d3bbf94d7d90c336bba5002dcc2e6ae904749b167da97 |

The runner snapshots below remained stable. The CoreValidation input
inventory contains both changed-file hashes. Unity's input inventory includes
the production file; the separate CoreValidation suite compiles and tests the
.NET test source.

## Validation

| Gate | Result | Evidence |
|---|---|---|
| Official CoreValidation, cold | **955/955 PASS** | Run a96a85ceeb6e47d2bbc0dd3011913069; stable input digest 4082ae30e0ce4fdf4fdf64bdf2bfed771f3aae63ee5d5484ecb92115fcf5a041; private manifest SHA-256 93f098004d64f73f5be5d7e934baf6709aa6cbad3b53a50400b973c62dab6448; cold TRX SHA-256 a8fd280d59ad73e9cc5a24aa1278147fb0b30515638c3048a49f2548ff6ecaef. |
| Official CoreValidation, warm | **955/955 PASS** | Same run and stable input inventory; warm TRX SHA-256 567cdda9187518d42238ead233c74486a243d8eb5d6a4eca0acb6e129983791f. |
| Full Unity EditMode | **3282/3282 PASS**; zero failures, skips, or inconclusive | Unity 6000.3.9f1; run 7e695e7bc8854d7b97698382674f6ffc; 1,974 validation inputs; stable digest 8ca6da0b293e035e9ddc91a9c8b742a2e81c16a32e2e33006b2bb8a4b4410648; protected original XML SHA-256 526db6f3db7e400c21317c950e1275a8298010414a106218aa494d2ab335a972; protected private run-manifest SHA-256 28f3496e6a651564d600364e00a7ce2d0b8c32fd48f058114a7c58ec39f53d5c. |
| Official filtered Smoke | **5/5 PASS**; zero failures, skips, or inconclusive | Official EditMode filter Smoke; run ea99a5752092456c836e145fac42b924; same stable input digest as full EditMode; protected original XML SHA-256 207e4a1cc94ad3acc81a3e85bc49f953baba8ab4e58cd3855d6f869de7e6a52e; protected private run-manifest SHA-256 1c5e164e504f29363cea0b623f2db0d3b76842bbfed835a022cbf561c99f3e04. |
| Unity result package | **Verified** | Normalized EditMode manifest: UnityEvidence/coref-city-owner-census-editmode.manifest.json, SHA-256 9796de80eccba0bd167871e667de3fbc0c463534d3c27a2d0bf79201da2c69c7; result-only archive: UnityEvidence/coref-city-owner-census-editmode.zip, SHA-256 7aa679afb4ca191440b26898ddd2f9ff8e4561c3081b9e40eb8310a5cad40db2, 173,783 bytes. The official verifier reported VALID: runs=1 entries=1 against the protected original run manifest. The full EditMode report includes the same five Smoke tests. |
| git diff --check | **PASS** | Protected ProjectSettings files were not staged or included. |

The cold/warm TRX reports and Unity originals remain unchanged in protected
local evidence storage. Raw reports and logs are not copied into this
documentation bundle. Only the normalized result-only Unity package is
included. A preliminary invocation using the category selector chose zero
tests and is excluded; the successful official Smoke filter run above is
the counted result.

Unrelated ProjectSettings edits, untracked user files and protected evidence
were preserved and excluded from the candidate.
