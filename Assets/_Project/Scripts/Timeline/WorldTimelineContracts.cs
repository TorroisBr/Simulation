using System;
using System.Collections.Generic;

public enum WorldTimelineFailureCode
{
    None = 0,
    InvalidTimelineId,
    InvalidCheckpointId,
    InvalidAbsoluteDay,
    InvalidRevision,
    InvalidSchemaVersion,
    InvalidSerializedState,
    InvalidTimelineDescriptor,
    InvalidCheckpointDescriptor,
    InvalidCheckpointPayload,
    InvalidForkDay,
    InvalidHead,
    ChildTimelineSameAsParent,
    TimelineNotFound,
    DuplicateTimeline,
    DuplicateCheckpoint,
    CheckpointNotFound,
    CheckpointAfterTarget,
    TargetAfterTimelineHead,
    TimelineMismatch,
    HeadRegression,
    InvalidWorldStateRepresentation,
    InvalidRandomStateToken,
    InvalidConfigurationRevision
}

public sealed class WorldTimelineFailure
{
    private readonly WorldTimelineFailureCode code;
    private readonly string message;

    public WorldTimelineFailureCode Code => code;
    public string Message => message;
    public bool IsFailure => code != WorldTimelineFailureCode.None;

    private WorldTimelineFailure(WorldTimelineFailureCode code, string message)
    {
        this.code = code;
        this.message = message ?? string.Empty;
    }

    public static WorldTimelineFailure None
    {
        get { return new WorldTimelineFailure(WorldTimelineFailureCode.None, string.Empty); }
    }

    public static WorldTimelineFailure Create(WorldTimelineFailureCode code, string message)
    {
        if (code == WorldTimelineFailureCode.None)
        {
            throw new ArgumentException("A failure must have a non-empty failure code.", nameof(code));
        }

        return new WorldTimelineFailure(code, message);
    }

    public override string ToString()
    {
        return code + ": " + message;
    }
}

public sealed class WorldTimelineId : IEquatable<WorldTimelineId>, IComparable<WorldTimelineId>
{
    private readonly string value;

    public string Value => value;

    public WorldTimelineId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Timeline ID cannot be null, empty, or whitespace.", nameof(value));
        }

        this.value = value.Trim();
    }

    public static bool TryCreate(
        string value,
        out WorldTimelineId timelineId,
        out WorldTimelineFailure failure)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            timelineId = null;
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Timeline ID cannot be null, empty, or whitespace.");
            return false;
        }

        timelineId = new WorldTimelineId(value);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool Equals(WorldTimelineId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldTimelineId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public int CompareTo(WorldTimelineId other)
    {
        return other == null
            ? 1
            : string.Compare(value, other.value, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(WorldTimelineId left, WorldTimelineId right)
    {
        return ReferenceEquals(left, right)
            || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    }

    public static bool operator !=(WorldTimelineId left, WorldTimelineId right)
    {
        return !(left == right);
    }
}

public sealed class WorldCheckpointId : IEquatable<WorldCheckpointId>, IComparable<WorldCheckpointId>
{
    private readonly string value;

    public string Value => value;

    public WorldCheckpointId(string value)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Checkpoint ID cannot be null, empty, or whitespace.", nameof(value));
        }

        this.value = value.Trim();
    }

    public static bool TryCreate(
        string value,
        out WorldCheckpointId checkpointId,
        out WorldTimelineFailure failure)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            checkpointId = null;
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidCheckpointId,
                "Checkpoint ID cannot be null, empty, or whitespace.");
            return false;
        }

        checkpointId = new WorldCheckpointId(value);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool Equals(WorldCheckpointId other)
    {
        return other != null && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldCheckpointId);
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(value);
    }

    public int CompareTo(WorldCheckpointId other)
    {
        return other == null
            ? 1
            : string.Compare(value, other.value, StringComparison.Ordinal);
    }

    public override string ToString()
    {
        return value;
    }

    public static bool operator ==(WorldCheckpointId left, WorldCheckpointId right)
    {
        return ReferenceEquals(left, right)
            || (!ReferenceEquals(left, null) && !ReferenceEquals(right, null) && left.Equals(right));
    }

    public static bool operator !=(WorldCheckpointId left, WorldCheckpointId right)
    {
        return !(left == right);
    }
}

public struct WorldRevision : IEquatable<WorldRevision>, IComparable<WorldRevision>
{
    private readonly long value;

    public long Value => value;

    public WorldRevision(long value)
    {
        if (value < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "World revision cannot be negative.");
        }

        this.value = value;
    }

    public static WorldRevision Zero => new WorldRevision(0L);

    public static bool TryCreate(
        long value,
        out WorldRevision revision,
        out WorldTimelineFailure failure)
    {
        if (value < 0L)
        {
            revision = default(WorldRevision);
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidRevision,
                "World revision cannot be negative.");
            return false;
        }

        revision = new WorldRevision(value);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool Equals(WorldRevision other)
    {
        return value == other.value;
    }

    public override bool Equals(object obj)
    {
        return obj is WorldRevision && Equals((WorldRevision)obj);
    }

    public override int GetHashCode()
    {
        return value.GetHashCode();
    }

    public int CompareTo(WorldRevision other)
    {
        return value.CompareTo(other.value);
    }

    public override string ToString()
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool operator ==(WorldRevision left, WorldRevision right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(WorldRevision left, WorldRevision right)
    {
        return left.Equals(right) == false;
    }

    public static bool operator <(WorldRevision left, WorldRevision right)
    {
        return left.value < right.value;
    }

    public static bool operator >(WorldRevision left, WorldRevision right)
    {
        return left.value > right.value;
    }
}

public struct WorldSchemaVersion : IEquatable<WorldSchemaVersion>, IComparable<WorldSchemaVersion>
{
    public int Major { get; }
    public int Minor { get; }

    public WorldSchemaVersion(int major, int minor)
    {
        if (major < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(major), major, "Schema major cannot be negative.");
        }

        if (minor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minor), minor, "Schema minor cannot be negative.");
        }

        Major = major;
        Minor = minor;
    }

    public static bool TryCreate(
        int major,
        int minor,
        out WorldSchemaVersion version,
        out WorldTimelineFailure failure)
    {
        if (major < 0 || minor < 0)
        {
            version = default(WorldSchemaVersion);
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidSchemaVersion,
                "Schema major and minor versions cannot be negative.");
            return false;
        }

        version = new WorldSchemaVersion(major, minor);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool Equals(WorldSchemaVersion other)
    {
        return Major == other.Major && Minor == other.Minor;
    }

    public override bool Equals(object obj)
    {
        return obj is WorldSchemaVersion && Equals((WorldSchemaVersion)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Major * 397) ^ Minor;
        }
    }

    public int CompareTo(WorldSchemaVersion other)
    {
        int majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public override string ToString()
    {
        return Major + "." + Minor;
    }
}

public sealed class WorldRandomStateToken : IEquatable<WorldRandomStateToken>
{
    private readonly bool isAvailable;
    private readonly string value;

    public bool IsAvailable => isAvailable;
    public string Value => value;

    public WorldRandomStateToken(string value)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Available random state tokens cannot be empty.", nameof(value));
        }

        isAvailable = true;
        this.value = value.Trim();
    }

    private WorldRandomStateToken(bool isAvailable, string value)
    {
        this.isAvailable = isAvailable;
        this.value = value;
    }

    public static WorldRandomStateToken Unavailable
    {
        get { return new WorldRandomStateToken(false, null); }
    }

    public bool Equals(WorldRandomStateToken other)
    {
        return other != null
            && isAvailable == other.isAvailable
            && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldRandomStateToken);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (isAvailable ? 1 : 0) * 397
                ^ (value == null ? 0 : StringComparer.Ordinal.GetHashCode(value));
        }
    }

    public override string ToString()
    {
        return isAvailable ? value : "unavailable";
    }
}

public sealed class WorldConfigurationRevision : IEquatable<WorldConfigurationRevision>
{
    private readonly bool isSpecified;
    private readonly string value;

    public bool IsSpecified => isSpecified;
    public string Value => value;

    public WorldConfigurationRevision(string value)
    {
        if (string.IsNullOrWhiteSpace(value) == true)
        {
            throw new ArgumentException("Configuration revision cannot be empty.", nameof(value));
        }

        isSpecified = true;
        this.value = value.Trim();
    }

    private WorldConfigurationRevision(bool isSpecified, string value)
    {
        this.isSpecified = isSpecified;
        this.value = value;
    }

    public static WorldConfigurationRevision Unspecified
    {
        get { return new WorldConfigurationRevision(false, null); }
    }

    public bool Equals(WorldConfigurationRevision other)
    {
        return other != null
            && isSpecified == other.isSpecified
            && string.Equals(value, other.value, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldConfigurationRevision);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (isSpecified ? 1 : 0) * 397
                ^ (value == null ? 0 : StringComparer.Ordinal.GetHashCode(value));
        }
    }

    public override string ToString()
    {
        return isSpecified ? value : "unspecified";
    }
}

public sealed class WorldSerializedState : IEquatable<WorldSerializedState>
{
    private readonly byte[] bytes;

    public int Length => bytes.Length;
    public IReadOnlyList<byte> Bytes => Array.AsReadOnly(CopyBytes());

    public WorldSerializedState(IEnumerable<byte> bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        this.bytes = new List<byte>(bytes).ToArray();
    }

    public byte[] CopyBytes()
    {
        byte[] copy = new byte[bytes.Length];
        Array.Copy(bytes, copy, bytes.Length);
        return copy;
    }

    public bool Equals(WorldSerializedState other)
    {
        if (other == null || bytes.Length != other.bytes.Length)
        {
            return false;
        }

        for (int i = 0; i < bytes.Length; i++)
        {
            if (bytes[i] != other.bytes[i])
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldSerializedState);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (byte value in bytes)
            {
                hash = hash * 31 + value;
            }

            return hash;
        }
    }
}

public interface IWorldStateRepresentation
{
}

public interface IWorldStateSerializer
{
    bool TrySerialize(
        IWorldStateRepresentation representation,
        out WorldSerializedState serializedState,
        out WorldTimelineFailure failure);

    bool TryDeserialize(
        WorldSerializedState serializedState,
        out IWorldStateRepresentation representation,
        out WorldTimelineFailure failure);
}

public sealed class WorldCheckpointDescriptor : IEquatable<WorldCheckpointDescriptor>
{
    public WorldCheckpointId CheckpointId { get; }
    public WorldTimelineId TimelineId { get; }
    public long AbsoluteDay { get; }
    public WorldRevision Revision { get; }
    public WorldSchemaVersion SchemaVersion { get; }
    public WorldRandomStateToken RandomState { get; }
    public WorldConfigurationRevision ConfigurationRevision { get; }

    public WorldCheckpointDescriptor(
        WorldCheckpointId checkpointId,
        WorldTimelineId timelineId,
        long absoluteDay,
        WorldRevision revision,
        WorldSchemaVersion schemaVersion,
        WorldRandomStateToken randomState = null,
        WorldConfigurationRevision configurationRevision = null)
    {
        if (checkpointId == null)
        {
            throw new ArgumentNullException(nameof(checkpointId));
        }

        if (timelineId == null)
        {
            throw new ArgumentNullException(nameof(timelineId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), absoluteDay, "Checkpoint day cannot be negative.");
        }

        CheckpointId = checkpointId;
        TimelineId = timelineId;
        AbsoluteDay = absoluteDay;
        Revision = revision;
        SchemaVersion = schemaVersion;
        RandomState = randomState ?? WorldRandomStateToken.Unavailable;
        ConfigurationRevision = configurationRevision ?? WorldConfigurationRevision.Unspecified;
    }

    public bool Equals(WorldCheckpointDescriptor other)
    {
        return other != null
            && CheckpointId == other.CheckpointId
            && TimelineId == other.TimelineId
            && AbsoluteDay == other.AbsoluteDay
            && Revision == other.Revision
            && SchemaVersion.Equals(other.SchemaVersion)
            && RandomState.Equals(other.RandomState)
            && ConfigurationRevision.Equals(other.ConfigurationRevision);
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldCheckpointDescriptor);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = CheckpointId.GetHashCode();
            hash = hash * 31 + TimelineId.GetHashCode();
            hash = hash * 31 + AbsoluteDay.GetHashCode();
            hash = hash * 31 + Revision.GetHashCode();
            hash = hash * 31 + SchemaVersion.GetHashCode();
            hash = hash * 31 + RandomState.GetHashCode();
            return hash * 31 + ConfigurationRevision.GetHashCode();
        }
    }
}

public sealed class WorldCheckpointPayload
{
    public WorldSchemaVersion SchemaVersion { get; }
    public WorldTimelineId TimelineId { get; }
    public WorldCheckpointId CheckpointId { get; }
    public long AbsoluteDay { get; }
    public WorldRevision Revision { get; }
    public WorldRandomStateToken RandomState { get; }
    public WorldConfigurationRevision ConfigurationRevision { get; }
    public WorldSerializedState SerializedWorldState { get; }

    public WorldCheckpointPayload(
        WorldSchemaVersion schemaVersion,
        WorldTimelineId timelineId,
        WorldCheckpointId checkpointId,
        long absoluteDay,
        WorldRevision revision,
        WorldRandomStateToken randomState,
        WorldSerializedState serializedWorldState,
        WorldConfigurationRevision configurationRevision = null)
    {
        if (timelineId == null)
        {
            throw new ArgumentNullException(nameof(timelineId));
        }

        if (checkpointId == null)
        {
            throw new ArgumentNullException(nameof(checkpointId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay), absoluteDay, "Checkpoint day cannot be negative.");
        }

        if (randomState == null)
        {
            throw new ArgumentNullException(nameof(randomState));
        }

        if (serializedWorldState == null)
        {
            throw new ArgumentNullException(nameof(serializedWorldState));
        }

        SchemaVersion = schemaVersion;
        TimelineId = timelineId;
        CheckpointId = checkpointId;
        AbsoluteDay = absoluteDay;
        Revision = revision;
        RandomState = randomState;
        ConfigurationRevision = configurationRevision ?? WorldConfigurationRevision.Unspecified;
        SerializedWorldState = serializedWorldState;
    }
}

public sealed class WorldTimelineDescriptor : IEquatable<WorldTimelineDescriptor>
{
    public WorldTimelineId TimelineId { get; }
    public WorldTimelineId ParentTimelineId { get; }
    public long? ForkAbsoluteDay { get; }
    public WorldRevision? ForkRevision { get; }
    public long HeadAbsoluteDay { get; }
    public WorldRevision HeadRevision { get; }
    public bool IsRoot => ParentTimelineId == null;

    private WorldTimelineDescriptor(
        WorldTimelineId timelineId,
        WorldTimelineId parentTimelineId,
        long? forkAbsoluteDay,
        WorldRevision? forkRevision,
        long headAbsoluteDay,
        WorldRevision headRevision)
    {
        TimelineId = timelineId;
        ParentTimelineId = parentTimelineId;
        ForkAbsoluteDay = forkAbsoluteDay;
        ForkRevision = forkRevision;
        HeadAbsoluteDay = headAbsoluteDay;
        HeadRevision = headRevision;
    }

    public static bool TryCreateRoot(
        WorldTimelineId timelineId,
        long headAbsoluteDay,
        WorldRevision headRevision,
        out WorldTimelineDescriptor descriptor,
        out WorldTimelineFailure failure)
    {
        if (timelineId == null)
        {
            descriptor = null;
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "Root timeline requires a timeline ID.");
            return false;
        }

        if (headAbsoluteDay < 0L)
        {
            descriptor = null;
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidAbsoluteDay,
                "Timeline head day cannot be negative.");
            return false;
        }

        descriptor = new WorldTimelineDescriptor(
            timelineId,
            null,
            null,
            null,
            headAbsoluteDay,
            headRevision);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public static bool TryCreateFork(
        WorldTimelineDescriptor parent,
        WorldTimelineId childTimelineId,
        long forkAbsoluteDay,
        WorldRevision? forkRevision,
        long childHeadAbsoluteDay,
        WorldRevision childHeadRevision,
        out WorldTimelineDescriptor descriptor,
        out WorldTimelineFailure failure)
    {
        descriptor = null;

        if (parent == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineDescriptor,
                "A fork requires a parent timeline descriptor.");
            return false;
        }

        if (childTimelineId == null)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidTimelineId,
                "A fork requires a child timeline ID.");
            return false;
        }

        if (childTimelineId == parent.TimelineId)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.ChildTimelineSameAsParent,
                "A child timeline must have a different ID from its parent.");
            return false;
        }

        if (forkAbsoluteDay < 0L || forkAbsoluteDay > parent.HeadAbsoluteDay)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidForkDay,
                "Fork day must be between zero and the parent head day.");
            return false;
        }

        if (forkAbsoluteDay == parent.HeadAbsoluteDay
            && forkRevision.HasValue == true
            && forkRevision.Value > parent.HeadRevision)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidForkDay,
                "Fork revision cannot be after the parent head revision on the parent head day.");
            return false;
        }

        if (childHeadAbsoluteDay < forkAbsoluteDay)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidHead,
                "Child head day cannot be before the fork day.");
            return false;
        }

        if (childHeadAbsoluteDay == forkAbsoluteDay
            && forkRevision.HasValue == true
            && childHeadRevision < forkRevision.Value)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidHead,
                "Child head revision cannot be before the fork revision.");
            return false;
        }

        descriptor = new WorldTimelineDescriptor(
            childTimelineId,
            parent.TimelineId,
            forkAbsoluteDay,
            forkRevision,
            childHeadAbsoluteDay,
            childHeadRevision);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool TryAdvanceHead(
        long newHeadAbsoluteDay,
        WorldRevision newHeadRevision,
        out WorldTimelineDescriptor updated,
        out WorldTimelineFailure failure)
    {
        updated = null;

        if (newHeadAbsoluteDay < 0L)
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidAbsoluteDay,
                "Timeline head day cannot be negative.");
            return false;
        }

        if (newHeadAbsoluteDay < HeadAbsoluteDay
            || (newHeadAbsoluteDay == HeadAbsoluteDay && newHeadRevision < HeadRevision))
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.HeadRegression,
                "Timeline head cannot move backwards.");
            return false;
        }

        if (ForkAbsoluteDay.HasValue == true
            && (newHeadAbsoluteDay < ForkAbsoluteDay.Value
                || (newHeadAbsoluteDay == ForkAbsoluteDay.Value
                    && ForkRevision.HasValue == true
                    && newHeadRevision < ForkRevision.Value)))
        {
            failure = WorldTimelineFailure.Create(
                WorldTimelineFailureCode.InvalidHead,
                "Timeline head cannot move before its fork point.");
            return false;
        }

        updated = new WorldTimelineDescriptor(
            TimelineId,
            ParentTimelineId,
            ForkAbsoluteDay,
            ForkRevision,
            newHeadAbsoluteDay,
            newHeadRevision);
        failure = WorldTimelineFailure.None;
        return true;
    }

    public bool Equals(WorldTimelineDescriptor other)
    {
        return other != null
            && TimelineId == other.TimelineId
            && ParentTimelineId == other.ParentTimelineId
            && ForkAbsoluteDay == other.ForkAbsoluteDay
            && ForkRevision == other.ForkRevision
            && HeadAbsoluteDay == other.HeadAbsoluteDay
            && HeadRevision == other.HeadRevision;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldTimelineDescriptor);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = TimelineId.GetHashCode();
            hash = hash * 31 + (ParentTimelineId == null ? 0 : ParentTimelineId.GetHashCode());
            hash = hash * 31 + (ForkAbsoluteDay.HasValue ? ForkAbsoluteDay.Value.GetHashCode() : 0);
            hash = hash * 31 + (ForkRevision.HasValue ? ForkRevision.Value.GetHashCode() : 0);
            hash = hash * 31 + HeadAbsoluteDay.GetHashCode();
            return hash * 31 + HeadRevision.GetHashCode();
        }
    }
}

public sealed class WorldRestorePlan : IEquatable<WorldRestorePlan>
{
    public WorldTimelineId TimelineId { get; }
    public long TargetAbsoluteDay { get; }
    public WorldCheckpointDescriptor BaseCheckpoint { get; }
    public WorldRevision ReplayFromRevision => BaseCheckpoint.Revision;
    public long ReplayStartAbsoluteDay { get; }
    public long ReplayEndAbsoluteDay => TargetAbsoluteDay;
    public bool RequiresReplay => BaseCheckpoint.AbsoluteDay < TargetAbsoluteDay;

    public WorldRestorePlan(
        WorldTimelineId timelineId,
        long targetAbsoluteDay,
        WorldCheckpointDescriptor baseCheckpoint)
    {
        if (timelineId == null)
        {
            throw new ArgumentNullException(nameof(timelineId));
        }

        if (targetAbsoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(targetAbsoluteDay));
        }

        if (baseCheckpoint == null)
        {
            throw new ArgumentNullException(nameof(baseCheckpoint));
        }

        if (baseCheckpoint.TimelineId != timelineId)
        {
            throw new ArgumentException("Checkpoint belongs to another timeline.", nameof(baseCheckpoint));
        }

        if (baseCheckpoint.AbsoluteDay > targetAbsoluteDay)
        {
            throw new ArgumentException("Checkpoint cannot be after the restore target.", nameof(baseCheckpoint));
        }

        TimelineId = timelineId;
        TargetAbsoluteDay = targetAbsoluteDay;
        BaseCheckpoint = baseCheckpoint;
        ReplayStartAbsoluteDay = baseCheckpoint.AbsoluteDay < targetAbsoluteDay
            ? baseCheckpoint.AbsoluteDay + 1L
            : targetAbsoluteDay;
    }

    public bool Equals(WorldRestorePlan other)
    {
        return other != null
            && TimelineId == other.TimelineId
            && TargetAbsoluteDay == other.TargetAbsoluteDay
            && BaseCheckpoint.Equals(other.BaseCheckpoint)
            && ReplayStartAbsoluteDay == other.ReplayStartAbsoluteDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldRestorePlan);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return ((TimelineId.GetHashCode() * 31) + TargetAbsoluteDay.GetHashCode()) * 31
                + BaseCheckpoint.GetHashCode();
        }
    }
}

public sealed class WorldForkPlan : IEquatable<WorldForkPlan>
{
    public WorldTimelineDescriptor ParentTimeline { get; }
    public WorldTimelineId NewTimelineId { get; }
    public long ForkAbsoluteDay { get; }
    public WorldRevision? ForkRevision { get; }
    public WorldCheckpointDescriptor BaseCheckpoint { get; }
    public long ReplayStartAbsoluteDay { get; }
    public long ReplayEndAbsoluteDay => ForkAbsoluteDay;
    public bool RequiresReplay => BaseCheckpoint.AbsoluteDay < ForkAbsoluteDay;

    public WorldForkPlan(
        WorldTimelineDescriptor parentTimeline,
        WorldTimelineId newTimelineId,
        long forkAbsoluteDay,
        WorldRevision? forkRevision,
        WorldCheckpointDescriptor baseCheckpoint,
        long replayStartAbsoluteDay)
    {
        if (parentTimeline == null)
        {
            throw new ArgumentNullException(nameof(parentTimeline));
        }

        if (newTimelineId == null)
        {
            throw new ArgumentNullException(nameof(newTimelineId));
        }

        if (baseCheckpoint == null)
        {
            throw new ArgumentNullException(nameof(baseCheckpoint));
        }

        if (baseCheckpoint.TimelineId != parentTimeline.TimelineId)
        {
            throw new ArgumentException("Checkpoint belongs to another timeline.", nameof(baseCheckpoint));
        }

        if (forkAbsoluteDay < baseCheckpoint.AbsoluteDay || forkAbsoluteDay > parentTimeline.HeadAbsoluteDay)
        {
            throw new ArgumentOutOfRangeException(nameof(forkAbsoluteDay));
        }

        ParentTimeline = parentTimeline;
        NewTimelineId = newTimelineId;
        ForkAbsoluteDay = forkAbsoluteDay;
        ForkRevision = forkRevision;
        BaseCheckpoint = baseCheckpoint;
        ReplayStartAbsoluteDay = replayStartAbsoluteDay;
    }

    public bool Equals(WorldForkPlan other)
    {
        return other != null
            && ParentTimeline.Equals(other.ParentTimeline)
            && NewTimelineId == other.NewTimelineId
            && ForkAbsoluteDay == other.ForkAbsoluteDay
            && ForkRevision == other.ForkRevision
            && BaseCheckpoint.Equals(other.BaseCheckpoint)
            && ReplayStartAbsoluteDay == other.ReplayStartAbsoluteDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as WorldForkPlan);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = ParentTimeline.GetHashCode();
            hash = hash * 31 + NewTimelineId.GetHashCode();
            hash = hash * 31 + ForkAbsoluteDay.GetHashCode();
            hash = hash * 31 + (ForkRevision.HasValue ? ForkRevision.Value.GetHashCode() : 0);
            return hash * 31 + BaseCheckpoint.GetHashCode();
        }
    }
}

public sealed class HistoricalWorldStateQuery : IEquatable<HistoricalWorldStateQuery>
{
    public WorldTimelineId TimelineId { get; }
    public long AbsoluteDay { get; }

    public HistoricalWorldStateQuery(WorldTimelineId timelineId, long absoluteDay)
    {
        if (timelineId == null)
        {
            throw new ArgumentNullException(nameof(timelineId));
        }

        if (absoluteDay < 0L)
        {
            throw new ArgumentOutOfRangeException(nameof(absoluteDay));
        }

        TimelineId = timelineId;
        AbsoluteDay = absoluteDay;
    }

    public bool Equals(HistoricalWorldStateQuery other)
    {
        return other != null && TimelineId == other.TimelineId && AbsoluteDay == other.AbsoluteDay;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as HistoricalWorldStateQuery);
    }

    public override int GetHashCode()
    {
        return TimelineId.GetHashCode() * 31 + AbsoluteDay.GetHashCode();
    }
}

public interface IHistoricalWorldStateProvider
{
    bool TryCreateRestorePlan(
        HistoricalWorldStateQuery query,
        out WorldRestorePlan plan,
        out WorldTimelineFailure failure);
}
