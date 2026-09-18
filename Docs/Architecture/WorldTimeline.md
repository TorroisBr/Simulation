# World Timeline and Persistence Architecture Spike

## Motivation

The simulation will eventually need historical queries, checkpoints, alternate timelines, deterministic replay, and persistence adapters. This spike defines the contracts and planning semantics needed for those capabilities without changing the current simulation loop or pretending that a diagnostic snapshot is already a save game.

The intended future flow is:

```text
checkpoint + committed changes/replay data + RNG state
    -> historical world state
    -> optional fork
    -> independent future timeline
```

No database, file format, replay engine, or runtime restore is implemented here.

## Non-goals

This spike does not implement save/load, JSON or binary world serialization, database storage, event-sourced reconstruction, `SimulationRuntime` restore, command replay, RNG restore, merge, cherry-pick, rebase, copy-on-write storage, `Person`, demography, institutions, or configuration resolution.

It also does not modify existing domain, diagnostics, calendar, configuration, scene, prefab, package, or project-settings files.

## Current architecture audit

The current `SimulationRuntime` owns mutable world truth such as `SimulationTime`, cities, the authoritative NPC roster, travel and economy systems, justice/crime state, adventure stores, and other runtime services. `SimulationTime.AbsoluteDay` is the current temporal coordinate, not a historical version identifier.

The existing `WorldStateSnapshot`, `WorldStateDiff`, canonical writer, and invariant validator are diagnostic read models. They are useful for validating a future restore, comparing timelines, and regression tests, but they do not claim to contain every piece of state required to resume a simulation. They remain unchanged by this spike.

Domain events and `NpcChronicle` entries describe activity and retained history. They are not the authoritative current world state and must not become an implicit event-sourcing store. `WorldCommand` and command records represent external/GM intent and command history; they are not arbitrary permission to mutate timeline metadata.

Randomness is currently heterogeneous: decision and crime paths use Unity random calls, conflict has an injectable `IConflictRandomSource`, and seeded conflict randomness uses `System.Random`. There is no common capture/restore contract. The new `WorldRandomStateToken` therefore explicitly supports an unavailable token; it does not claim that the current runtime can be replayed deterministically.

The current configuration work is intentionally not referenced. A checkpoint may carry an opaque `WorldConfigurationRevision` in the future, but this spike does not define or duplicate `SimulationConfiguration`.

## Terminology and truth boundaries

| Concept | Role |
| --- | --- |
| World truth | Mutable runtime state that a future checkpoint serializer must capture sufficiently to restore a simulation. |
| World checkpoint | Immutable metadata and, in the future, a serialized state payload from which restoration can begin. |
| Domain event | A fact about an operation that occurred; useful history, not automatically the current truth. |
| Chronicle | Human-facing narrative/history projection; never authoritative persistence. |
| World command | External/GM intent that a future timeline runtime will commit through a command pipeline. |
| Timeline metadata | Immutable identity, ancestry, head day, and committed revision metadata. |
| Diagnostic snapshot | Read-only observation used for debug, diff, validation, and regression. |

## Timeline identity and revision

`WorldTimelineId` is an ordinal, trimmed, non-whitespace value object. It is never based on Unity instance IDs, hash codes, display names, or implicit randomness.

`WorldCheckpointId` is independent from day and revision, so one timeline can have multiple checkpoints on the same day and revision.

`WorldRevision` is a non-negative value object representing committed mutations within a timeline. It is deliberately separate from `AbsoluteDay`; a day can contain multiple committed revisions.

`WorldSchemaVersion` is a non-negative major/minor value object carried by future checkpoint payloads. Schema migration is intentionally out of scope.

## Timeline descriptor

`WorldTimelineDescriptor` contains:

* timeline ID;
* optional parent timeline ID;
* optional fork day and fork revision;
* current head day and committed head revision.

Root timelines have no parent or fork metadata. Fork descriptors point to any existing timeline, including another child timeline. Descriptors contain metadata only and no runtime world references.

Descriptors are immutable. Advancing a head returns a new descriptor and rejects regressions. A store replaces its own metadata entry only through the explicit head-update operation.

## Checkpoints and payloads

`WorldCheckpointDescriptor` identifies a checkpoint by its own ID and records timeline, day, revision, schema version, RNG token, and opaque configuration revision metadata.

`WorldCheckpointPayload` is only an envelope. It contains the same identity metadata, a `WorldRandomStateToken`, an opaque configuration revision, and `WorldSerializedState`. It does not model NPCs, people, institutions, genealogy, or future configuration sections.

`WorldSerializedState` copies input bytes and returns copies when read. It is deliberately format-agnostic; no serializer implementation or definitive wire format is selected.

## Restore algorithm

`WorldRestorePlanner` is a pure planner:

1. reject an invalid or future target;
2. inspect only checkpoints belonging to the requested timeline;
3. ignore checkpoints after the target day;
4. choose the greatest checkpoint day not after the target;
5. break ties by greatest revision, then ordinal checkpoint ID;
6. produce a `WorldRestorePlan` with the checkpoint and replay interval.

For checkpoints at day 800 and target day 865, the plan describes replay from day 801 through day 865. A checkpoint at day 1000 is never selected. If no usable checkpoint exists, the planner returns a structured `CheckpointNotFound` failure.

`IHistoricalWorldStateProvider` exposes only a future restore-plan query in this phase. `PlannedHistoricalWorldStateProvider` demonstrates the composition of timeline metadata, checkpoint metadata, and the pure planner; it does not return or mutate a `SimulationRuntime`.

## Fork semantics

`WorldForkPlanner` selects a historical base checkpoint, validates that the requested fork day is not after the parent head, and returns a `WorldForkPlan`. The plan identifies the parent, new timeline ID, fork day, base checkpoint, and replay interval. If the target day is after the base checkpoint, the exact final fork revision is intentionally unknown until future replay/commit processing; the plan preserves this as an optional value rather than inventing a revision.

`WorldTimelineDescriptor.TryCreateFork` can materialize a child descriptor once the child head revision is known. Creating a fork never changes the parent descriptor. A child can later be used as the parent for a nested fork.

There is no merge operation. A timeline fork is one-directional: the child logically shares immutable history before the fork and owns new history after it. Physical copy-on-write storage, reference counting, and payload deduplication are future storage concerns.

## Storage contracts

`IWorldTimelineStore` describes root/fork creation, metadata lookup, head updates, and deterministic metadata enumeration. `IWorldCheckpointStore` describes checkpoint storage, lookup, timeline filtering, and nearest-checkpoint selection.

`InMemoryWorldTimelineStore` and `InMemoryWorldCheckpointStore` are small C#-only contract fakes. They reject duplicate IDs, missing parents, and invalid head transitions; they are not a persistence engine and do not write files.

No contract exposes SQL, a database client, Unity serialization, PlayerPrefs, or a filesystem path.

## Serialization contract

`IWorldStateSerializer` separates a future semantic state representation from a serialized payload. It can serialize or deserialize an `IWorldStateRepresentation`, but this phase provides no implementation. This prevents the timeline layer from prematurely choosing JSON, binary, SQLite, or a Unity scene representation.

## RNG determinism requirement

A deterministic future requires at least:

```text
restored world state
+ effective configuration identity
+ committed world commands
+ RNG state
```

The current project does not expose one common RNG snapshot/restore mechanism. `WorldRandomStateToken.Unavailable` records that limitation explicitly. Future runtime work must add an adapter at the RNG boundary before claiming deterministic replay; this spike intentionally does not modify existing random sources.

## Configuration relationship

Configuration Foundation is a parallel branch and is not imported here. The timeline envelope only has an opaque `WorldConfigurationRevision` slot, represented as either a stable fingerprint/revision or `Unspecified`. A future checkpoint writer can populate it after effective configuration exists, without making timeline contracts depend on a configuration implementation.

## Diagnostics relationship

Diagnostics remains diagnostics. Future persistence work can capture a `WorldStateSnapshot` before and after a restore, use `WorldStateDiff` for regression comparison, and run its invariant validator against a reconstructed state. None of those diagnostic classes are used as the checkpoint payload in this spike.

## WorldCommand relationship

After a fork, GM/API changes should enter the child through `WorldCommand` records and a future command-commit pipeline. The timeline contracts do not mutate or extend the existing command types. This preserves the distinction between external command history and world truth.

## Future database adapters and Phase 8

Future adapters may implement the storage contracts with memory, binary files, SQLite, PostgreSQL, cloud storage, or another backend. The core contracts remain storage-agnostic. Phase 8 can add checkpoint materialization, replay data, command replay, RNG restoration, and an actual `IHistoricalWorldStateProvider` that returns a restored world representation.

## Example

```text
MAIN

Day 0
  ───────── Day 865 ───────────────────────── Day 54000
                 │
                 │ fork plan
                 ▼
              ALT-001
              Day 865
                 ↓
        apply WorldCommand("King survives")
                 ↓
              simulate toward Day 1865
```

The main descriptor remains unchanged. `ALT-001` owns future commits after the fork and can itself become the parent of another timeline.
