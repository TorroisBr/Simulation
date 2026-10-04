using System;

/// <summary>
/// Explicit isolated composition for the single inert P15-A proving structure.
/// This profile is not UnityBootstrap-Daily-v1 and installs no gameplay surface.
/// </summary>
public sealed class P15AProvingStructureComposition
{
    private P15AProvingStructureComposition(SimulationRuntime runtime)
    {
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        if (Runtime.StructureStore == null)
            throw new ArgumentException("The P15-A proving runtime must own its StructureStore.", nameof(runtime));
    }

    public SimulationRuntime Runtime { get; }
    public StructureStore StructureStore => Runtime.StructureStore;

    /// <summary>
    /// Publishes a fresh proving runtime over already registered P8-A geography.
    /// The first runtime creation remains unavailable until the first day completes.
    /// </summary>
    public static P15AProvingStructureComposition Compose(
        SimulationTime simulationTime,
        SpatialAuthorityStore spatialAuthorityStore)
    {
        if (simulationTime == null) throw new ArgumentNullException(nameof(simulationTime));
        if (spatialAuthorityStore == null) throw new ArgumentNullException(nameof(spatialAuthorityStore));
        if (spatialAuthorityStore.LocationCount == 0 || !spatialAuthorityStore.ValidateInvariants().IsValid)
            throw new ArgumentException(
                "P15-A proving composition requires an existing valid Location and anchor Hex.",
                nameof(spatialAuthorityStore));

        StructureStore structures = new StructureStore(spatialAuthorityStore);
        SimulationRuntime runtime = new SimulationRuntime(
            simulationTime,
            cities: null,
            npcRuntimes: null,
            economyEnabled: false,
            spatialAuthorityStore: spatialAuthorityStore,
            compositionProfile: SimulationRuntimeCompositionProfile.P15AProvingStructure,
            structureStore: structures);
        if (!runtime.TryCompleteP15AProvingPublication())
            throw new InvalidOperationException("P15-A proving composition could not publish a healthy initial world.");
        return new P15AProvingStructureComposition(runtime);
    }

    public bool TryCreateStructure(
        StructureId structureId,
        LocationId locationId,
        out StructureStoreFailure failure) =>
        Runtime.TryCreateP15AProvingStructure(structureId, locationId, out failure);
}
