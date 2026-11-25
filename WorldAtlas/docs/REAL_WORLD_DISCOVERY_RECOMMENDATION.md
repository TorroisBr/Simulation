# Real-world observation discovery recommendation

**Status:** bounded design recommendation only. No production catalog schema, discovery root, or real-world reader is approved by this document.

**Authority baseline:** current architecture tip `158f04d4a021c3984b3ba841e246b45e1b0a457b`; World Atlas handoff at promoted base `e3403ebe4d208a9e364b634893879488f424973a`. The user approved the world-first product direction on 2026-11-25 and authorized only the bundled-Demo UX iteration here.

## Current ATLAS-A behavior

ATLAS-A opens the bundled `atlas.demo/v1` World by default. `MainWindowViewModel.LoadBundledDemo` reads Day 1 and Day 2 through the same `ObservationFileReader` used by the optional file picker; the compare path uses the existing `ObservationComparer`. The UI does not derive provenance from a fixture filename. This is the first zero-configuration World-first experience.

The approved LAB-OBS-A v1 contract describes one immutable observation per file with explicit WorldId, RunId, producer/build identity, completed boundary, capabilities, and a raw-byte predecessor digest. Its source/implementation handoff remains gated on a suitable promoted Core host and proved writer isolation. ATLAS-B remains `WAIT_DEPENDENCY` on valid reviewed real observations. There is no approved production catalog or directory-discovery contract today.

## Recommended next design

When the LAB-OBS-A producer and architecture owner are ready, design a **producer-owned, versioned observation catalog** (or an equivalent explicitly bounded index) as the first candidate for world-first discovery. Treat that as a direction to evaluate, not a frozen format. The catalog would identify which immutable artifacts are published and how a user may select them; it would not replace or weaken the observation files' versioned reader, provenance checks, or compatibility rules.

The design review should settle, before implementation:

1. **Ownership and approved source.** Name the producer that publishes the catalog, its trusted discovery root/API, publication boundary, freshness policy, and behavior when an index is missing, stale, or only partly written. Atlas must not scan arbitrary directories or monitor unrelated files.
2. **World and run grouping.** Define how the catalog names a World and its distinct runs/continuations from producer-declared identities. Filenames, folder names, timestamps, and matching labels are not provenance and must not merge unrelated runs.
3. **Artifact references and integrity.** Decide how each catalog entry points to an immutable observation and binds the exact artifact bytes/digest, profile, producer/build, boundary, and predecessor relationship. Keep the catalog and observation-reader versions independently explicit.
4. **Selection and comparison.** Present World first; offer a run/continuation choice only when needed; then show completed observations with human-readable day and sequence labels. Identify a current view separately from a compatible comparison target. Reuse the existing comparison compatibility checks; a catalog must never authorize a gap, foreign run, mismatched scope, or invalid predecessor.
5. **Failure and recovery.** A rejected catalog or observation leaves the last valid World view active, reports the real boundary/source state, and never fabricates a default zero, event, or latest observation from file ordering.
6. **Privacy and execution boundary.** Use an explicitly approved local source. The Atlas remains a read-only consumer; it does not read P12 persistence slots, call Core, start/advance a simulation, or infer a real World or run identity.

Do not settle the catalog's exact version string, JSON fields, filesystem layout, API, fallback rules, or automatic default observation in this UX task. The producer must confirm the publication owner and source lifecycle first. If a catalog cannot be made atomic and provenance-complete, retain explicit artifact selection as the supported advanced path.

## Dependencies and scope

- **Can proceed now:** improve the bundled Demo startup, labels, comparison presentation, accessibility, typography, scaling preference, and Atlas-specific About layout. Use the existing reader, comparer, and fixtures.
- **Must wait:** a production catalog/source contract, real World/Run discovery, and ATLAS-B integration depend on a reviewed LAB-OBS-A producer and real valid artifacts.
- **No change here:** observation v1 wire semantics, Core mutation authority, P12/P13 behavior, and cross-application UI extraction.

The future design acceptance should include producer/Atlas conformance cases for stale or partial catalogs, duplicate/foreign World and Run identities, malformed references, invalid files, provenance mismatch, predecessor gaps, and compatibility rejection, plus last-good-view retention. Exact tests follow the reviewed contract.
