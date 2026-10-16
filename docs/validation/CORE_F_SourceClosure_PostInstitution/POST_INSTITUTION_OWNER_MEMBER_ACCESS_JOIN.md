# CORE-F post-Institution owner and member/access join

Status: **CURRENT OWNER INSTANCE JOIN VERIFIED; SOURCE/ACCESS CUT AND
NON-WRITER DISPOSITIONS REMAIN OPEN — NOT READY FOR IMPLEMENTATION**.

This read-only audit binds the existing Daily-v1 owner and operation records to
the promoted source tree. It does not change production or test code, prove
behavioral completeness, or authorize shared runtime/host implementation.

## Initial join inputs (historical)

- Core canonical at audit: `33f41329c1ca5eb7aa60f276bc08319a55a71884`.
- Architecture canonical: `797eb02f06fc5ca759a7e47283589d97a5efe73d`.
- Production source/test identity: `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`,
  tree `e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`.
- Inputs: the promoted 89-row `OWNER_WRITER_MATRIX.csv`, the 89-row
  `OWNER_CAPTURE_TARGET_CROSSWALK.csv`, the 24-row `OPERATION_CASE_MAP.csv`,
  the post-Institution Roslyn source inventory and symbol-edge tables, and the
  exact Core compile-input inventory.

## Mechanical join results

`owner-operation-current-source-refs.csv` retains 1,660 field-qualified C#
source references from the 89 owner-writer rows, 89 capture/target rows, and 24
operation rows. They resolve to 142 unique repo-relative paths and exact Git
blobs at `ca55c3f`; none is missing. The references classify as 939 Unity
`Assembly-CSharp` source references and 721 test-source references. None of
these existing matrix references directly names a current `Simulation.Core`
compile input.

`selected-core-member-access.csv` is the closure-filtered projection of the
retained Roslyn semantic symbol edges. It contains 2,582 exact `Simulation.Core`
symbol references in the 289-file restore closure; 1,928 of those are in the
220-file runtime closure. The retained edge set has 833 distinct Core symbols
for restore and 752 for runtime. `selected-unity-engine-access.csv` similarly
projects the 288 Unity-engine symbol references in the restore closure (243 in
runtime). The existing post-Institution compiler graph has 315 Unity source
files, 53 Core compile inputs, 116 SCCs, 220 runtime-reachable sources, and 289
restore-reachable sources.

The generator is
[`PostInstitutionJoin.py`](audit-source/PostInstitutionJoin.py). It consumes
the retained graph and owner matrices, projects only repo-relative paths and
symbol identities, and fails if a cited source blob is unresolved or the
selected Unity-symbol projection disagrees with its retained edge table.

SHA-256 identities:

- `owner-operation-current-source-refs.csv` —
  `4d8a064e894e95e1e57a71da44d183a8adf066a785ae52a3ef805c0568cdde63`.
- `selected-core-member-access.csv` —
  `0b2d2d9dc30033684ba7d5263b8b80d676556175eb5ed163544b1e6c0cb06917`.
- `selected-unity-engine-access.csv` —
  `f3034828c80fac73202ccc4f1d22d0e758ee2d9ff5b8431ad3f7a1d762ce5a5f`.
- `owner-current-source-anchors.csv` —
  `97c1a6ed140798a46230254ba7f63a9e219dbb7558f7068acae234433d5e4533`.
- `PostInstitutionJoin.py` —
  `a83cdb2dfd7d8cb4d0206db66c66c69071c19ffb0ab355c63b87ccc7291a7351`. Its
  exact Unity projection guard compares the full `(source_path, unity_symbol)`
  multiset. A focused positive check and
  an equal-row-count symbol-substitution rejection both pass.

## Remaining exact blocker

The join establishes that the old owner/capture/operation citations still
resolve to files in the promoted code tree. It does **not** bind each of the 89
families to a complete current chain of:

`family → concrete owner type/member and constructor → exact live owner instance
→ supported writer and revision/epoch callback → capture/codec → staged target
factory/hydrator → target validation → applicable case identity`.

In particular, the matrix path references are primarily P12 providers, codecs,
restore entry points, and tests; they do not directly identify the post-migration
declarations/members for every installed owner. The 2,582 Core symbol edges are
exact selected-closure references but are not yet assigned to the owner family,
constructor, or operation that requires them. Unity-engine symbols are listed
but their per-member adapter/split disposition is not closed. File existence,
line-range survival, or SCC membership alone is not a semantic join.

Therefore the required selected owner source/member/access cut is still
unproven. `CORE-F-Shared-DailyV1-Application-and-Host` remains
`WAIT_DEPENDENCY`; this audit does not authorize implementation, source
movement, or API widening. The next technical task is to bind each owner and
operation row to exact current declarations, members, constructors, live
instances and call paths, then independently review that bounded ledger against
the approved access rules in `CORE_F_RUNTIME_RESTORE_INTEGRATION_DESIGN.md` §14.

For that initial documentation-only source join, no tests were rerun because
the source/test tree was unchanged. Existing implementation validation at
`ca55c3f` remained applicable at that point; the later test-only revalidation
is recorded below.

## 2026-10-16 current-source closure audit

The current Core canonical tip is `1998974515b6357c308d861fb608f2db5071a63a`.
The production and test source tree is unchanged from code-bearing commit
`ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d` (tree
`e0d3de22bdc7bcc16e16a925f6f8c38f6a6397a5`). Thus retained Unity validation
remains applicable to this audit; the documentation-only commits after
`ca55c3f` do not require a Unity rerun.

The source-join generator now performs executable closure checks before it
emits current references. It requires the authoritative Daily-v1 family
inventory, owner-writer matrix, and capture/target crosswalk to have the same
89 unique keys; the operation map to contain its 24 unique operation IDs; each
owner/operation reference and compiler inventory row to resolve to its exact
source blob; and the selected-profile test to invoke the live-owner identity
assertion. That test compares all selected registered section IDs with the
independently built owner map, then checks each provider's
`OwnerInstanceIdentity` is the exact expected runtime object.

Exact result from the original source-anchor generation at `ca55c3f`:

| Check | Result |
| --- | ---: |
| Owner-family inventory / writer rows / capture-target rows | 89 / 89 / 89 exact keys |
| Sealed operation IDs | 24 unique |
| Source references across the three matrices | 1,660 references; 142 unique paths; 0 unresolved |
| Selected-profile live owner-identity assertion | PASS |
| Unity compiler graph | 315 Assembly-CSharp sources; 53 Core compile inputs; 220 runtime and 289 restore sources |
| Selected Core symbol references | 2,582 across 171 source files; 830 distinct resolved symbols; 752 distinct runtime symbols |
| Selected Unity symbol references | 288 across 67 source files; 243 runtime rows across 57 files; 45 restore-only rows across 10 files; 37 distinct symbols |

The earlier prose count of 833 distinct Core symbols was stale; the generated
current table has 830 unique `resolved_symbol` values. Exact current owner
schema line/blob anchors for all 89 families are emitted to
[`owner-current-source-anchors.csv`](owner-current-source-anchors.csv).
Specific stale or incorrect joins are reconciled in
[`OWNER_CURRENT_JOIN_CORRECTIONS.md`](OWNER_CURRENT_JOIN_CORRECTIONS.md). The
original validation matrices remain unchanged as historical evidence.

### Owner and writer disposition

The selected-profile executable identity map closes the family → provider →
live owner instance binding for every registered row, including fixed,
per-NPC, unbound-residence, per-Person, and per-City rows. The live selected
profile uses the accepted `61 + 22*N + U + P + 4*C` census formula; for the
recorded initial values `N=10, U=10, P=0, C=2`, the exact inventory is 299
sections. The test distinguishes aliases by object identity. The current
source map therefore closes composition-to-instance identity as observed
through the real selected bootstrap; it does not turn a test oracle into
.NET runtime evidence.

The writer/epoch ledger and sealed operation map identify the supported
Daily-v1 mutation paths and their actual changed-owner notifications. In
particular, the existing P12B membership, market, material, travel, allocator,
sequence, institution/claim/property, and supported Knowledge/Travel writes
are retained only at their mapped operation boundaries. P8-B through P8-D
rows remain exact-zero `ExplicitlyEmpty` rows where the profile contract says
so; no producer is added. Exact-zero Required owners remain Required and are
not silently recast as ExplicitlyEmpty.

These 18 Required families still have no normal selected Daily-v1 writer in
the current matrix, so they are not claimed as writer/epoch-closed:

- P16/P17-gated armed-force owners:
  `p12e.armed-force.{forces,contingents,relevant-person-references}`,
  `p12e.contingent-manpower.states`, `p12e.armed-force-spatial.positions`,
  `p12e.conflicts`, `p12e.wars`, and `p12e.battles` (8).
- Required political owners with no ordinary Daily-v1 producer:
  `p12f.political-knowledge.holders` and `p12f.political-decisions.records`
  (2); populated P12-G synthetic restore evidence is not a normal writer.
- Required per-NPC site/topology/adventure knowledge views without a selected
  producer: `p12f.explorable-site-knowledge/`, both
  `p12f.local-topology-knowledge.*` views, and the four
  `p12f.adventure-intel.*` views (7).
- Required `p12f.expeditions` has no normal selected-profile producer. Its
  current contract explicitly says to preserve payload and not impose a hard
  zero or invent an operation (1).

Their providers, revisions, census rows, codecs, and staged restore paths are
present in the matrices and source join, but these facts do not satisfy the
requested writer-or-approved-exact-empty edge. No normal writer, epoch callback,
or exact-empty disposition is invented here. This is a scope/disposition gate,
not evidence that deferred gameplay producers should be implemented.

### Exact selected compiler/member access graph

The original file/symbol projection is now augmented by a Roslyn call-site
inventory generated by `audit-source/MemberAccessCut.cs` from the current
`Assembly-CSharp.rsp`. The response SHA-256 exactly matches the preserved
compiler-response identity; all 315 current Unity compile-source blobs match
the pinned source inventory before semantic binding. Its rows are repo-relative
and contain the source blob/SCC, runtime/restore closure, line and source span,
enclosing caller type/member/accessibility, referenced symbol/kind/accessibility,
target assembly/namespace, and in-source declaration file/line where available.
No source text or absolute workstation path is emitted.

The result has 42,625 selected call-site rows: 39,305 `Simulation.Core`
references in 171 source files and 3,320 `UnityEngine` references in 67 source
files. The existing exact symbol projections reconcile without loss or extra
pairs: 2,582/2,582 Core pairs and 288/288 Unity pairs. The call-site rows split
into 32,810 runtime rows and 9,815 restore-only rows. The existing selected
file plan classifies 222 files `CoreMove` and 67 `SplitHostDomain`; this is a
bounded candidate closure, not an instruction to move the largest 190-file
SCC wholesale.

The repo-relative call-site table is preserved as
[`selected-member-access-sites.csv.gz`](selected-member-access-sites.csv.gz).
Its manifest records the compiler response hash, exact source tree, compressed
and uncompressed artifact hashes, and pair reconciliation. The 19.3 MB CSV
compresses to 416,359 bytes; decompression reproduces the recorded uncompressed
SHA-256 exactly. Re-running the analyzer produces the same archive hash.

The high-collision sources are now measurable. `TesteSimulacao.cs` has 528
Core and 357 Unity member references; `SimulationRuntime.cs` has 3,649 Core
and 55 Unity references; `CityRuntime.cs` has 533 Core and 222 Unity
references; `NpcRuntime.cs` has 108 Core and 151 Unity references. This
identifies concrete host-crossing members while preserving the design's §14
same-assembly rule for semantic implementation and its one public
`DailyV1Application` boundary. Existing Core cross-assembly references are
public in the current Unity assembly; the move may keep semantic implementation
members internal once callers move with them, and must not widen the host API.

### Remaining exact owner/access edges

The new call-site map closes *where* selected Core/Unity symbols are used, but
does not decide the approved destination for each reference in the 67 split
files. Each selected caller still needs one explicit §14 disposition: shared
same-assembly Core member, thin Unity adapter call through `DailyV1Application`,
or an already-approved host-neutral helper for Unity null/numeric behavior.
The 89-row family/capture crosswalk and live identity test also do not yet give
one current-source row per family for the concrete declaration/member,
constructor parameter and initialization path, writer callback and its
revision/epoch notification, and exact capture/stage/hydrator target. The
current `owner-current-source-anchors.csv` anchors continuation-image section
keys, not each concrete runtime owner declaration. The 24 operation records
resolve all their cited paths/blobs, but their complete per-owner mutation
method/notification joins are not thereby proven.

The typed-definition gap is a separate code edge. The genesis fingerprint path
calls `DailyV1DefinitionAdapter.TryCreate` and consumes its
`DailyV1EffectiveDefinitions` for identity/fingerprinting. The selected startup
then constructs `SimulationRuntime` from the Unity-side `CityRuntimeList`,
`NpcRuntimeList`, action data and configuration; it does not pass that
canonical definition graph to runtime/owner construction. `CityRuntime` and
`NpcRuntime` still contain Unity authoring/serialization behavior. The
approved design already defines the destination and does not require another
architecture redesign, but this constructor-binding source change remains
implementation work after the exact per-family cut is closed.

Shared runtime/restore therefore remains **`WAIT_DEPENDENCY`**, not
`READY_FOR_IMPLEMENTATION`. Remaining evidence work is bounded to joining all
89 families to current type/member/constructor and P12 capture/restore targets;
assigning the selected callsites to the already-approved §14 boundary; and
reconciling each no-writer row against the selected-profile baseline contract.
The 17 Required no-normal-writer rows have executable initial exact-zero
evidence, but that alone does not decide whether they satisfy the entry
criterion while preserving Required role and nonzero restore semantics. The
Required Expedition row remains preserve-only, without a hard-zero claim. No
gameplay producer, owner-role change, broad API, or architecture redesign is
proposed.

### Selected-profile Required Knowledge zero-baseline revalidation

The independent source review identified that the selected authored-profile
test checked provider identity and matched census values to owner values for
seven Required per-NPC Knowledge rows, but did not independently assert their
initial cardinality and revision. Added a bounded admission-boundary assertion
to `SimulationBootstrapCompositionTests.SelectedDailyV1ProfileBootstrapsItsAuthoredP8GeographyBeforeDayOne`:

- `p12f.explorable-site-knowledge/`;
- `p12f.local-topology-knowledge.places/` and
  `p12f.local-topology-knowledge.connections/`;
- `p12f.adventure-intel.opposition/`, `notable-items/`, `common-resources/`,
  and `access/`.

For each selected-profile NPC, both the source owner and registered witness
must have cardinality zero and revision zero at initial admission. These rows
remain `Required`; this is not an `ExplicitlyEmpty` reclassification, a hard-zero
restore rule, or evidence of a writer/epoch path after admission. Restored and
later runtime states may carry nonzero values.

The assertion checker in `audit-source/PostInstitutionJoin.py` now fails unless
all seven family prefixes, the cardinality/revision assertions, and the
initial-profile-only boundary are present. Its exact source is
`cb31d4d6f777a78af25670e3e6d43f01578b8809`; the production implementation
remains unchanged at `ca55c3f9fa4d9c99e6a47a25adbbd1956ea04a1d`. The checker
reports `89/89/89` owner keys, 24 operations, 1,660 references with zero
unresolved blobs, live-owner identity PASS, and selected Required Knowledge
zero-baseline PASS.

Validation for the changed test bytes is bound by their SHA-256 and stable
runner snapshots. The focused run preceded the test-only commit, but its entire
1,666-file Unity input snapshot matches the exact candidate run byte-for-byte;
the full suite and official filtered Smoke ran at candidate
`cb31d4d6f777a78af25670e3e6d43f01578b8809` (tree
`07a5a406985f83a672007a82fe126048dd30b26d`, Unity `6000.3.9f1`):

- Focused selected-profile admission test: 1/1 PASS; original XML/log retained
  in private local evidence.
- Full EditMode: 3,179/3,179 PASS, with zero failed, skipped, or inconclusive.
  The packaged report is
  [`core-f-owner-current-join-all-editmode-20261016.zip`](Evidence/core-f-owner-current-join-all-editmode-20261016.zip),
  SHA-256 `891b12d48e9c95efcec095a45ce67d398edebf29d0b279adf84fb042067da0a3`;
  its public manifest SHA-256 is
  `7b5c1aa2150d1d90e9f0d7604d653624808c57d1563b7575e6ebe2790ce5ca1e`.
- The public package verified against its retained private run manifest. The
  complete archive contains the exact selected-profile case once and each of
  the five supported Smoke identities once, all Passed.
- Official filtered Smoke: 5/5 PASS, zero failed, skipped, or inconclusive.
  Its original filtered XML/log remain in private local evidence; the closed
  packager does not publish filtered parent-suite counts as a complete report.
- `git diff --check`: PASS. The Unity evidence input snapshot was stable.

This closes the missing initial-zero executable evidence for those seven rows;
it does not resolve the required writer/epoch or approved-disposition question.
The current count is therefore 17 Required no-normal-writer families with an
executable initial exact-zero baseline (the eight P16/P17 rows, two political
rows, and these seven Knowledge rows), plus the Required Expedition row whose
contract preserves payload and forbids a hard-zero claim. The unresolved
entry-condition decision is whether the existing selected-profile baseline
contract is sufficient for each no-writer row while preserving its Required
role and nonzero restore semantics. Do not infer that result from the passing
baseline assertions alone.

The source/member/access blocker is independent of this test gap. The 89-row
family and capture maps, live registered-owner identity oracle, and compiler
file/symbol projections still do not bind every family to its concrete current
owner member and constructor, every writer callback to revision/epoch, or every
Core/Unity member access to the approved same-assembly/adapter boundary. The
typed `DailyV1EffectiveDefinitions` graph is created by
`DailyV1DefinitionAdapter` from the genesis pipeline but is not consumed by the
selected `TesteSimulacao` runtime-construction call; existing `CityRuntime` and
`NpcRuntime` constructors still bind Unity authoring types. These are the
bounded remaining source-closure edges under the already-approved §14 design,
not a request for another broad architecture redesign.

### Final artifact integrity and bounded next work

The selected call-site artifact is reproducible from
`audit-source/MemberAccessCut.cs` and its .NET project file. Their SHA-256
identities are `2931dab3ea6e16fa00d95fae589c67f7f4f7a8873aab746b41b2978b94fc57f7`
and `ba5fb505ce9124c1cbc443051a2d7f06fcf940d13cee4b1639f85c8def67f41c`,
respectively. The call-site manifest SHA-256 is
`c9eaf9b89144d58618b22f2d6d8651547d9d34f63be5de8f991240dc075effad`. The
compressed artifact SHA-256 is
`2898b762de97b6134ece33f8cdad035a1aa4f30f3026852850b1913e38919e25`; its
decompressed CSV SHA-256 is
`2702d6d14e33fb7da6a56beda6cdecdbf71f1559fc951e48b2ae975536305d47`.
The final byte check reproduced 42,625 data rows and the recorded raw hash;
the privacy scan found no workstation/home path or credential-like marker.

The remaining work is bounded to one consolidated closure package under the
approved §14 design; no new general architecture model is needed:

1. Add one current-source row per each of the 89 owner families binding the
   section ID and role to concrete owner declaration/member, runtime
   field/property, constructor/provider injection, exact live identity
   assertion, supported mutation method plus revision/epoch callback (or an
   explicit no-writer disposition), capture codec, staged constructor,
   restore target validation, and applicable test identity. Reuse existing
   owner/capture/operation records and preserve current `Required` and
   `ExplicitlyEmpty` roles.
2. Assign every callsite in the 67 split files to its existing §14 destination:
   shared semantic members move together within Core; Unity-specific behavior
   stays in a thin adapter through `DailyV1Application`; only an already-
   approved host-neutral helper may own compatibility behavior. Group repeated
   calls by caller/target member but retain links to every row in the 42,625-row
   source map.
3. Bind `DailyV1EffectiveDefinitions` nodes to the selected runtime owner
   constructors and authoring objects while preserving current object
   identity. Current source shows a real code edge: genesis uses the typed
   graph for fingerprinting, while selected construction still consumes
   Unity-side `CityRuntime`/`NpcRuntime` values.
4. Close the 18 admitted Required families that currently have no normal
   selected Daily-v1 writer: eight P16/P17 armed-force/war owners, two
   political owners, seven per-NPC Knowledge views, and preserve-only
   Expedition. The approved runtime design requires a writer/epoch path for
   every admitted family; exact-zero initial evidence for 17 rows does not
   satisfy that entry condition, and Expedition has no hard-zero proof. Do not
   add an unsupported producer, hard-zero restore assertion, or owner-role
   change. These rows remain blocked until their required source capability
   exists or the architecture owner changes the accepted entry rule.

Items 1–2 are evidence work; item 3 is implementation that follows source
closure readiness; item 4 is a missing admitted capability under the current
entry rule, not an invitation to fabricate a writer. The current evidence and
reviews do not satisfy these remaining edges. Shared runtime/restore is **not
ready**, and this candidate does not authorize source extraction or claim a
promotion.
