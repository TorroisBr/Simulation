using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serialization-only representation for the legacy Unity nested field. Live
/// selected and legacy runtime reads/writes are delegated to one Core owner;
/// this adapter copies values only at Unity serialization boundaries.
/// </summary>
[Serializable]
internal sealed class SpatialKnowledgeSerializationAdapter : ISerializationCallbackReceiver
{
    // Keep these names stable: Unity has persisted this nested value in NPC
    // state and older scenes/runtime fixtures still deserialize the same shape.
    [SerializeField] private string ownerRuntimeId;
    [SerializeField] private List<string> knownLocationRuntimeIds = new List<string>();
    [SerializeField] private List<string> knownRouteRuntimeIds = new List<string>();
    [SerializeField] private long revision;
    [NonSerialized] private SpatialKnowledgeRuntime owner;

    public SpatialKnowledgeSerializationAdapter() { }

    internal SpatialKnowledgeSerializationAdapter(SpatialKnowledgeRuntime owner)
    {
        if (!TryBind(owner)) throw new ArgumentNullException(nameof(owner));
    }

    internal SpatialKnowledgeRuntime BoundOwner => owner;

    internal SpatialKnowledgeRuntime GetOrCreateOwner(string fallbackRuntimeId)
    {
        if (owner != null) return owner;
        string selectedOwnerId = string.IsNullOrWhiteSpace(ownerRuntimeId)
            ? fallbackRuntimeId
            : ownerRuntimeId;
        if (!SpatialKnowledgeRuntime.TryCreateFromSerializedState(
                selectedOwnerId, knownLocationRuntimeIds, knownRouteRuntimeIds, revision,
                out owner))
            return null;
        return owner;
    }

    internal bool TryBind(SpatialKnowledgeRuntime exactOwner)
    {
        if (exactOwner == null) return false;
        owner = exactOwner;
        CaptureCoreProjection();
        return true;
    }

    public void OnBeforeSerialize()
    {
        CaptureCoreProjection();
    }

    public void OnAfterDeserialize()
    {
        owner = null;
    }

    private void CaptureCoreProjection()
    {
        if (owner == null) return;
        ownerRuntimeId = owner.OwnerRuntimeId;
        knownLocationRuntimeIds = new List<string>(owner.KnownLocationRuntimeIds);
        knownRouteRuntimeIds = new List<string>(owner.KnownRouteRuntimeIds);
        revision = owner.Revision;
    }
}
