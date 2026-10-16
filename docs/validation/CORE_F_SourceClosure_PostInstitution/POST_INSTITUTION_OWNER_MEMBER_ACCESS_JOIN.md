# CORE-F post-Institution owner and member/access join

Status: **PARTIAL SOURCE JOIN — NOT READY FOR IMPLEMENTATION**.

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
