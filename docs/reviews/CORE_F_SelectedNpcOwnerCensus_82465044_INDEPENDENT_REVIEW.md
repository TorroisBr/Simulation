# Independent review — CORE-F selected NPC owner census

**Verdict: VALIDATED_CANDIDATE**

## Exact review identity

- Candidate branch: `codex/core/CORE-F-core-owned-runtime-application`
- Candidate evidence tip: `8246504413802efe44d6667c41bcd7c71e18e103` (tree `841c22891f7c2abd6f1ea8df59d1ce7885227f97`)
- Code-bearing commit reviewed: `67d384c79cfd9942955fa6fdfb9aece18e2c8901` (tree `96a32d93d2d5e550270f293d580be31025dc7659`)
- Exact base: `457a17da8a7d1b0df0a852af9a65fc8062204244` (tree `93d1332c1f5793517b838eebf80fb229d13e5259`)
- Current Core canonical: `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`
- Remote verification on 2026-10-29: `origin/codex/core/CORE-F-core-owned-runtime-application` points to the candidate tip; `origin/codex/core/canonical` points to the current Core canonical. The canonical commit is an ancestor of the stated base.

I reviewed the complete base-to-code diff and the documentation-only evidence commit from `67d384c` to `8246504`. The candidate tip adds only the validation note, manifest, and normalized Unity result archive over the code-bearing commit. Review was performed against the immutable committed tree in a clean isolated worktree; no candidate source was edited.

## Findings

### Core owner identity and census values

The new Core provider creates one Inventory and one MoneyAccount witness per selected NPC, orders providers by ordinal `RuntimeId`, rejects duplicate IDs and aliased selected owners, and keeps each Inventory bound to the same effective definitions graph. Inventory cardinality comes from the Core owner’s non-materializing census method, and its revision comes from that same owner. MoneyAccount reports cardinality one and the exact account owner’s revision. The Unity provider checks that selected Core owners are the exact objects bound to the NPC projections.

When any NPC in the roster lacks selected Core owners, Inventory provider creation uses the Unity `InventoryRuntime` projection for the whole family. The selected-only path cannot be mixed with projection identities per NPC. MoneyAccount continues to use the runtime’s existing `MoneyAccountRuntime` object, which is also the selected Core owner. The focused provider tests and selected-profile owner-vector test cover these identities and the mixed-roster fallback.

### Alias transitions and P12 snapshots

A registered Inventory section may change identity between a Core owner and its Unity projection only for the same NPC and only while the projection’s selected owner is that exact Core Inventory. Before replacing a provider, reconciliation validates the prior witness; it then stages the replacement witness and counts the identity change as one family delta. Tests exercise Core-to-projection fallback when a legacy NPC is added. The reverse transition is implemented by the symmetric alias check and is expected on unregistering that legacy NPC; there is no direct test for that direction, which is a non-blocking coverage note.

P12 NPC snapshot capture selects Core Inventory identity only when every roster member has selected owners. Otherwise it expects the projection identity for every member. It checks the selected projection points to that exact Core owner and validates the Inventory section’s owner identity, row cardinality, and revision against the live snapshot. A projection that cannot represent the Core rows therefore fails the section check instead of silently dropping them. MoneyAccount snapshot identity remains the exact runtime account owner.

### Ordering, mutation binding, and failure atomicity

All provider lists are ordinal by `RuntimeId`; lookup dictionaries use ordinal string comparison and are not used as semantic iteration order. Existing P12 mutation binding still reaches the Core Inventory mutation boundary through its `InventoryRuntime` projection, while MoneyAccount binds the same owner directly. The candidate does not add writers or broaden epoch claims.

Roster reconciliation validates candidate families and stages section maps, owner identities, and provider arrays before publishing. It re-reads an existing Inventory witness before an alias-identity transition, commits the expected section inventory once, swaps the staged maps, and advances the mutation epoch at most once for the combined operation. Invalid coverage or identity faults the census without publishing a partial family baseline. The exact code diff passes `git diff --check`.

## Validation evidence and privacy

The committed validation note binds results to code commit `67d384c` and tree `96a32d9`. Its documented changed-source hashes align with the committed inputs and the public Unity input manifest. The official CoreValidation report records 919/919 cold and warm passes. The Unity report records full EditMode 3279/3279, official Smoke 5/5, and focused owner-closure 1/1, P12-D NPC snapshot 33/33, Runtime Admission 228/228, and Bootstrap Composition 39/39. I did not rerun tests.

The published Unity archive SHA-256 matches its manifest (`bcac673aca538aa63f15d36bd4076ce572f34b76c45f46cdc8c8e508e300898e`). It contains one normalized test-result XML; that XML reports `Passed`, 3279 total and 3279 passed, with zero failed, skipped, or inconclusive cases. The manifest contains repository-relative input paths and no absolute user/home paths. The packaged XML contains no absolute path or credential-like marker. The filtered Smoke XML was rejected by the packager because its parent-suite count did not match its descendants; it was not included or relabelled, while the separate official 5/5 Smoke result remains reported. Private original reports/logs were not opened or changed.

The existing candidate linked worktree was dirty at review time, with local edits to `SimulationRuntime.cs`, `DailyV1RuntimeAdvanceStage.cs`, `DailyV1ApplicationSessionGateTests.cs`, ProjectSettings, and private/untracked evidence folders. Those edits are outside the reviewed ref; they were not inspected or modified. The review applies only to the exact committed candidate tip and its attached validation evidence.

## Limits

This validates the selected NPC census identity checkpoint only. It does not promote Core canonical, complete CORE-F, establish detached-host continuation, claim full runtime closure or capture readiness, or reopen or expand the accepted P12 profile. The review is not canonical promotion approval.
