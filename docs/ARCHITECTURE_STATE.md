# General Architecture State

## Capability-level P12 DAG promotion — 2026-10-03

**Status:** `ARCHITECTURE_PROMOTED` — `PARTIAL_PARALLELIZATION_APPROVED`.
**Canonical branch:** `codex/architecture/world-identity-projection`.
**Previous canonical SHA:** `451340c56e9b676bf6ea43412bcb856b9ccde3de`.
**Approved candidate and promoted architecture SHA:** `ee8cca1010c8f5f37928e81849b6489bffd6a318` from `codex/architecture/p12-capability-dag`.

The user approved promotion of that exact candidate. Final remote preflight found both branches at the expected SHAs, a clean canonical worktree, fast-forward ancestry, unchanged candidate, no newer canonical architecture commit, and unchanged active `codex/phase12/canonical` at `54fc23b89cb13598dd184a2b5b6bacf9dc23b0a7`. `git diff --check 451340c..ee8cca1` passed. The independent exact-tip review of semantic revision `d5ac8a5` passed; the final `ee8cca1` commit adds only its durable review record. Promotion was a non-destructive fast-forward and remote push. No executable file or P12-B branch/worktree was changed.

Canonical meaning is in `SIMULATION_ARCHITECTURE.md` §92A; the [capability DAG audit](architecture/P12_CAPABILITY_DAG_AUDIT.md) and [independent review](architecture/P12_CAPABILITY_DAG_REVIEW.md) record scope and evidence. P12 still owes full deterministic continuation for every admitted owner of `UnityBootstrap-Daily-v1`, and P12-B remains incomplete. There is no global `PHASE 12 FOUNDATION READY` milestone. P13 fork implementation remains capability-blocked; P15/P16 are not implementation-authorized by this promotion. New owners must prove exclusion from the selected P12 composition or tested fail-closed rejection before their domain checkpoint is promoted.

The [Master handoff](architecture/P12_CAPABILITY_DAG_MASTER_HANDOFF.md) records the refreshed executable design set, product decisions, dependency gates, and serial integration boundaries. Numbered-phase States continue to own their own delivery history; this architecture promotion reopens none of them.
