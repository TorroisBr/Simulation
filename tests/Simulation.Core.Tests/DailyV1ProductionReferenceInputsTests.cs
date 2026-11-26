using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;

[TestFixture]
public sealed class DailyV1ProductionReferenceInputsTests
{
    private const string CampoVerdeDefinitionId = "CAMPO_VERDE";
    private const string TrigoDefinitionId = "TRIGO";

    [Test]
    public void ProductionProviderMatchesFrozenOracleBeforeAndAfterCoreAdmission()
    {
        DailyV1EffectiveDefinitions production = DailyV1LabSelectedReferenceInputs.CreateDefinitions();
        DailyV1EffectiveDefinitions oracle = DailyV1CapturedSelectedContentFixture.CreateDefinitions();

        AssertDefinitionsEqual(production, oracle, "before admission");

        StartedScenario productionStart = Start(production, "production-input-parity");
        StartedScenario oracleStart = Start(oracle, "frozen-oracle-parity");

        AssertDefinitionsEqual(
            productionStart.Session.Definitions, oracleStart.Session.Definitions, "after admission");
        Assert.That(productionStart.Session.ProfileFingerprint,
            Is.EqualTo(oracleStart.Session.ProfileFingerprint));
        Assert.That(CanonicalFingerprint(productionStart.Session.GenesisComposition.P9ManifestSnapshot),
            Is.EqualTo(CanonicalFingerprint(oracleStart.Session.GenesisComposition.P9ManifestSnapshot)),
            "The admitted P9 profile, manifest, stage ordering and authored geography identity must be equal.");
        Assert.That(RuntimeOwnerFingerprint(productionStart.Session),
            Is.EqualTo(RuntimeOwnerFingerprint(oracleStart.Session)),
            "The admitted City/NPC roster, exact owner links, complete 89-family P12 census, revisions, "
                + "Market rows and starting quantities must be equal.");
    }

    [Test]
    public void CampoVerdeTrigoStockChangesThroughTwoRealCoreDaysAndRepeatsDeterministically()
    {
        ProbeResult first = RunTwoDayProbe(DailyV1LabSelectedReferenceInputs.CreateDefinitions(), "stock-probe-a");
        ProbeResult repeated = RunTwoDayProbe(DailyV1LabSelectedReferenceInputs.CreateDefinitions(), "stock-probe-b");

        Assert.That(first.WorldId, Is.Not.EqualTo(repeated.WorldId),
            "The repeat must use a newly allocated WorldId rather than reuse the first runtime.");
        Assert.That(repeated.InitialStock, Is.EqualTo(first.InitialStock));
        Assert.That(repeated.Day1Stock, Is.EqualTo(first.Day1Stock));
        Assert.That(repeated.Day2Stock, Is.EqualTo(first.Day2Stock));
        Assert.That(repeated.TwoDayDelta, Is.EqualTo(first.TwoDayDelta));
    }

    private static void AssertDefinitionsEqual(
        DailyV1EffectiveDefinitions actual,
        DailyV1EffectiveDefinitions expected,
        string boundary)
    {
        Assert.That(actual, Is.Not.Null);
        Assert.That(expected, Is.Not.Null);
        Assert.That(actual.ContentFingerprint, Is.EqualTo(expected.ContentFingerprint),
            "Captured authored-value projection fingerprint differs " + boundary + ".");
        Assert.That(actual.ProfileContractIdentity, Is.EqualTo(expected.ProfileContractIdentity));
        Assert.That(actual.GeographyProfileContractIdentity,
            Is.EqualTo(expected.GeographyProfileContractIdentity));
        Assert.That(actual.GeographyStageIdentity, Is.EqualTo(expected.GeographyStageIdentity));
        Assert.That(actual.StageOrder, Is.EqualTo(expected.StageOrder));
        Assert.That(CanonicalFingerprint(actual), Is.EqualTo(CanonicalFingerprint(expected)),
            "The complete typed Daily-v1 graph, references, ordered values, and exact numeric values differ "
                + boundary + ".");
    }

    private static StartedScenario Start(DailyV1EffectiveDefinitions definitions, string label)
    {
        var application = new DailyV1Application();
        var host = new DailyV1ApplicationHost(
            label,
            new NoSaveSlotOperations(),
            stageCompleted: null,
            logOutput: null,
            dotNetIdentity: DotNetIdentity());

        Assert.That(application.TryStartDotNetOnExistingApplication(
            definitions,
            host,
            out DailyV1ApplicationStartPreparation preparation,
            out P12AResult persistenceResult,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(preparation, Is.Not.Null);
        Assert.That(preparation.Kind, Is.EqualTo(DailyV1ApplicationStartPreparationKind.GenesisPrepared));
        Assert.That(persistenceResult.Code, Is.EqualTo(P12AResultCode.NoSave));
        Assert.That(application.ActiveSession, Is.SameAs(preparation.CoreSession));
        Assert.That(preparation.CoreSession.Definitions, Is.SameAs(definitions));
        Assert.That(preparation.CoreSession.ExecutionBoundaryState.IsFactualReadWorldPublished, Is.True);
        return new StartedScenario(application, preparation.CoreSession);
    }

    private static ProbeResult RunTwoDayProbe(DailyV1EffectiveDefinitions definitions, string label)
    {
        StartedScenario started = Start(definitions, label);
        DailyV1ActiveRuntimeSession session = started.Session;
        DailyV1CityRuntimeOwners city = session.OwnerGraph.Cities.Single(
            candidate => candidate.Definition.DefinitionId == CampoVerdeDefinitionId);
        Assert.That(ReferenceEquals(city.Definition, definitions.Cities[CampoVerdeDefinitionId]), Is.True);
        Assert.That(ReferenceEquals(city.Market.Definitions, definitions), Is.True);
        DailyV1ItemDefinition trigo = definitions.Items[TrigoDefinitionId];

        long initialDay = session.SimulationTime.AbsoluteDay;
        long initialSequence = session.ExecutionBoundaryState.CompletedCoreSequence;
        int initial = ReadStock(city, trigo);
        int day1 = AdvanceAndReadStock(started.Application, session, city, trigo, initialDay + 1L, initialSequence + 1L);
        int day2 = AdvanceAndReadStock(started.Application, session, city, trigo, initialDay + 2L, initialSequence + 2L);
        long delta = (long)day2 - initial;

        TestContext.WriteLine(
            "Daily-v1 real Core stock probe: world={0}; city={1}; item={2}; day0={3}; day1={4}; day2={5}; signedTwoDayDelta={6}",
            session.WorldId, city.Definition.DefinitionId, trigo.DefinitionId, initial, day1, day2, delta);

        Assert.That(delta, Is.Not.Zero,
            "The ordinary admitted Daily-v1 runtime must produce a real nonzero two-day Campo Verde/TRIGO stock delta; "
                + "do not synthesize or force a Market mutation for this probe.");

        return new ProbeResult(session.WorldId.ToString(), initial, day1, day2, delta);
    }

    private static int AdvanceAndReadStock(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession expectedSession,
        DailyV1CityRuntimeOwners expectedCity,
        DailyV1ItemDefinition item,
        long expectedDay,
        long expectedSequence)
    {
        Assert.That(application.TryAdvanceDay(out SimulationRuntimeAdvanceFailure failure), Is.True,
            "The normal Core daily lifecycle must advance without a Unity runtime. Failure=" + failure);
        Assert.That(application.ActiveSession, Is.SameAs(expectedSession));
        Assert.That(expectedSession.SimulationTime.AbsoluteDay, Is.EqualTo(expectedDay));
        Assert.That(expectedSession.ExecutionBoundaryState.CompletedCoreSequence,
            Is.EqualTo(expectedSequence));
        Assert.That(expectedSession.ExecutionBoundaryState.ActiveRuntimeOperationCount, Is.Zero,
            "The complete daily operation must return to the existing idle/capture boundary.");
        Assert.That(expectedSession.OwnerGraph.Cities.Single(
            candidate => ReferenceEquals(candidate, expectedCity)), Is.SameAs(expectedCity));
        return ReadStock(expectedCity, item);
    }

    private static int ReadStock(DailyV1CityRuntimeOwners city, DailyV1ItemDefinition item)
    {
        DailyV1MarketItemState row = city.Market.GetItem(item);
        Assert.That(row, Is.Not.Null,
            "The authored Market row must remain present; absence is not interchangeable with zero stock.");
        Assert.That(ReferenceEquals(row.Item, item), Is.True,
            "The Market row must retain the exact canonical admitted Item definition identity.");
        return row.Amount;
    }

    private static string RuntimeOwnerFingerprint(DailyV1ActiveRuntimeSession session)
    {
        Assert.That(DailyV1CoreSelectedOwnerCensusProviderComposition.TryCreateForApplicationStart(
            session,
            out DailyV1CoreSelectedOwnerCensusProviderComposition census,
            out string diagnostic), Is.True, diagnostic);
        Assert.That(census.Registrations, Has.Count.EqualTo(299),
            "The admitted reference input profile must retain the complete accepted P12 census (299 rows across its 89-family vector).");

        var value = new StringBuilder();
        foreach (DailyV1CityRuntimeOwners city in session.OwnerGraph.Cities)
        {
            Assert.That(ReferenceEquals(city.Definitions, session.Definitions), Is.True);
            Assert.That(session.Definitions.Cities.TryGetValue(
                city.Definition.DefinitionId, out DailyV1CityDefinition admittedCity), Is.True);
            Assert.That(ReferenceEquals(city.Definition, admittedCity), Is.True);
            Assert.That(ReferenceEquals(city.Market.Definitions, session.Definitions), Is.True);
            foreach (DailyV1MarketItemState row in city.Market.Items)
            {
                Assert.That(session.Definitions.Items.TryGetValue(
                    row.Item.DefinitionId, out DailyV1ItemDefinition admittedItem), Is.True);
                Assert.That(ReferenceEquals(row.Item, admittedItem), Is.True,
                    "Every admitted Market stock row must retain its exact typed Item identity.");
            }
        }
        foreach (DailyV1NpcRuntimeOwners npc in session.OwnerGraph.Npcs)
        {
            Assert.That(ReferenceEquals(npc.Definitions, session.Definitions), Is.True);
            Assert.That(session.Definitions.Npcs.TryGetValue(
                npc.Definition.DefinitionId, out DailyV1NpcDefinition admittedNpc), Is.True);
            Assert.That(ReferenceEquals(npc.Definition, admittedNpc), Is.True,
                "Every admitted actor must retain its exact typed Npc identity.");
        }

        Append(value, session.OwnerGraph.Cities.Select(city => new object[]
        {
            city.RuntimeId,
            city.LocationRuntimeId,
            city.Definition.DefinitionId,
            city.Market.Revision,
            city.NpcPresence.OrderedNpcRuntimeIds.ToArray(),
            city.Market.Items.Select(row => new object[]
            {
                row.Item.DefinitionId,
                row.Amount,
                row.DesiredAmount,
                BitConverter.ToInt32(BitConverter.GetBytes(row.CurrentPrice), 0)
            }).ToArray()
        }).ToArray(), new HashSet<object>(ReferenceComparer.Instance));
        Append(value, session.OwnerGraph.Npcs.Select(npc => new object[]
        {
            npc.RuntimeId,
            npc.Definition.DefinitionId
        }).ToArray(), new HashSet<object>(ReferenceComparer.Instance));
        Append(value, census.Registrations.OrderBy(registration => registration.SectionId, StringComparer.Ordinal)
            .Select(registration =>
            {
                OwnerSectionCensusWitness witness = registration.Provider.GetCurrentCensus();
                Assert.That(witness.SectionId, Is.EqualTo(registration.SectionId));
                if (registration.HasBaseline)
                {
                    Assert.That(ReferenceEquals(witness.OwnerInstanceIdentity,
                        registration.BaselineOwnerInstanceIdentity), Is.True,
                        "Each baseline witness must remain bound to its exact admitted owner instance.");
                    Assert.That(witness.Cardinality, Is.EqualTo(registration.BaselineCardinality));
                    Assert.That(witness.Revision, Is.EqualTo(registration.BaselineRevision));
                }
                return new object[]
                {
                    registration.SectionId,
                    registration.SchemaVersion,
                    registration.Role,
                    registration.HasBaseline,
                    witness.Cardinality,
                    witness.Revision
                };
            }).ToArray(), new HashSet<object>(ReferenceComparer.Instance));
        return Sha256(value.ToString());
    }

    private static string CanonicalFingerprint(object value)
    {
        var canonical = new StringBuilder();
        Append(canonical, value, new HashSet<object>(ReferenceComparer.Instance));
        return Sha256(canonical.ToString());
    }

    private static string Sha256(string value)
    {
        using (SHA256 sha = SHA256.Create())
        {
            byte[] digest = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
            var result = new StringBuilder(digest.Length * 2);
            foreach (byte item in digest) result.Append(item.ToString("x2", CultureInfo.InvariantCulture));
            return result.ToString();
        }
    }

    private static void Append(StringBuilder output, object value, HashSet<object> active)
    {
        if (value == null)
        {
            output.Append("null;");
            return;
        }

        Type type = value.GetType();
        output.Append(type.FullName).Append(':');
        if (value is string text)
        {
            output.Append(text.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(text).Append(';');
            return;
        }
        if (type.IsEnum)
        {
            output.Append(Convert.ToInt64(value, CultureInfo.InvariantCulture)
                .ToString(CultureInfo.InvariantCulture)).Append(';');
            return;
        }
        if (value is float single)
        {
            output.Append(BitConverter.ToInt32(BitConverter.GetBytes(single), 0)
                .ToString("x8", CultureInfo.InvariantCulture)).Append(';');
            return;
        }
        if (value is double real)
        {
            output.Append(BitConverter.DoubleToInt64Bits(real)
                .ToString("x16", CultureInfo.InvariantCulture)).Append(';');
            return;
        }
        if (type.IsPrimitive || value is decimal || value is Guid)
        {
            output.Append(Convert.ToString(value, CultureInfo.InvariantCulture)).Append(';');
            return;
        }

        bool trackReference = !type.IsValueType;
        if (trackReference && !active.Add(value))
        {
            output.Append("cycle;");
            return;
        }

        try
        {
            if (value is IDictionary dictionary)
            {
                var entries = new List<DictionaryEntry>();
                foreach (DictionaryEntry entry in dictionary) entries.Add(entry);
                entries.Sort((left, right) => StringComparer.Ordinal.Compare(
                    CanonicalFingerprint(left.Key), CanonicalFingerprint(right.Key)));
                output.Append("dict[").Append(entries.Count).Append(']');
                foreach (DictionaryEntry entry in entries)
                {
                    Append(output, entry.Key, active);
                    Append(output, entry.Value, active);
                }
                return;
            }

            if (value is IEnumerable sequence)
            {
                int count = 0;
                foreach (object item in sequence)
                {
                    Append(output, item, active);
                    count++;
                }
                output.Append("sequence-count=").Append(count).Append(';');
                return;
            }

            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                .OrderBy(property => property.Name, StringComparer.Ordinal)
                .ToArray();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                .OrderBy(field => field.Name, StringComparer.Ordinal)
                .ToArray();
            if (properties.Length == 0 && fields.Length == 0)
                throw new InvalidOperationException("Selected input graph contains an opaque value: " + type.FullName);

            foreach (PropertyInfo property in properties)
            {
                output.Append("property=").Append(property.Name).Append(';');
                Append(output, property.GetValue(value), active);
            }
            foreach (FieldInfo field in fields)
            {
                output.Append("field=").Append(field.Name).Append(';');
                Append(output, field.GetValue(value), active);
            }
        }
        finally
        {
            if (trackReference) active.Remove(value);
        }
    }

    private static DailyV1HostEnvironmentIdentity DotNetIdentity() =>
        new DailyV1HostEnvironmentIdentity(
            1,
            ".NET 8.0",
            "8.0.1.0",
            "Windows",
            "win-x64",
            "x64",
            "net8.0",
            new[]
            {
                new P12AAssemblyIdentityV2("Simulation.Core", "1.0.0.0",
                    "00000000-0000-0000-0000-000000000001")
            },
            "numeric",
            1);

    private sealed class NoSaveSlotOperations : IAtomicFileSlotOperations
    {
        public void CreateDirectory(string path) => throw new NotSupportedException();
        public bool Exists(string path) => false;
        public System.IO.Stream CreateNewTemporary(string path) => throw new NotSupportedException();
        public System.IO.Stream OpenRead(string path) => throw new System.IO.FileNotFoundException();
        public void FlushToDisk(System.IO.Stream stream) => throw new NotSupportedException();
        public void Replace(string source, string destination) => throw new NotSupportedException();
        public void Move(string source, string destination) => throw new NotSupportedException();
        public void Delete(string path) => throw new NotSupportedException();
    }

    private sealed class StartedScenario
    {
        internal StartedScenario(DailyV1Application application, DailyV1ActiveRuntimeSession session)
        {
            Application = application;
            Session = session;
        }

        internal DailyV1Application Application { get; }
        internal DailyV1ActiveRuntimeSession Session { get; }
    }

    private sealed class ProbeResult
    {
        internal ProbeResult(string worldId, int initialStock, int day1Stock, int day2Stock, long twoDayDelta)
        {
            WorldId = worldId;
            InitialStock = initialStock;
            Day1Stock = day1Stock;
            Day2Stock = day2Stock;
            TwoDayDelta = twoDayDelta;
        }

        internal string WorldId { get; }
        internal int InitialStock { get; }
        internal int Day1Stock { get; }
        internal int Day2Stock { get; }
        internal long TwoDayDelta { get; }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceComparer Instance = new ReferenceComparer();
        public new bool Equals(object left, object right) => ReferenceEquals(left, right);
        public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
}
