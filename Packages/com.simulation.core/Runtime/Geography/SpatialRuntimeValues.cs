using System;

[Serializable]
public sealed class SpatialLocationRuntime
{
    private readonly string runtimeId;

    public string RuntimeId => runtimeId;

    public SpatialLocationRuntime(string runtimeId)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("SpatialLocationRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        this.runtimeId = runtimeId;
    }
}

[Serializable]
public sealed class SpatialRouteRuntime
{
    private readonly string runtimeId;
    private readonly SpatialLocationRuntime origin;
    private readonly SpatialLocationRuntime destination;
    private readonly int travelDays;

    public string RuntimeId => runtimeId;
    public SpatialLocationRuntime Origin => origin;
    public SpatialLocationRuntime Destination => destination;
    public int TravelDays => travelDays;

    public SpatialRouteRuntime(
        string runtimeId,
        SpatialLocationRuntime origin,
        SpatialLocationRuntime destination,
        int travelDays)
    {
        if (string.IsNullOrWhiteSpace(runtimeId) == true)
        {
            throw new ArgumentException("SpatialRouteRuntime requires a non-empty RuntimeId.", nameof(runtimeId));
        }

        if (origin == null)
        {
            throw new ArgumentNullException(nameof(origin));
        }

        if (destination == null)
        {
            throw new ArgumentNullException(nameof(destination));
        }

        if (origin == destination)
        {
            throw new ArgumentException("SpatialRouteRuntime cannot connect a location to itself.", nameof(destination));
        }

        this.runtimeId = runtimeId;
        this.origin = origin;
        this.destination = destination;
        this.travelDays = Math.Max(1, travelDays);
    }
}
