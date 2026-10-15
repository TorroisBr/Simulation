# CORE-F WP2-Persistence — implementation validation

## Current final validation — 2026-10-15

Final tested production/test source: `50976a358e3563be3dcd37404ccd9d511d6855ff`.
Tree: `2a3d95cec8b72df29939d03693caa3de522256f9`; Assets:
`cf38591739b1a2f017ca1b2d79e6243697645b16`; Packages:
`08a12ac6d1adf71582341714b19e5e5c104c8fa6`. ProjectSettings and both
validation-tool trees remain the identities recorded in the historical section below.

The final independent source audit identified the combined foreign-host/unknown-
profile case. One structural canonical parser now precedes admission policy:
malformed framing/UTF-8/mandatory version zero is Corrupt; Unity rejects every
structurally canonical v2 as UnsupportedSchema; the pure .NET policy rejects a
canonical foreign Unity host as IncompatibleBuildOrRuntime before profile/version
admission. Native-host unknown profile and positive unsupported profile version
retain UnsupportedProfile and UnsupportedSchema respectively. The public v2
canonical parser retains its supported-profile contract. The added 2x2x2
host/profile/version regression also tests malformed data for each combination.

| Final gate | Protected run | Result |
|---|---|---|
| .NET pinned cold/warm, Library absent | `e96eed452ca149b78e4e6024e8e18e2b` | 153/153 each |
| Locked Lab cold/warm | `5048deb8e044446a8b5079d4f0806fac` | PASS, exact four-line scenario |
| Unity focused codec/shared persistence/admission | `2362aac6728f4432ac30196ebbd20009` | 269/269 |
| ALL EditMode | `11094e4768424ff9a2e07e718774d14a` | 3163/3163, no nonpassing cases |
| Official -TestFilter Smoke | `36bb6127cff2482793934160691b351c` | 5/5 |
| Original LongRun cases in ALL | same ALL | 7/7, unchanged 100/1000/5000-day horizons |
| Base-to-tip diff --check | final source above | PASS |

New complete result packages supersede the earlier result packages for final
source applicability; historical packages and immutable originals remain intact.

- `core-f-persistence-taxonomy-cold-warm.manifest.json` and matching ZIP:
  archive SHA-256 `1a7c318676284f85421c5659328437ade89a1e98d5f5984b76a7a6d2b6210ba2`.
  Private comparison: originalComparisonVerified. Separate archive-only check:
  originalComparisonNotPerformed. SDK/project/compiled-source identity is bound
  by the TRX package, including inputs outside Unity capture roots.
- `core-f-persistence-taxonomy-all.manifest.json` and matching ZIP:
  archive SHA-256 `8f1fcfa134a9fe86d77c2c7134ece309ac1fbdc63eae084e0eb3adca88c21601`.
  Package verification against the protected run manifest passed.

All three Unity runs captured identical input inventories, stable before/after,
from the same source SHA. Every focused and Smoke case was joined to a passing
case in the complete ALL report. Original focused XML SHA-256:
`2f7d14093e168444685de0cc93ce5d507c927b909fd0a2c0337702e38eea5d60`;
Smoke XML SHA-256:
`41b4facbde2ba1b33758d30915a9de0f7d247b01fd742de1182be2ec460871b7`.
The filtered-suite packaging limitation described below still applies; no report
was rewritten. Public packages contain complete normalized results with private
metadata removed by the approved tool, not private binaries/log locators.

The same infrastructure-only exclusions below apply. Final exact-tip independent
review is recorded separately before promotion. Documentation-only successors
reuse these runs only after verification that all effective inputs are unchanged.

## Historical validation at 3d0ed689 (superseded for final source)

## Candidate and exact executed source

Production base: `8ea3b47e9ab1be9ab3fe6261a73dc33fece8cd8f`.
Approved architecture: `4da844dee0c69fdf66562590fa96cc728fc86bf7`.
Contract: `docs/architecture/CORE_F_PREREQUISITE_MASTER_HANDOFF.md` section 4,
with parent host design section 4.3. This package implements no owner/runtime host.

Final tested source commit: `3d0ed689d90810045c76391f72dab64fad59df6d`.
Tree: `8ebe9013ba80aa5987cca362994071300c27a9a6`.
Assets: `4978a46b45064f3c19e970db1b45393daf97d0d3`.
Packages: `e548292a8cb82515af2d111d510f0afa8d8d14c6`.
ProjectSettings: `db5c1636993b7f82f2409e07300df2744a5dfe85`.
UnityValidation: `965c46eef5d172f96cec6a17fd39bdc18ffde1f0`.
CoreValidation: `60d538d4e177c081e904f240f24ad6d665d8054f`.

Author code commits are `bfdc14f037f78ae7592bff23f6501462e4a0a7d6`,
`703cf1dae4d39c13ff788ca168e8daf56394239d` and
`7786196153ae29a788141a37a26180f67bf254df`. They were cherry-picked onto
an additive clean branch from the Core base, excluding local checkout-recovery
ancestry. Root integrated the explicit input inventory and two reviewed
compatibility/regression corrections in subsequent commits.
Only three approved prerequisite documents were imported from architecture;
its executable tree was not merged. Three missing existing-script metadata files
from a clean Unity import are separately identified additive integration inputs.
No numbered-phase canonical, active unrelated checkout or protected original changed.

## Implemented boundary

- The existing descriptor-v1 value/parser/writer and result-code enum now compile
  once in the engine-free Core; wire framing, limits and equality have one shared
  implementation. The internal P12AWire forwarding adapter remains in Unity.
- Typed immutable v2 descriptor values implement the accepted field order,
  normalization, canonical validation, host/profile/runtime/content taxonomy and
  foreign-host rejection. Environment discovery and real host anchor inventory
  remain later host-composition work. The numeric probe/hash helper is pure.
- Unity emits/accepts v1 only. Actual codec header admission recognizes -2 and
  rejects canonical v2 as UnsupportedSchema before owner staging. Malformed v2 is
  corruption. A canonical unknown v2 profile retains UnsupportedProfile for the
  pure/.NET policy, but the v1-only Unity policy rejects that variant as schema.
  Mandatory nonpositive versions are malformed; positive unknown schemas are
  UnsupportedSchema. No fallback to v1 follows the -2 discriminator.
- One Core atomic byte-slot kernel owns IO. The Unity wrapper retains slot policy,
  original decode-before-IO, result/signature behavior and Application conveniences.
  No owner/image/census DTO was exposed as a public factual API; no friend grant
  or second simulation authority was added.
- Core targets netstandard2.1/C#9; the SDK project now links 19 production sources.
  CoreValidation's 91-file input inventory includes every explicitly compiled
  production/shared-test source and new metadata. The project-link coverage check
  and existing CoreValidation integrity/ACL/runtime-selection self-tests passed.

## Required executed gates

| Gate | Run | Result |
|---|---|---|
| .NET cold/warm, pinned SDK 9.0.315, net8.0, selected CLR 8.0.28 | `cfdc8d72d6ba4d1e84015cfcbcebc48c` | 152/152 each; zero nonpassing cases; stable inputs; no Library |
| Locked standalone Lab | `d46ba4fa4a98408db087ef95b356d7f7` | PASS; exact four-line cold/warm scenario output |
| Unity P12A codec, shared persistence and RuntimeAdmission | `043d950a089540f2a2ebef33b454973b` | 268/268; stable inputs |
| ALL EditMode | `044726ab3032457a815511db4729c391` | 3162/3162; zero failed/skipped/inconclusive; stable inputs |
| Official EditMode -TestFilter Smoke | `3c1f7357e05a450c91413cf77e4d9b74` | 5/5; stable inputs |
| Existing SimulationRuntimeLongRunTests in ALL | same ALL run | 7/7, including unchanged 100/1000/5000-day cases |
| Full base-to-candidate git diff --check | final source tip above | PASS |

The original 125 .NET cases remain; 27 shared persistence cases were added. Unity
adds those shared cases plus three codec/store integration regressions. New cases
cover the independent v1 canonical byte vector, v2 field order/malformed/canonical/
normalization/bounds/host/profile/mismatch cases, atomic fault ordering and exact
slot/temp preservation, and real Unity codec/save rejection of v2 frames. Existing
storage, startup, persisted restore and continuation cases remain passing.
The pre-extraction production reference was independently run at the original
Core source with Values/Owners characterization tests only; its ALL 3134 report
and existing v1 codec/storage tests are retained on the prerequisite baseline
branch. The newly pinned v1 vector is derived from the original wire layout;
no claim is made that the later-added shared fixture itself ran before movement.

## Complete retrievable result packages

.NET package: `core-f-persistence-cold-warm.manifest.json` and matching ZIP.
ZIP SHA-256: `3e77d1759dd5b881c2f3a61b87eb17437a1f51c7d93d7672bca543459ca5de74`.
Private-context verification: originalComparisonVerified. Separate archive-only
verification: originalComparisonNotPerformed, as required without source context.
Input digest: `8e62b2b674194b2e5a67fdf18b59a51766dbadf138add86e60f1b528811b05e2`.
The paired .NET manifest binds SDK/project/shared-source inputs outside Unity's
four default capture roots; both hosts executed the same committed package/test
sources identified above. Capture-time and packaging-tool identities are distinct
fields; the evidence tool implementation was unchanged.

Unity package: `core-f-persistence-all.manifest.json` and matching ZIP.
ZIP SHA-256: `034462c5e6e4ff6f0e692cd62662d9f1715b1a7babece1396868faedb125f8cb`.
Packaging and private original comparison passed. Its complete report includes
every focused and official Smoke case. ALL and Smoke captured identical source
file inventories; all five Smoke identities were independently joined to passing
ALL cases. Separate Smoke original XML SHA-256:
`18717bc7c13de4a306a64be743523bdaae3afee808ffbbfc2f0174a45a286f10`.
No separate filtered-report package is claimed: the existing packager rejects
Unity's filtered-suite testcasecount inconsistency. It is not waived or rewritten;
the complete ALL package supplies durable retrievable results. Raw reports/logs,
runner manifests, traces, matched .NET DLL/PDB copies and retention receipts remain
unchanged in verified owner-only storage; no binary/private locator is published.

## Review corrections and diagnostic runs

Independent source review requested the completed fault matrix and actual Unity
codec/store v2 rejection, then verified the added coverage. Root also found and
corrected an initially misplaced internal Core wire adapter, path-resolution catch
scope, a retained brace in the mixed Unity file, and a Unity-incompatible test
formatting API. The final correction preserves the exact legacy combined-argument
size-rejection precedence, with a regression test. Those earlier failing/diagnostic
runs remain retained; they are not promotion evidence for the final source tip.
Final exact-tip independent implementation/evidence review is required separately.

## Limits and readiness

This is the shared persistence foundation only. No actual .NET world descriptor,
world save/restore host, live owner closure, runtime extraction, cross-host save
compatibility, P12 profile expansion, historical fork, P13 or CORE-X readiness is
claimed. Unity v1 same-build/current-host behavior remains the supported contract.
Full CORE-F remains WAIT_DEPENDENCY. Values' canonical promotion consumes this
package only after its own canonical promotion and remaining Values gates.
