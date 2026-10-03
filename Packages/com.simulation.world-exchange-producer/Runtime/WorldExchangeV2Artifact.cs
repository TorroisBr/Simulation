using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Simulation.WorldExchangeProducer
{
    public enum WorldExchangeCollection
    {
        People,
        Cities,
        Locations,
        Organizations,
        Institutions,
        Factions,
        Items,
        HistoricalEvents,
        Relationships
    }

    public enum WorldExchangeCoverageStatus
    {
        Included,
        KnownEmpty,
        Unsupported,
        NotIncluded
    }

    /// <summary>Copied source values required to map one Faction without membership claims.</summary>
    public sealed class FactionProjectionInput
    {
        public FactionProjectionInput(string sourceId, string displayName)
        {
            SourceId = sourceId;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
        }

        public string SourceId { get; }
        public string DisplayName { get; }
    }

    /// <summary>The only Faction fields supported by the first World Exchange producer.</summary>
    public sealed class WorldExchangeFaction
    {
        internal WorldExchangeFaction(string id, string name)
        {
            Id = id;
            Name = name;
        }

        public string Id { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Immutable schema-v2 World Exchange projection for the currently supported
    /// whole-World source set: World identity and complete Faction facts.
    /// </summary>
    public sealed class WorldExchangeV2Artifact
    {
        private static readonly WorldExchangeCollection[] AllCollections =
        {
            WorldExchangeCollection.People,
            WorldExchangeCollection.Cities,
            WorldExchangeCollection.Locations,
            WorldExchangeCollection.Organizations,
            WorldExchangeCollection.Institutions,
            WorldExchangeCollection.Factions,
            WorldExchangeCollection.Items,
            WorldExchangeCollection.HistoricalEvents,
            WorldExchangeCollection.Relationships
        };

        private readonly ReadOnlyCollection<WorldExchangeFaction> factions;
        private readonly ReadOnlyDictionary<WorldExchangeCollection, WorldExchangeCoverageStatus> coverage;

        private WorldExchangeV2Artifact(
            string worldId,
            IEnumerable<WorldExchangeFaction> factions,
            WorldExchangeCoverageStatus factionCoverage)
        {
            WorldId = worldId;
            this.factions = new List<WorldExchangeFaction>(factions).AsReadOnly();

            Dictionary<WorldExchangeCollection, WorldExchangeCoverageStatus> copiedCoverage =
                new Dictionary<WorldExchangeCollection, WorldExchangeCoverageStatus>();
            foreach (WorldExchangeCollection collection in AllCollections)
            {
                copiedCoverage.Add(
                    collection,
                    collection == WorldExchangeCollection.Factions
                        ? factionCoverage
                        : WorldExchangeCoverageStatus.Unsupported);
            }
            coverage = new ReadOnlyDictionary<WorldExchangeCollection, WorldExchangeCoverageStatus>(copiedCoverage);
        }

        public int SchemaVersion => 2;
        public string WorldId { get; }
        public IReadOnlyList<WorldExchangeFaction> Factions => factions;
        public IReadOnlyDictionary<WorldExchangeCollection, WorldExchangeCoverageStatus> CollectionCoverage => coverage;

        public WorldExchangeCoverageStatus GetCoverage(WorldExchangeCollection collection)
        {
            if (!coverage.TryGetValue(collection, out WorldExchangeCoverageStatus status))
                throw new ArgumentOutOfRangeException(nameof(collection));
            return status;
        }

        /// <summary>Copies, validates, maps, and deterministically orders FR-C Faction source facts.</summary>
        public static bool TryCreate(
            string worldId,
            IEnumerable<FactionProjectionInput> sourceFactions,
            out WorldExchangeV2Artifact artifact,
            out string failureCode,
            out string failureMessage)
        {
            artifact = null;
            failureCode = null;
            failureMessage = null;
            if (!IsCanonicalWorldId(worldId))
                return Fail("world-id.invalid", "World identity is not in canonical world:<32 lowercase hex> form.", out failureCode, out failureMessage);
            if (sourceFactions == null)
                return Fail("factions.source-null", "Faction source facts are required.", out failureCode, out failureMessage);

            List<FactionProjectionInput> orderedSources = new List<FactionProjectionInput>();
            HashSet<string> sourceIds = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (FactionProjectionInput source in sourceFactions)
                {
                    if (source == null || string.IsNullOrWhiteSpace(source.SourceId))
                        return Fail("faction-id.invalid", "Every Faction requires a non-empty source ID.", out failureCode, out failureMessage);
                    StrictUtf8.GetByteCount(source.SourceId);
                    if (!sourceIds.Add(source.SourceId))
                        return Fail("faction-id.duplicate", "Faction source IDs must be unique.", out failureCode, out failureMessage);
                    if (source.DisplayName != null)
                        StrictUtf8.GetByteCount(source.DisplayName);
                    orderedSources.Add(source);
                }
            }
            catch (EncoderFallbackException)
            {
                return Fail("faction-text.invalid-utf8", "Faction identifiers and names must contain valid Unicode text.", out failureCode, out failureMessage);
            }
            catch (Exception)
            {
                return Fail("factions.source-invalid", "Faction source facts could not be copied.", out failureCode, out failureMessage);
            }

            orderedSources.Sort((left, right) => StringComparer.Ordinal.Compare(left.SourceId, right.SourceId));
            List<WorldExchangeFaction> mappedFactions = new List<WorldExchangeFaction>(orderedSources.Count);
            HashSet<string> mappedIds = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                foreach (FactionProjectionInput source in orderedSources)
                {
                    string mappedId = MapFactionId(source.SourceId);
                    if (!mappedIds.Add(mappedId))
                        return Fail("faction-id.mapped-duplicate", "Mapped Faction IDs must be unique.", out failureCode, out failureMessage);
                    mappedFactions.Add(new WorldExchangeFaction(mappedId, source.DisplayName));
                }
            }
            catch (EncoderFallbackException)
            {
                return Fail("faction-id.invalid-utf8", "Faction identifiers must contain valid UTF-8 text.", out failureCode, out failureMessage);
            }

            WorldExchangeCoverageStatus coverage = mappedFactions.Count == 0
                ? WorldExchangeCoverageStatus.KnownEmpty
                : WorldExchangeCoverageStatus.Included;
            artifact = new WorldExchangeV2Artifact(worldId, mappedFactions, coverage);
            if (!Validate(artifact, out failureCode, out failureMessage))
            {
                artifact = null;
                return false;
            }
            return true;
        }

        public static string MapFactionId(string sourceId)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
                throw new ArgumentException("A non-empty Faction source ID is required.", nameof(sourceId));
            byte[] utf8 = StrictUtf8.GetBytes(sourceId);
            return "sim:faction:" + Convert.ToBase64String(utf8)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public static bool Validate(WorldExchangeV2Artifact artifact, out string failureCode, out string failureMessage)
        {
            failureCode = null;
            failureMessage = null;
            if (artifact == null)
                return Fail("artifact.null", "A World Exchange artifact is required.", out failureCode, out failureMessage);
            if (artifact.SchemaVersion != 2 || !IsCanonicalWorldId(artifact.WorldId))
                return Fail("artifact.identity-invalid", "The artifact must use schema v2 and a canonical World identity.", out failureCode, out failureMessage);
            if (artifact.CollectionCoverage == null || artifact.CollectionCoverage.Count != AllCollections.Length)
                return Fail("coverage.incomplete", "Schema v2 requires exactly nine collection coverage entries.", out failureCode, out failureMessage);

            foreach (WorldExchangeCollection collection in AllCollections)
            {
                if (!artifact.CollectionCoverage.TryGetValue(collection, out WorldExchangeCoverageStatus status)
                    || !Enum.IsDefined(typeof(WorldExchangeCoverageStatus), status))
                    return Fail("coverage.invalid", "Every World Exchange collection requires a known coverage status.", out failureCode, out failureMessage);
                bool hasFactions = collection == WorldExchangeCollection.Factions && artifact.Factions.Count > 0;
                if ((status == WorldExchangeCoverageStatus.Included) != hasFactions
                    && (collection == WorldExchangeCollection.Factions
                        || status == WorldExchangeCoverageStatus.Included))
                    return Fail("coverage.contradictory", "Coverage status contradicts the collection array.", out failureCode, out failureMessage);
                if (collection != WorldExchangeCollection.Factions
                    && status != WorldExchangeCoverageStatus.Unsupported)
                    return Fail("coverage.unsupported-collection", "This producer only supports complete Faction projection.", out failureCode, out failureMessage);
            }

            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal) { artifact.WorldId };
            foreach (WorldExchangeFaction faction in artifact.Factions)
            {
                if (faction == null || string.IsNullOrWhiteSpace(faction.Id)
                    || !faction.Id.StartsWith("sim:faction:", StringComparison.Ordinal)
                    || (faction.Name != null && string.IsNullOrWhiteSpace(faction.Name)))
                    return Fail("faction.entity-invalid", "A mapped Faction entity has invalid required or optional values.", out failureCode, out failureMessage);
                if (!ids.Add(faction.Id))
                    return Fail("entity-id.duplicate", "World Exchange entity IDs must be unique within the artifact.", out failureCode, out failureMessage);
            }

            WorldExchangeCoverageStatus factionCoverage = artifact.GetCoverage(WorldExchangeCollection.Factions);
            if ((artifact.Factions.Count == 0 && factionCoverage != WorldExchangeCoverageStatus.KnownEmpty)
                || (artifact.Factions.Count > 0 && factionCoverage != WorldExchangeCoverageStatus.Included))
                return Fail("coverage.faction-incomplete", "Complete Faction coverage must be INCLUDED or KNOWN_EMPTY according to its actual rows.", out failureCode, out failureMessage);
            return true;
        }

        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private static bool IsCanonicalWorldId(string value)
        {
            if (value == null || value.Length != 38 || !value.StartsWith("world:", StringComparison.Ordinal))
                return false;
            for (int index = 6; index < value.Length; index++)
            {
                char current = value[index];
                if (!((current >= '0' && current <= '9') || (current >= 'a' && current <= 'f')))
                    return false;
            }
            return !string.Equals(value.Substring(6), "00000000000000000000000000000000", StringComparison.Ordinal);
        }

        private static bool Fail(string code, string message, out string failureCode, out string failureMessage)
        {
            failureCode = code;
            failureMessage = message;
            return false;
        }
    }
}
