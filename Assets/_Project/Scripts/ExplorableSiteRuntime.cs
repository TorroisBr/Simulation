using System;

[Serializable]
public sealed class ExplorableSiteRuntime
{
    private readonly string runtimeId;
    private readonly ExplorableSiteData definition;
    private readonly SpatialLocationRuntime location;

    public string RuntimeId => runtimeId;
    public ExplorableSiteData Definition => definition;
    public ExplorableSiteData SiteData => definition;
    public string DefinitionId => definition.DefinitionId;
    public SpatialLocationRuntime Location => location;

    public ExplorableSiteRuntime(
        string runtimeId,
        ExplorableSiteData definition,
        SpatialLocationRuntime location)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("ExplorableSiteRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (definition == null)
        {
            throw new ArgumentNullException(nameof(definition));
        }

        if (string.IsNullOrWhiteSpace(definition.DefinitionId) == true)
        {
            throw new ArgumentException("ExplorableSiteRuntime requires a definition with a non-empty DefinitionId.", nameof(definition));
        }

        if (location == null)
        {
            throw new ArgumentNullException(nameof(location));
        }

        if (string.IsNullOrWhiteSpace(location.RuntimeId) == true)
        {
            throw new ArgumentException("ExplorableSiteRuntime requires a location with a non-empty RuntimeId.", nameof(location));
        }

        if (string.Equals(runtimeId, location.RuntimeId, StringComparison.Ordinal) == true)
        {
            throw new ArgumentException("ExplorableSiteRuntime and its SpatialLocationRuntime require distinct RuntimeIds.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
        this.definition = definition;
        this.location = location;
    }

    public ExplorableSiteRuntime(
        RuntimeIdAllocator idAllocator,
        ExplorableSiteData definition,
        SpatialLocationRuntime location)
        : this(
            (idAllocator ?? throw new ArgumentNullException(nameof(idAllocator))).AllocateExplorableSiteId(),
            definition,
            location)
    {
    }
}
