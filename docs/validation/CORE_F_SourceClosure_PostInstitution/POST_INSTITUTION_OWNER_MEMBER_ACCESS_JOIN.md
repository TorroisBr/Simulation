# CORE-F post-Institution owner and member/access join

Status: **CURRENT OWNER INSTANCE JOIN VERIFIED; SOURCE/ACCESS CUT AND
NON-WRITER DISPOSITIONS REMAIN OPEN — NOT READY FOR IMPLEMENTATION**.

This read-only audit binds the existing Daily-v1 owner and operation records to
the promoted source tree. It does not change production or test code, prove
behavioral completeness, or authorize shared runtime/host implementation.

## Exact inputs

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
  `03cd183e9efc88882d0a86cce836d508bac73bc8f1027cd3e73a2417c47dd196`.
- `selected-core-member-access.csv` —
  `0b2d2d9dc30033684ba7d5263b8b80d676556175eb5ed163544b1e6c0cb06917`.
- `selected-unity-engine-access.csv` —
  `f3034828c80fac73202ccc4f1d22d0e758ee2d9ff5b8431ad3f7a1d762ce5a5f`.
- `PostInstitutionJoin.py` —
  `dad277efb6ea45e438c43d250537afe21a91725dc7847f7692eb357e8b3a8b4f`. Its
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

No tests were rerun because the source/test tree is unchanged and this is
documentation/evidence preparation only. Existing implementation validation
remains applicable to `ca55c3f` as recorded in its validation package.

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

Exact result from `PostInstitutionJoin.py` at source `ca55c3f`:

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

### Exact remaining compiler/access cut

The compiler graph is complete at the file/declaration edge level for the
Unity `Assembly-CSharp` response, but it does not identify the exact selected
owner/member access boundary needed to compile the Daily-v1 runtime from one
engine-free Core source set. In current code, `TesteSimulacao` builds the
runtime from `CityRuntimeList`, `NpcRuntimeList`, `NpcActionData`,
`SimulationConfigData`, and Unity-side stores. `CityRuntime` and `NpcRuntime`
retain `UnityEngine` serialization and authoring types. `DailyV1DefinitionAdapter`
already creates `DailyV1EffectiveDefinitions`, but the current startup path
does not pass that graph into `SimulationRuntime` or the owner constructors.
The typed definition binding and canonical node/reference identity join is
therefore a code/source gap, not merely a missing citation.

The exact symbol inventory at this source is available in
`selected-unity-engine-access.csv` and `selected-core-member-access.csv`. It
contains Unity API references in both adapters and semantic runtime sources,
including Unity object equality/null behavior and `Mathf` calls in runtime
owner/economy/action code. The current report identifies source and symbol,
but not each callsite/member's host adapter or shared compatibility helper; it
also does not show which public/internal access remains after the Core assembly
cut. The graph's largest SCC is 190 files; that is a compiler fact, not a
mandate to move that entire SCC. The reviewed design's same-assembly internal
cut and single public `DailyV1Application` boundary remain the authority.

Accordingly, the current owner identity and source-reference portions are
closed, but shared runtime/restore is **not** `READY_FOR_IMPLEMENTATION` under
the requested full owner/source/access criterion. The bounded remaining work
is to bind the existing 18 no-writer dispositions to an authorized profile
contract and produce the member-level selected source/access cut for typed
definition-backed runtime construction, owner constructors, Unity-specific
null/numeric/serialization members, capture/store, and restore stages. No
broader architecture redesign or additional gameplay producer is proposed.
