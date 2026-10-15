# CORE-F WP1-Values validation

## Intermediate integration evidence, not a promotion verdict

Source `dad86c6cdcaac43321fd0b27d6763bc2b103bb0b` was independently reviewed
against promoted Core `8fc975631db0d4e53fa58c490c752fb79d209b0e` and approved
architecture `4da844dee0c69fdf66562590fa96cc728fc86bf7`. Review found a remaining
Unity typed-adapter field-by-field parity test gap. This package is historical
intermediate evidence; it is not full WP1 closure or full CORE-F readiness.

Core cold/warm run `a6239afd2d4f42148aa497fdd775eceb` passed169/169 in each
attempt using the pinned SDK and selected CLR, with129 explicit input files,
locked dependencies and no Library. The complete normalized TRX package is
`core-f-values-cold-warm.manifest.json` and matching ZIP; archive SHA-256
`e8047074cbdf5e6790d7d0a2c2dbcf8878c6ceec561c1277c56232e1704c7f62`.
Protected original comparison passed. This is distinct from archive-only
verification. Originals and matched validation binaries remain protected and
unchanged; private paths/binaries are not published.

Unity focused characterization/budget run `49505501fe5442e59de883248a5c407e`
passed3/3 from the same source. Broader configuration/bootstrap/genesis/P10/P12/
restore-focused run `b3e322a786de4fdc9825d79775bed5e1` passed. These runs retain
original case/source identities in protected storage; final full gates and
complete Unity report package must follow the additional typed adapter proof.

Preceding diagnostic runs caught private nested-member/cross-assembly constant
access and a test-helper mistake decoding plain hash values as base64. Those
were corrected before these passing runs; no production parity mismatch was
masked. Earlier diagnostic runs are not final completion evidence.

The fixed C# constructor fixture regenerated identically from the independent
718 member captures and164-row source map. Normalized generated-source SHA-256:
`0f7711d29f323c06ee09b759a9d003b783dfd581ab6c08dece7cf89132a7d4b0`.
Expected P12 bytes are output vectors only; generator input is not the encoded
catalog. Baseline Unity fixture bytes remain unchanged at SHA-256
`422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.


## Final closure evidence — source e2184090

Exact tested source: `e2184090f0803a2b0ede16d00da0742189fab9be`.
Tree: `69667f7e485cc15242ae071aae659330395ac111`.
Assets: `47992d69c9484524f515a190100fedbe3abb6dc0`.
Packages: `121020c847703090757766b7b355203c71b7559f`.
ProjectSettings: `db5c1636993b7f82f2409e07300df2744a5dfe85`.
CoreValidation: `43e9645d454bf710e8f69686e30dfd31b2cf5346`.
UnityValidation: `965c46eef5d172f96cec6a17fd39bdc18ffde1f0`.
Core base: `8fc975631db0d4e53fa58c490c752fb79d209b0e`.
Architecture: `4da844dee0c69fdf66562590fa96cc728fc86bf7`.

The initial field-parity gap is resolved by one generated C# constructor fixture
shared from Editor/Core via an explicit SDK link. The pre-migration baseline
remains under tests/Simulation.Core.Tests/Fixtures, unchanged at SHA-256
`422805cbe373cf1410ee6a928d363fa1ef413dbcf50dc062dce7867ed4fdd1b3`.
The generator consumes718 independently captured member occurrences and164 mapped
rows, not expected P12 bytes. It produces typed values driving the actual P9
builder (all184 records/hash) and raw values driving the sole P12 encoder (complete
bytes/hash,39 records including one profile and38 referenced assets).
Final normalized constructor source SHA-256:
`c7280804cb43a597992fd59667c646b8991e6d0d9ea22042c03f837e83becadc`.

The Unity comparison checks actual adapter output recursively across root/effective
configuration, calendar, all six ID tables, nested properties, enum numeric and
symbolic values, float/double bits, reference IDs, null/list shape/order and
identity inputs. The copy-immunity test mutates actual source scalars, nested
production and city-list references after capture, checks unchanged copied values,
and restores source state in finally without saving assets. The separate raw P12
boundary remains intact. The fixture location is test-only shared ownership, not
a new runtime loader or extension API.

| Required final gate | Protected run | Result |
|---|---|---|
| Core cold/warm, pinned SDK/CLR,132 inputs, no Library | `b42be6cd2a0d4e5c860fb268376013a8` | 169/169 each |
| Locked Lab cold/warm | `84430f74092649f8b469876a861af793` | exact four-line PASS |
| Explicit typed/copy filter | `30bd22fab633429daf07391cd2816fd9` | 2/2 |
| Config/bootstrap/genesis/P10/content/P12/restore/typed | `904b3e9fe34845f499841875b4aca4aa` | 340/340 |
| ALL EditMode | `077e36b28c624f9cac8466fcada9e37d` | 3169/3169, no nonpassing cases |
| Official -TestFilter Smoke | `8d3be4885e604f4da29f43503a1e1bfc` | 5/5 |
| Original LongRun in ALL | same ALL | 7/7, original100/1000/5000-day horizons |
| Base diff-check | final source above | PASS |

Both exact typed test names are individual passing nodes in focused, broad and
ALL XML: `DailyV1DefinitionAdapterTypedParityTests.UnityAdapterMatchesEveryFixedTypedDefinitionValue`
and `.AdaptedTypedDefinitionsRemainCopiedWhenSourceAssetsAndListsChange`.
Independent review confirmed actual execution; a filter/assembly match is not
execution proof. All four Unity inventories match and are stable before/after;
every filtered case joins to a passing ALL case. The owner test's actual P8
fingerprint is unchanged from its pre-Values source-bound witness:
`db509154628cf74ffd21d01fd9b37e8d31f1e9fc39ee34b151eed4132b22b017`.

Complete final packages:

- `core-f-values-typed-cold-warm.manifest.json` and matching ZIP. Archive SHA-256:
  `0fda9451e8b7faf4d37210b119a134400fa0e211504fb5460919bd46ddcefacb`.
  Private original comparison: originalComparisonVerified. Separate archive-only
  check: originalComparisonNotPerformed. Actual SDK/runtime/assembly/source
  identity and132 inputs are bound by the package.
- `core-f-values-typed-all.manifest.json` and matching ZIP. Archive SHA-256:
  `a05a20f21d903d9db5bae11065cd665145da3ca0730fe1307cd7bb7e2e588738`.
  Verification against protected originals passed. Original ALL XML SHA-256:
  `3332e47d0302d709f6e897eaf8f250cd8298cc89653e7d0bb2ddbea8b2525d65`.

The preceding `core-f-values-final-*` bundles are superseded ab7dd68 intermediate
evidence: Unity ignored two new C# assets with malformed importer metadata, so
those reports do not satisfy typed-parity closure. Root corrected only their
metas to valid MonoImporter form with the same GUIDs and reran required gates.
No report was rewritten. Other diagnostic runs remain protected and excluded.
Filtered aggregate packaging limitations are unchanged; the complete final ALL
package supplies retrievable case results. Original reports/logs/binaries remain
immutable and private; no private locators or binaries are published.

Delivery is the five-file pure configuration closure, complete copied selected
values, one P9 identity builder and one selected P12 catalog encoder with Unity
conversion adapters. Existing default/error/order semantics and truthful build
compatibility remain; continuation in the new Unity build passes. No .NET runtime
host, owner extraction, cross-host save, profile expansion, P12 reopening, full
CORE-F or CORE-X readiness is claimed. Final exact-tip evidence review and refreshed
promotion preflight are recorded separately. Docs-only successors may reuse these
runs only after effective-input applicability is confirmed.


## Final source representation alignment — authoritative .NET pair

The final paired .NET result is now `core-f-values-aligned-cold-warm.manifest.json`
and matching ZIP, run `7932d8734925477bb138ad634bc84bf7`, source
`e2184090f0803a2b0ede16d00da0742189fab9be`,169/169 cold and warm, Library absent.
Archive SHA-256:
`946a0543ed9d731cffab28e2d2d734261fa3ed6021effc3dbe7cb950db780b8e`.
Private-original verification passed; distinct archive-only verification reports
originalComparisonNotPerformed. Fresh locked Lab run
`59adfcfa0698469b88eb776097abb1ee` passed its exact four-line cold/warm output.
These supersede the earlier typed cold/warm pair as the final .NET gates.
The final Unity typed ALL/focused/Smoke results above are unchanged.

Independent evidence review found10 LF/CRLF representation differences between
the earlier SDK checkout and the preserved Unity checkout (eight overlap Unity's
capture roots). Every difference was proven to be EOL-only against the exact
canonical Git content. The supported Git clean filter and existing autocrlf policy
map the captured working bytes to the committed source; a raw hash of differently
filtered Git text alone is not a dirty-source finding. In particular the preserved
Unity codec test file has87,253 bytes and SHA-256
`2d1bd1503b3798b158a0b00d9a90a6092ccbc0133ca53925868a45462dc02855`,
exactly its captured hash, and canonical Git blob
`c390ec65f03d9098f4702bf0cee5bac73a4c2772`. All1,635 Unity capture files were
independently matched against the preserved actual run-source checkout.

To make the paired evidence raw-byte comparable, the Master aligned the132 SDK
inputs to that preserved checkout after checking exact canonical equivalence,
verified that the canonical index tree stayed unchanged and clean, and reran the
SDK/Lab gates above. All107 overlapping SDK/Unity paths now match exact raw bytes
and hashes. The other25 SDK inputs are explicitly outside the default Unity roots.
`SOURCE_REPRESENTATION_LEDGER.csv` records all132 paths, prior/final hashes,
Unity overlap scope, canonical source blob and10 EOL-only alignment rows. It
changes no simulation code, fixture values, validation tool rule or original report.
Actual assembly identities in the fresh package remain truthful; no MVID is masked.
