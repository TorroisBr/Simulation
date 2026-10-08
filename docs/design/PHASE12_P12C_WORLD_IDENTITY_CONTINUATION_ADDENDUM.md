# P12-C World Identity Continuation Addendum

**Status:** Narrow current-architecture revalidation for the accepted P12-C identity roots. This adds no checkpoint and does not authorize P12-A integration or Phase closure.

**Authority:** Architecture `codex/architecture/world-identity-projection` at `47eff220c7ce00f6e7c759bdc2b76780bb46f628`, §91A; accepted P12-B–P12-G capability decomposition; and `PHASE12_P12C_P9B_GENESIS_MANIFEST_SNAPSHOT_DESIGN.md`, which assigns `WorldId` its own P12-C slice.

## Required capability

The selected `UnityBootstrap-Daily-v1` continuation must retain the exact `WorldId` of the same causal world. Loading must not generate or substitute another identity. WI-A already publishes a typed immutable `WorldId` with the validated bootstrap composition; P12-C must snapshot that value and privately reconstruct an equal `WorldId` for the staged root.

Extend the existing P12-C private root-composition contract with one `P12CWorldIdentitySnapshot` containing its snapshot contract/schema and canonical `WorldId.Value`. Capture accepts the identity already held by the validated composition. Capture and staging validate the canonical `world:<32 lowercase hexadecimal digits>` form with `WorldId.TryParse`; staging returns a fresh typed `WorldId` with the exact retained value. The identity is included in the all-or-none private root bundle. It is never newly allocated during continuation staging.

The final restore owner will use this staged instance when it composes the runtime and bootstrap, preserving WI-A's exact shared-instance requirement before publication. This addendum does not add that composition or publication path.

## Tests and exclusions

Focused tests prove exact value round-trip, rejection of null/unsupported/malformed identity snapshots with no partial root, and no source identity mutation. Existing P12-C owner tests and the aggregate composition tests remain required; ALL EditMode, official Smoke, applicable SimulationRuntime LongRun, and `git diff --check` run on the final integrated code tree.

This is continuation of the existing C identity capability, not a new world, branch, or product behavior. It does not implement an encoded save envelope, migration of pre-WI-A worlds, copied-save branching, P13 historical fork/provenance, P12-A profile integration, runtime publication, or any new WorldId allocation. P12-A remains `WAIT_DEPENDENCY`; P13 remains blocked.
