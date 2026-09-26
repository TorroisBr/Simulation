# Phase 19 — Code Mods & Public Extension Surface v1

**Authority:** future planning entry under `../SIMULATION_ARCHITECTURE.md` §§2,
12, 91–93. **Readiness:** `DEFERRED` for platform implementation; existing
phases must preserve the principles now. No P19 checkpoint IDs are approved.

## Objective and closure

A later bounded public API/loader permits player-selected real code to add
systems/mechanics and authoritative data beyond the base game's catalog.
Define supported module lifecycle, semantic hooks and contributions, compatibility
and state ownership. Optional official expansions should ideally use the same
surface; useful mechanics can move into core without conceptual reinvention.

The world belongs to the local single-player user. No anti-cheat, adversarial
authorization or restrictive mod-security architecture is in this contract.
Existing validation/guards remain coherence tools for supported operations.

## Dependencies and gates

- **Entry gate:** concrete real extension consumers and stable contracts to expose;
  do not invent a plugin framework for unrelated phases. Decompose checkpoints
  and approve v1 scope at entry. No particular DLL/package/loader technology selected.
- **Component dependencies:** generation adapters consume relevant P9/P10 pipeline
  capabilities; timed systems consume P18; supported durable module state and
  retrofit consume applicable P12/P13 state/compatibility boundaries. A pure
  extension contract may precede persistence, but cannot claim saved-mod-world
  support. No blanket dependency on all those phases or on P17.
  Multi-participant activity adapters consume the relevant P20 participant
  contract/capability; the basic mod platform need not wait for all of P20,
  and P20's official implementation does not require the P19 loader.
- **Public boundary:** presentation-independent semantic APIs/hooks; registries,
  policies/modifiers/pipelines only where independent contributors need them.
  Resolve ordering, duplicates/conflicts and causal RNG deterministically.
  A future alternate renderer/application can interact with the same simulation;
  transport/extraction, including Unity-hosted IPC fallback, needs its own design.
  A code mod can define a new activity and role/requirement, participation and
  participant-effect policies using supported semantic contracts, without editing
  `NpcRuntime` or requiring a special activity manager added to the base game.
  This preserves arbitrary future arrangements without implementing every role
  or arrangement up front or introducing a universal workflow engine.
- **Generation:** modules may consume earlier stage outputs, generate persistent
  domain data, contribute scoring/policies or add dependency-ordered stages.
  New-world-only participation is valid. Existing-world support is a separate,
  optional explicit retrofit/migration, not automatic historical regeneration.
- **State/compatibility:** module-owned data joins the authoritative inventory.
  Define recoverable code/content versions and required configuration, identities,
  causal inputs/order and lifecycle. Missing incompatible contributors must not
  silently discard state or pretend continuation is equivalent.
- **Exclusions:** early loader in P8–P18, wind/climate/sailing gameplay, adversarial
  mod-security/anti-cheat platform, universal hooks on every method, multiplayer and a second
  world authority. No mod-state/manifest/save schema is fixed here.
- **Validation:** a bounded code extension adds a new mechanic/state through the
  public surface; deterministic independent contributions; new-world versus
  explicit retrofit distinction; compatible continuation where claimed; no silent
  historical stage rerun. Use real selected consumers, not unrelated gameplay.
- **Hotspots:** composition, domain/application seams, generation stages,
  temporal hooks, commands, content/versioning and persistence. Dedicated
  isolated implementation and independent review are required later.
