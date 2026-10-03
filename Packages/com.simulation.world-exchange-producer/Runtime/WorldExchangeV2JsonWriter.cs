using System;
using System.Text;

namespace Simulation.WorldExchangeProducer
{
    /// <summary>Stable compact JSON serialization for this producer's schema-v2 projection.</summary>
    public static class WorldExchangeV2JsonWriter
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        public static bool TrySerialize(
            WorldExchangeV2Artifact artifact,
            out byte[] utf8Json,
            out string failureCode,
            out string failureMessage)
        {
            utf8Json = null;
            if (!WorldExchangeV2Artifact.Validate(artifact, out failureCode, out failureMessage))
                return false;

            try
            {
                StringBuilder json = new StringBuilder(256 + artifact.Factions.Count * 96);
                json.Append('{');
                json.Append("\"cities\":[],");
                json.Append("\"collectionCoverage\":{");
                json.Append("\"cities\":\"UNSUPPORTED\",");
                json.Append("\"factions\":");
                AppendQuoted(json, CoverageName(artifact.GetCoverage(WorldExchangeCollection.Factions)));
                json.Append(',');
                json.Append("\"historicalEvents\":\"UNSUPPORTED\",");
                json.Append("\"institutions\":\"UNSUPPORTED\",");
                json.Append("\"items\":\"UNSUPPORTED\",");
                json.Append("\"locations\":\"UNSUPPORTED\",");
                json.Append("\"organizations\":\"UNSUPPORTED\",");
                json.Append("\"people\":\"UNSUPPORTED\",");
                json.Append("\"relationships\":\"UNSUPPORTED\"},");
                json.Append("\"factions\":[");
                for (int index = 0; index < artifact.Factions.Count; index++)
                {
                    if (index != 0) json.Append(',');
                    WorldExchangeFaction faction = artifact.Factions[index];
                    json.Append('{');
                    json.Append("\"id\":");
                    AppendQuoted(json, faction.Id);
                    if (faction.Name != null)
                    {
                        json.Append(',');
                        json.Append("\"name\":");
                        AppendQuoted(json, faction.Name);
                    }
                    json.Append('}');
                }
                json.Append("],\"historicalEvents\":[],\"institutions\":[],\"items\":[],\"locations\":[],\"organizations\":[],\"people\":[],\"relationships\":[],\"schemaVersion\":2,\"world\":{\"id\":");
                AppendQuoted(json, artifact.WorldId);
                json.Append("}}");
                utf8Json = StrictUtf8.GetBytes(json.ToString());
                failureCode = null;
                failureMessage = null;
                return true;
            }
            catch (EncoderFallbackException)
            {
                failureCode = "serialization.invalid-utf8";
                failureMessage = "Artifact text cannot be encoded as valid UTF-8.";
                return false;
            }
            catch (Exception)
            {
                failureCode = "serialization.failed";
                failureMessage = "Artifact serialization failed.";
                return false;
            }
        }

        private static string CoverageName(WorldExchangeCoverageStatus status)
        {
            switch (status)
            {
                case WorldExchangeCoverageStatus.Included: return "INCLUDED";
                case WorldExchangeCoverageStatus.KnownEmpty: return "KNOWN_EMPTY";
                case WorldExchangeCoverageStatus.Unsupported: return "UNSUPPORTED";
                case WorldExchangeCoverageStatus.NotIncluded: return "NOT_INCLUDED";
                default: throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        private static void AppendQuoted(StringBuilder output, string value)
        {
            output.Append('"');
            for (int index = 0; index < value.Length; index++)
            {
                char current = value[index];
                switch (current)
                {
                    case '"': output.Append("\\\""); break;
                    case '\\': output.Append("\\\\"); break;
                    case '\b': output.Append("\\b"); break;
                    case '\f': output.Append("\\f"); break;
                    case '\n': output.Append("\\n"); break;
                    case '\r': output.Append("\\r"); break;
                    case '\t': output.Append("\\t"); break;
                    default:
                        if (current < 0x20)
                        {
                            output.Append("\\u");
                            output.Append(((int)current).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            output.Append(current);
                        }
                        break;
                }
            }
            output.Append('"');
        }
    }
}
