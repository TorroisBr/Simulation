# CORE-F Justice Census Host-Neutral Identity — Scoped Architecture Review

**Verdict:** `PASS — compatible with the existing P12 shared-identity contract`
**Review type:** Focused architectural-compatibility review; not a full implementation or validation review.
**Reviewed candidate:** `cbacf61759a6403c316148c9a3a15b4c5fa2b073`
**Reviewed tree:** `e57c6ba1b0193e0eea1dfea8fcf81daf1640c7ea`
**Candidate parent:** `71c48a255b9e28d3142cf57650d5b5cc3ae66afd`
**Core canonical at review:** `a07402faa4739d0bb276d3bc4aa24e6ed7bd6cea`

## Contract findings

The existing P12 owner ledger requires the Justice Records and Justice P18 receipt census rows to share one exact owner identity. The record row carries the sum of wanted-record and sentence cardinalities and the Justice records revision; the receipt row has cardinality one and its own receipt revision. This review preserves that shared-identity invariant and does not change P12 section IDs, schemas, roles, payloads, or wire behavior.

The Unity providers continue to use the exact `JusticeSystem` instance as the identity for both rows. In `SimulationRuntime.TryGetExactJusticeRecordsCensusProjectionIdentity`, Unity verifies that the supplied Core `DailyV1JusticeRecordsOwner` is the exact selected graph owner and that `justiceSystem.CoreOwner` is that same instance. It returns the Unity projection identity and the receipt census revision. The Unity projection identity therefore remains unchanged where the accepted Unity/P12 path requires it.

## Candidate compatibility

The candidate adds `DailyV1SelectedRuntimeOwnerCensusSource.TryCreateCoreOwnedJusticeSections`. It creates expected census witnesses with the same supplied identity for both rows. It obtains record cardinality from the selected Core owner’s wanted and sentence lists and obtains record revision from that owner. The P18 receipt row retains its fixed cardinality of one and uses the host-supplied receipt revision. The identity is transient and is not serialized.

`TryValidateCoreOwnedJusticeSections` compares the captured rows against those expected witnesses, including unique row presence, required role, schema, reference identity, cardinality, and revision. The normal and restored capture paths still obtain the projection identity through the host port before validating. The change does not rebind either row to a different identity or weaken the previous checks.

The new regression in `DailyV1ApplicationSessionGateTests.CoreRejectsJusticeCensusThatDoesNotMatchItsExactOwnerAndProjection` uses one opaque host-local identity for both rows, verifies it is shared and is not the Core owner object, then exercises the Core capture boundary and the existing rejection mutations. This demonstrates the host-neutral identity shape while retaining the P12 pair invariant.

## Detached-host compatibility

The fixed-host design requires a .NET host with no Unity dependency and the same Core capture, codec, storage, and restore path. The current Core interfaces carry this census projection identity as an opaque `object`; they do not require a Unity type. `DailyV1P12ECoreOwnerSourceCapture.TryCapture` requires the Justice rows to share one identity while taking the Justice values and local revision from the Core owner. Therefore a detached .NET adapter can supply one ephemeral host-local identity for both rows, provided its adapter binds that identity to the exact selected Core Justice owner and the corresponding receipt witness/revision. It must not claim that the identity is a Unity `JusticeSystem`.

This is an adapter obligation for the future .NET host. The helper alone does not establish that binding: each host port must verify its own exact owner/projection relationship. No separate row identities or P12 reconciliation are required by this candidate.

## Scope and limits

This review covers only architectural compatibility of the Justice census helper and its focused test change in candidate `cbacf617`. It does not establish that a detached .NET Justice adapter exists, that CORE-F has completed its runtime/restore integration, or that the candidate has passed its required validation and exact-tip implementation review. It is not approval for canonical promotion or phase closure.
