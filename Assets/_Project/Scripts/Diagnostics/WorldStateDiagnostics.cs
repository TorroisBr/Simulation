public static class WorldStateDiagnostics
{
    public static WorldStateSnapshot Capture(WorldStateSnapshotContext context)
    {
        return WorldStateSnapshotBuilder.BuildSnapshot(context);
    }

    public static WorldStateDiff Compare(WorldStateSnapshot before, WorldStateSnapshot after)
    {
        return WorldStateDiff.Compare(before, after);
    }

    public static WorldStateInvariantReport Validate(WorldStateSnapshot snapshot)
    {
        return WorldStateInvariantValidator.Validate(snapshot);
    }

    public static string Format(WorldStateSnapshot snapshot)
    {
        return WorldStateSnapshotFormatter.Format(snapshot);
    }

    public static string Format(WorldStateDiff diff)
    {
        return WorldStateDiffFormatter.Format(diff);
    }

    public static string Export(WorldStateSnapshot snapshot)
    {
        return WorldStateCanonicalWriter.Write(snapshot);
    }
}

public static class WorldStateDiffer
{
    public static WorldStateDiff Compare(WorldStateSnapshot before, WorldStateSnapshot after)
    {
        return WorldStateDiff.Compare(before, after);
    }
}
