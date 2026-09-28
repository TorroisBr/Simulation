using System;
using System.Globalization;
using System.Text;

/// <summary>Stable value payload for one trusted P11 ActorChoice timeline input.</summary>
public sealed class ActorChoiceTemporalCommand
{
    public string WorldCommandId { get; }
    public PersonId Actor { get; }
    public string ActionDefinitionId { get; }
    public WorldCommandOrigin Origin { get; }
    public WorldCommandAuthorityMode Authority { get; }

    public ActorChoiceTemporalCommand(string worldCommandId, PersonId actor, string actionDefinitionId,
        WorldCommandOrigin origin = WorldCommandOrigin.LocalPlayer,
        WorldCommandAuthorityMode authority = WorldCommandAuthorityMode.Request)
    {
        if (string.IsNullOrWhiteSpace(worldCommandId)) throw new ArgumentException("World command identity is required.", nameof(worldCommandId));
        if (actor == null) throw new ArgumentNullException(nameof(actor));
        if (string.IsNullOrWhiteSpace(actionDefinitionId)) throw new ArgumentException("Action definition identity is required.", nameof(actionDefinitionId));
        if (!Enum.IsDefined(typeof(WorldCommandOrigin), origin)) throw new ArgumentOutOfRangeException(nameof(origin));
        if (!Enum.IsDefined(typeof(WorldCommandAuthorityMode), authority)) throw new ArgumentOutOfRangeException(nameof(authority));
        WorldCommandId = worldCommandId;
        Actor = actor;
        ActionDefinitionId = actionDefinitionId;
        Origin = origin;
        Authority = authority;
    }

    /// <summary>Encodes the typed payload as a deterministic, reversible P18-A command-data string.</summary>
    public string Encode()
    {
        StringBuilder builder = new StringBuilder("actor-choice/v1|");
        AppendField(builder, WorldCommandId);
        AppendField(builder, Actor.Value);
        AppendField(builder, ActionDefinitionId);
        AppendField(builder, ((int)Origin).ToString(CultureInfo.InvariantCulture));
        AppendField(builder, ((int)Authority).ToString(CultureInfo.InvariantCulture));
        return builder.ToString();
    }

    internal static bool TryDecode(string encoded, out ActorChoiceTemporalCommand command)
    {
        command = null;
        const string prefix = "actor-choice/v1|";
        if (encoded == null || !encoded.StartsWith(prefix, StringComparison.Ordinal)) return false;
        int index = prefix.Length;
        if (!TryReadField(encoded, ref index, out string worldCommandId)
            || !TryReadField(encoded, ref index, out string actorValue)
            || !TryReadField(encoded, ref index, out string actionDefinitionId)
            || !TryReadField(encoded, ref index, out string originValue)
            || !TryReadField(encoded, ref index, out string authorityValue)
            || index != encoded.Length
            || !int.TryParse(originValue, NumberStyles.None, CultureInfo.InvariantCulture, out int originNumber)
            || !Enum.IsDefined(typeof(WorldCommandOrigin), originNumber)
            || (WorldCommandOrigin)originNumber != WorldCommandOrigin.LocalPlayer
            || !int.TryParse(authorityValue, NumberStyles.None, CultureInfo.InvariantCulture, out int authorityNumber)
            || !Enum.IsDefined(typeof(WorldCommandAuthorityMode), authorityNumber)
            || (WorldCommandAuthorityMode)authorityNumber != WorldCommandAuthorityMode.Request) return false;

        try
        {
            command = new ActorChoiceTemporalCommand(worldCommandId, new PersonId(actorValue), actionDefinitionId,
                (WorldCommandOrigin)originNumber, (WorldCommandAuthorityMode)authorityNumber);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static void AppendField(StringBuilder builder, string value)
    {
        builder.Append(value.Length.ToString(CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }

    private static bool TryReadField(string encoded, ref int index, out string value)
    {
        value = null;
        int separator = encoded.IndexOf(':', index);
        if (separator < index || !int.TryParse(encoded.Substring(index, separator - index),
            NumberStyles.None, CultureInfo.InvariantCulture, out int length) || length < 0) return false;
        index = separator + 1;
        if (length > encoded.Length - index) return false;
        value = encoded.Substring(index, length);
        index += length;
        return true;
    }
}

/// <summary>
/// Commits an accepted P18-A actor-choice input to the existing P11 command owner.
/// The timeline remains responsible for dispatch order and retry; P11 remains the
/// authority for retained command identity and terminal disposition.
/// </summary>
public sealed class ActorChoiceTemporalInputOwner : ITimelineInputOwner
{
    public const string CommandKind = "actor-choice/v1";

    private readonly ActorChoiceStore actorChoiceStore;
    private readonly string profileId;

    public ActorChoiceTemporalInputOwner(ActorChoiceStore actorChoiceStore, string profileId)
    {
        this.actorChoiceStore = actorChoiceStore ?? throw new ArgumentNullException(nameof(actorChoiceStore));
        if (string.IsNullOrWhiteSpace(profileId)) throw new ArgumentException("Profile identity is required.", nameof(profileId));
        this.profileId = profileId;
    }

    public bool TryPrepare(TimelineInputReference input, out ITimelineInputCommit prepared, out TimelineFailure failure)
    {
        prepared = null;
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (!string.Equals(input.CommandKind, CommandKind, StringComparison.Ordinal)
            || !ActorChoiceTemporalCommand.TryDecode(input.CommandData, out ActorChoiceTemporalCommand command))
        {
            failure = TimelineFailure.UnknownWorkKind;
            return false;
        }

        prepared = new CaptureCommit(actorChoiceStore, profileId, input, command);
        failure = TimelineFailure.None;
        return true;
    }

    private sealed class CaptureCommit : ITimelineInputCommit
    {
        private readonly ActorChoiceStore owner;
        private readonly string profileId;
        private readonly TimelineInputReference input;
        private readonly ActorChoiceTemporalCommand command;

        public CaptureCommit(ActorChoiceStore owner, string profileId, TimelineInputReference input,
            ActorChoiceTemporalCommand command)
        {
            this.owner = owner;
            this.profileId = profileId;
            this.input = input;
            this.command = command;
        }

        public System.Collections.Generic.IReadOnlyList<DueWorkReference> NewOwnerFacts =>
            Array.Empty<DueWorkReference>();

        public bool TryCommit(out TimelineFailure failure)
        {
            if (owner.TryCaptureTemporal(command.WorldCommandId, command.Actor, command.ActionDefinitionId,
                command.Origin, command.Authority, profileId, input, out _, out ActorChoiceStoreFailureCode storeFailure))
            {
                failure = TimelineFailure.None;
                return true;
            }

            failure = TimelineFailure.DispatchFailed;
            return false;
        }
    }
}
