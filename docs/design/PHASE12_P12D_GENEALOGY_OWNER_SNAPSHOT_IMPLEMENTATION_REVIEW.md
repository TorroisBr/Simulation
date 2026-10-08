# P12-D Genealogy owner snapshot — implementation review

**Verdict: VALIDATED_CANDIDATE**

## Review boundary

- Current P12 canonical base: `0e786db8e6ed5ed937ff62e3f63258d8b73fd93c`.
- Architecture baseline: `47eff220c7ce00f6e7c759bdc2b76780bb46f628`.
- Candidate branch: `codex/phase12/P12DECurrentRevalidation`.
- Exact candidate tip reviewed: `aa2f4044e533cb8cd9f60f8672fef4bfb47b5c13`.
- Code commit: `2b3de7cd13e6f35d35d5dece56eefb64c1fd972a`.
- Code tree: `a2fe8033298b96a68b8e209cce6d605dbbe6ca44`.
- `Assets` tree: `127bc9dc10c0e69312ae98748f04becbe2d8ceda`.
- Exact-tip validation record: [`P12DGenealogyOwnerSnapshot/VALIDATION.md`](../validation/P12DGenealogyOwnerSnapshot/VALIDATION.md).
- Independent reviewer: `/root/p12_de_implementation_boundary`.

At final preflight, `origin/codex/phase12/canonical` remained the stated base and the candidate remained an ancestor-descendant fast-forward. The candidate's `Assets` tree exactly matches the requested tree. The base-to-code diff changes only `GenealogyContracts.cs`, `GenealogyStore.cs`, and `GenealogyFoundationTests.cs` under `Assets`; the remaining changes are bounded D/E design and inventory documentation. The code-to-reviewed-tip delta consists only of archived validation XML/logs and `VALIDATION.md`. No `PersonStore`, runtime, bootstrap, or gameplay code changed. The unrelated dirty ProjectSettings and untracked `.meta`/validation files in other worktrees were not touched.

## Implementation findings

- Schema version is explicitly `1`, including an empty edge list. The owner snapshot contains only directed `ParentId`/`ChildId` edges and the exact local `GenealogyStore.Revision`; it does not mint an owner/runtime identity or capture token. `PersonId` values are copied without normalization, and ordering is deterministic by ordinal parent ID then child ID.
- Export is detached: the row collection is a private `ReadOnlyCollection`, each valid `ParentageRecord` is copied, and its `PersonId` values are immutable. Later store writes cannot alter the snapshot.
- The internal staged factory validates schema, nonnegative revision, a non-null row collection, and the necessary `Revision >= edge count` invariant. It preserves revision gaps and `long.MaxValue` exactly. A restored saturated store rejects a subsequent ordinary write with `RevisionOverflow` and leaves its rows/revision unchanged.
- The factory builds only a local unpublished store and its derived adjacency indexes. Null rows, null endpoints (defensive against malformed internal data), self edges, duplicate directed edges, and cycles are rejected before `stagedStore` is assigned. The source store is never mutated, and invalid staging returns no store.
- Edge uniqueness is exact `PersonId` pair equality. The implementation does not impose a one-parent/one-child restriction; multiple parents, multiple children, and acyclic diamond graphs remain valid as allowed by the existing domain contract.
- Tests cover empty/populated graphs, detachment/read-only rows, revision gaps, saturated revision, unsupported schema, impossible revision, null collection/row, duplicate edges, cycles, and source preservation. The constructor checks self edges; public domain mutation already rejects self-parentage. `PersonStore` endpoint membership is deliberately deferred to the merged P12-D graph stage, as required by the design; no dangling-endpoint or cross-owner capture test is claimed here.

The snapshot methods are internal owner primitives with a documented caller precondition: the caller must hold the P12-B completed-boundary capture authority. They do not themselves validate a capture token, owner-thread/quiescence window, or owner-section revision vector. This is acceptable for this isolated owner slice only; the later D adapter must enforce those P12-B conditions and must reject dangling endpoints after Persons are staged.

## Validation evidence

The retained XML summaries independently parse as:

- `GenealogyFoundationTests`: 24/24 PASS.
- All Genealogy-matching tests: 52/52 PASS.
- `PersonNamedBirthLifecycleTests`: 15/15 PASS.
- ALL EditMode: 2540/2540 PASS.
- Official Smoke: 5/5 PASS.
- `git diff --check` on the exact base-to-code diff: PASS.

All successful XMLs and the initial documented 23/24 diagnostic XML hash to the recorded manifest values when represented with CRLF checkout bytes; their committed Git blobs are normalized LF under the repository's `core.autocrlf=true`. Each retained gzip log's compressed and decompressed SHA-256 matches the manifest. The XML suite/fixture identities match the claimed focused filters.

The Unity logs identify the `p12de-current-revalidation` candidate worktree. The successful runs ended at 12:39:37Z and the code commit timestamp is 12:40:05Z (28 seconds later); the logs do not embed a Git SHA. The validation manifest records code commit `2b3de7c` and `Assets` tree `127bc9d`, and the submitted code commit contains that exact tree with no later executable changes. This is accepted as validation of the candidate working-tree content; no post-commit rerun was performed during review.

## Scope and integration limits

This validates only the Genealogy schema-v1 owner snapshot and exact private local factory. It does not deliver the rest of P12-D, implement Person or NPC export/hydration, validate Person membership, integrate the profile envelope/capture-token vector, bind the staged store into `SimulationRuntime`, or prove whole-profile atomic publication/parity. P12-D remains open; P12-A remains `WAIT_DEPENDENCY`; no P12 or P13 readiness is inferred. Canonical promotion is a separate refreshed operation.
