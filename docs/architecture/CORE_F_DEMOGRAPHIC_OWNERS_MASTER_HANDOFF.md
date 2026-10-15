# CORE-F demographic owners — Master implementation handoff

**Production base:** `0094febf7f97333d0653eaf9688fe1d24d73327f`.
**Architecture base:** `4b69e43516b3fc548611ce704ef45af9e54e3d0e`.
**Technical readiness:** READY_FOR_IMPLEMENTATION after independent design PASS and canonical architecture publication. This is one bounded prerequisite package with two owner work areas, not a new checkpoint/Phase and not full CORE-F. No production implementation occurs in this architecture workstream.

## Contract and ownership

Read the [closure execution contract](CORE_F_CLOSURE_EXECUTION_CONTRACT.md), [population source/API design](CORE_F_AGGREGATE_POPULATION_OWNER_DESIGN.md) and [genealogy source/API design](CORE_F_GENEALOGY_OWNER_DESIGN.md). Those documents fix the actual source types, bridge signatures, original writer access, snapshot/staging and rollback. Preserve exact algorithms except a separately identified, source-proven correction explicitly accepted by independent review. Do not implement an unreviewed cleanup or silently fix another legacy behavior.

Deliver one shared authoritative population ledger with receipt-backed aggregate proposals and one shared genealogy owner. Unity keeps the original City/Person/NPC/Battle/runtime orchestration and P12 census/codec/coordinator callers, adapted to the same installed Core owners. .NET directly tests those real owners. P12's two per-City population sections and single global genealogy section keep their schema, roles, aliases, values and revisions. Full owner/writer/runtime migration remains a later dependency.

The shared contract is `IAuthoritativeMutationStatus { bool CanMutate { get; } }`, implemented by the original world-owned guard object. Preserve binding identity and one-way/idempotent rules. There is no new latch, second simulation, writable fault API, global synchronization layer or IVT. Named composition bridges do not become a public factual or mod API. Capture/restore transfer immutable values and construct only unpublished owners; they cannot manufacture completed-boundary authority.

## Exact integration sequence

1. Fetch current Core, classify source changes since0094, import only this reviewed documentation delta and record its architecture SHA. Preserve active implementation worktrees/numbered canonicals.
2. One integration owner establishes the shared read-only status interface and private binding helper; Unity's concrete guard implements it without changing fault ownership. Freeze that contract for both owner workers.
3. Population worker moves its seven real sources and implements only its exact bridge/receipt DTO/opaque rollback-point and retained caller forwarding. It reuses Core-E policy and calls `StableKeyEncoding.Encode` directly in the existing fingerprint. Preserve both-unbound pair acceptance, exact same-bound-object requirement, P12 callback order/flags and rollback receipt pruning.
4. Genealogy worker moves its two real sources and immutable snapshot/stage bridge, preserves all graph rules/revision semantics and adapts official Unity composition/restore/census accesses exactly as its design specifies.
5. Workers can prepare isolated candidates/tests concurrently after step2. One integration owner serializes project/asmdef links, SimulationRuntime/City binding, P12 D/codec/coordinator integration and the single Studio import/validation window. Review combined diffs for semantic collisions before integration; do not merge two competing guard implementations.
6. Verify explicit same-source SDK Compile links and Core noEngineReferences asmdef, GUID/source ownership and all named incoming caller accesses. Recompute the semantic compile graph against actual candidate Unity inputs; analytic metadata substitution from the planning graph is not final Unity validation.
7. Run the focused semantic/P12 tests below, locked full .NET cold/warm, current Unity full EditMode, official Smoke and required LongRun for daily causal behavior. Independently review exact code/evidence tip, promote the bounded package normally to Core, record State and recompute residual source closure.

## Required validation and exit

- .NET Core `netstandard2.1` engine-free build and `net8.0` real-owner tests; preserve existing171 baseline case identities plus applicable additions. Use the pinned/current approved SDK, complete original/retained TRX and full effective input/toolchain/assembly identity.
- Population proposals, bounds/floor/stale state, throwing/mutating provider recovery, receipt replay/conflict/revision/order, paired migration accepted/rejected states, same original guard object, callback binding/admission/commit exception behavior and flags.
- Battle rollback uses only a pre-write opaque witness captured from the exact owner. After fault it preserves legacy count/revision/receipt pruning; wrong-owner witness rejects without writes. No arbitrary target setter.
- Genealogy duplicate/self/cycle/invalid endpoints, add/remove/traversal ordering, overflow, birth rollback, immutable export and detached restore, exact restored revision and same-bound/unbound rules. Preserve the named existing tests in the design.
- Authentic Unity runtime ingress→P12 context/epoch→same-owner census→D snapshot→existing codec→detached stage→merged binding and target recapture. Match all affected section IDs/roles/aliases/key sets/bytes/revisions and rejection atomicity. Test required zero and populated valid states; no snapshot overrides standing in for capture.
- Existing Core payload/numeric/provider tests, .NET full cold/warm, Unity assembly/asset/bootstrap/P12 focused regressions, ALL EditMode, official Smoke and causally required LongRun. Derive actual test counts from final source; do not assume the current3176 or171 are permanent totals.
- Exact-tip independent implementation review, diff-check, complete accessible privacy-safe evidence and truthful new assembly/build identities. Existing prior Unity/.NET results establish the base only.

Exit is real shared owner capability with every specified original writer/capture/hydration adapter represented and tested. No active writer loses its revision or enclosing epoch behavior. A successful owner extraction does **not** claim shared SimulationRuntime, private P12-G host closure, v2 image/store integration, detached .NET continuation or cross-host behavior parity.

## Rollback and next authorized work

Use additive isolated commits preserving matching prior build/content/save evidence and source/meta mappings. Roll back with a reviewed additive revert and that matching build; never reset shared history, rewrite saves or fake old MVIDs. No new Library, linked mutable Unity cache or deletion of another worktree is needed.

After promotion, update the member/source/access ledger against the actual resulting interfaces and graph, then finish the selected definition-bound runtime and private restore integration described in [the dependent design](CORE_F_RUNTIME_RESTORE_INTEGRATION_DESIGN.md). Its implementation remains WAIT_DEPENDENCY; do not infer readiness from this owner package or from the existing Unity continuation result. CORE-X remains DEFERRED and P12 scope unchanged. No additional human product/architecture decision is required for this handoff; normal independent implementation and canonical gates remain.
