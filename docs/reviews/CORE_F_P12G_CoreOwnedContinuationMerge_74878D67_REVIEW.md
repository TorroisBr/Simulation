# CORE-F Core-Owned P12 Continuation Merge — Exact-Tip Review

**Verdict:** `VALIDATED_CANDIDATE` — PASS for the bounded four-file implementation delta below. This is an independent code-review record only. It is not a canonical promotion, a CORE-F completion claim, or approval to promote.

## Exact identities

- Candidate branch: `codex/core/CORE-F-restore-stage-transaction`
- Candidate commit: `74878d67ece6880989b0a50c84f82fe86ed0a254`
- Candidate tree: `d56c6d2ae1c4b4e2077e6418fc7d93b7bbd48926`
- Exact parent/base: `90e8ec927bfe308ad43f7b493b7fab734e9d9ef7`
- Core canonical observed remotely during review: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Candidate remote ref, freshly queried before recording: `74878d67ece6880989b0a50c84f82fe86ed0a254`
- Review date: 2026-11-05

The candidate commit was checked out and its full diff against the exact parent was reviewed. The review record is on a separate branch whose parent is the candidate commit. The branch adds this record only.

## Scope reviewed

Only these four changed files were reviewed:

- `Assets/_Project/Scripts/P12GDailyV1RestoreCoordinator.cs`
- `Assets/_Project/Tests/EditMode/Editor/SimulationRuntimeAdmissionTests.cs`
- `Packages/com.simulation.core/Runtime/Application/DailyV1Application.cs`
- `Packages/com.simulation.core/Runtime/Persistence/DailyV1P12GContinuationSourceWitness.cs`

The change composes P12-D/E/F sections from the active Core continuation-save operation, checks and replaces matching Unity-encoded Core-owned sections, rejects conflicting overlaps, and keeps the existing Unity compatibility route for an incomplete Core NPC roster.

## Review findings

No actionable correctness findings remain for this delta.

The Core path verifies the live save operation and its application/session ownership, then encodes P12-D/E/F from the operation-bound sources. It rejects invalid or duplicate section IDs. For every overlapping section, it compares section ID, schema version, role, alias group, section revision, row count and order, row owner key, row revision, and payload bytes. Equal overlaps are omitted from the adapter list and the Core-owned section is added once; conflicting overlaps fail with `OwnerGraphRejected` before image composition.

The resulting section list is passed through the existing image composer, which validates the exact owner census, required sections, exact-empty sections, duplicate IDs, and manifest. The save operation is revalidated during Core encoding and immediately before and after image composition. Failed composition returns no image and does not mutate authoritative owners or publish a slot.

The Unity coordinator selects this path only when the operation has captured Core NPC owner sources. That capture requires the Core runtime-ID set to exactly equal the admitted NPC roster and validates all 22 per-NPC census families. The existing whole-roster Unity compatibility route remains in place for the explicitly supported incomplete/mixed-roster case; other capture failures continue to reject. The test change asserts the complete Core NPC vector is present on the selected restored-session path.

## Validation evidence applicability

The validation ran against the parent commit plus the candidate's then-uncommitted code changes. I compared the manifest input hashes for all four changed source files with the exact checked-out candidate content; all match. Core source input hashes for `DailyV1Application.cs` and `DailyV1P12GContinuationSourceWitness.cs` also match. Each relevant manifest reports stable inputs.

- CoreValidation run `ca925641729d47bcabc3200d1f80918b`: cold 1017/1017 and warm 1017/1017; stable input digest `e819ec319e9845c08b9bead7ad7f1842cb481caa4ca3a80a4c70cea4e0e8518b`. Manifest SHA-256: `e7af39d602042252060eab84dcf4c42fa8d6386c39d0946c7a67b812b0fc55c8`.
- Focused Unity restore run `6859c3519cde47798d6612346be970f9`: 1/1 passed; stable before/after input digest `f9db13cee446b27758d2df86994bcdd105f774369ca034b8f4bb57834a96bdd0`. Run-manifest SHA-256: `070ef1cf47216a3fe43bd5aeeacfa8b202ca78afdea57306971e70bce5180169`.
- Full Unity EditMode run `9da0e1a89af746e1aa5b79dca81fc5c4`: 3299/3299 passed; stable before/after input digest matches the focused run. Run-manifest SHA-256: `26e09c7aaa4db69c515c4581ccf4ac21c289a4c77cdcea0f93e7da6e0a055ec4`.
- Full EditMode normalized evidence package: `core-owned-full-editmode-20261105.zip`, SHA-256 `fa016c4efec842e3775b3c53e4371ca9e695c82e09801ab98deceb0024ab90da`.
- Official Unity Smoke run `d98c931ee8454c34ac19f85b61d05b6a`: 5/5 passed; stable inputs. Run-manifest SHA-256: `d3d38bbcfef1763ef8e212d56d0691f732e194f3e3960fa6edfc3dfdd34fd238`.

Private evidence remains in the E: worktree and is not included in this review-record branch. The earlier direct-only attempt failed; it is not part of this candidate's commit history or the passing evidence above. The corrected merged composition is the reviewed and validated implementation.

## Coverage limitation and integration constraints

The supplied focused and full runs exercise the successful exact Core-section composition and the existing mixed-roster compatibility behavior. The delta does not add a dedicated negative test that deliberately alters an overlapping adapter section and asserts conflict rejection for each compared field. The comparison logic was reviewed directly and is exhaustive over the encoded section value. This is a non-blocking test-coverage limitation for this candidate.

This verdict applies only to the exact candidate SHA/tree and this composition boundary. It does not establish P12-A readiness, P12-B completion, complete CORE-F integration, canonical promotion readiness, or Phase closure. Do not promote without the separate current-canonical, integration, and promotion preflight.
