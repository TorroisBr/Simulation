using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

public sealed class RuntimeStructureTruthTests
{
    [Test]
    public void CreatesOneExplicitInertStructureAtAnExistingLocationAndRetainsExactOrder()
    {
        SpatialAuthorityStore spatial = CreateSpatialAuthority();
        StructureStore store = new StructureStore(spatial);
        StructureRecord laterId = CreateRecord("structure-z", "location-a", 1L, 0L);

        Assert.That(TryCreateStructure(store, laterId, 1L, true, out StructureStoreFailure firstFailure), Is.True, firstFailure.ToString());
        Assert.That(store.Revision, Is.EqualTo(1L));
        Assert.That(store.Records.Select(value => value.Id.Value), Is.EqualTo(new[] { "structure-z" }));
        Assert.That(store.Records.Single().Definition,
            Is.EqualTo(StructureDefinitionReference.P15AProving));
        Assert.That(store.Records.Single().LocationId,
            Is.EqualTo(new LocationId("location-a")));
        Assert.That(store.Records.Single().CreatedAtBoundary, Is.EqualTo(1L));
        Assert.That(store.Records.Single().CreationOrder, Is.EqualTo(0L));
        Assert.That(store.ValidateInvariants(1L).IsValid, Is.True);
        Assert.That(spatial.LocationCount, Is.EqualTo(1));
        Assert.That(spatial.Revision, Is.EqualTo(2L));

        StructureStoreSemanticState state = store.CaptureSemanticState();
        Assert.That(state.Revision, Is.EqualTo(1L));
        Assert.That(state.Records.Select(value => value.Id.Value), Is.EqualTo(new[] { "structure-z" }));
    }

    [Test]
    public void RejectsPrePublicationWrongBoundaryUnknownLocationAndIncompatibleDefinitionWithoutMutation()
    {
        StructureStore store = new StructureStore(CreateSpatialAuthority());
        AssertRejectedUnchanged(store, CreateRecord("structure-a", "location-a", 1L, 0L), 1L, false,
            StructureStoreFailureCode.InitialPublicationIncomplete);
        AssertRejectedUnchanged(store, CreateRecord("structure-a", "location-a", 0L, 0L), 0L, true,
            StructureStoreFailureCode.InvalidBoundary);
        AssertRejectedUnchanged(store, CreateRecord("structure-a", "missing", 1L, 0L), 1L, true,
            StructureStoreFailureCode.LocationNotRegistered);
        AssertRejectedUnchanged(store,
            new StructureRecord(new StructureId("structure-a"), new StructureDefinitionReference("other", "v1"),
                new LocationId("location-a"), 1L, 0L),
            1L, true, StructureStoreFailureCode.InvalidDefinition);
    }

    [Test]
    public void RejectsDuplicateIdentityAndSecondCreationWithNoStateChange()
    {
        StructureStore store = new StructureStore(CreateSpatialAuthority());
        Assert.That(TryCreateStructure(store, CreateRecord("structure-a", "location-a", 1L, 0L), 1L, true, out _), Is.True);

        AssertRejectedUnchanged(store, CreateRecord("structure-a", "location-a", 1L, 1L), 1L, true,
            StructureStoreFailureCode.DuplicateStructureId);
        AssertRejectedUnchanged(store, CreateRecord("structure-b", "location-a", 1L, 0L), 1L, true,
            StructureStoreFailureCode.StructureLimitReached);
        Assert.That(store.Count, Is.EqualTo(1));
    }

    [Test]
    public void FaultedGuardAndRevisionOverflowRejectWithoutChangingStructureState()
    {
        StructureStore guarded = new StructureStore(CreateSpatialAuthority());
        Type guardType = typeof(StructureStore).Assembly.GetType("AuthoritativeMutationGuard");
        Assert.That(guardType, Is.Not.Null);
        object guard = Activator.CreateInstance(guardType, true);
        MethodInfo bind = typeof(StructureStore).GetMethod("TryBindMutationGuard", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That((bool)bind.Invoke(guarded, new[] { guard }), Is.True);
        MethodInfo markFaulted = guardType.GetMethod("MarkFaulted", BindingFlags.Instance | BindingFlags.NonPublic);
        Type reasonType = typeof(StructureStore).Assembly.GetType("AuthoritativeMutationFaultReason");
        object reason = Enum.Parse(reasonType, "RollbackRestoreFailed");
        markFaulted.Invoke(guard, new[] { reason });
        AssertRejectedUnchanged(guarded, CreateRecord("structure-a", "location-a", 1L, 0L), 1L, true,
            StructureStoreFailureCode.RuntimeFaulted);

        StructureStore overflow = new StructureStore(CreateSpatialAuthority());
        FieldInfo revision = typeof(StructureStore).GetField("revision", BindingFlags.Instance | BindingFlags.NonPublic);
        revision.SetValue(overflow, long.MaxValue);
        AssertRejectedUnchanged(overflow, CreateRecord("structure-a", "location-a", 1L, 0L), 1L, true,
            StructureStoreFailureCode.RevisionOverflow);
    }

    [Test]
    public void ClonePreservesIdentityDefinitionLocationBoundaryOrderAndRevision()
    {
        StructureStore source = new StructureStore(CreateSpatialAuthority());
        Assert.That(TryCreateStructure(source, CreateRecord("structure-b", "location-a", 1L, 4L), 1L, true, out _), Is.True);

        StructureStore clone = source.Clone(CreateSpatialAuthority());
        Assert.That(clone, Is.Not.SameAs(source));
        Assert.That(clone.CaptureSemanticState().Revision, Is.EqualTo(source.CaptureSemanticState().Revision));
        Assert.That(clone.Records.Select(value => new
        {
            Id = value.Id.Value,
            Definition = value.Definition.ToString(),
            Location = value.LocationId.Value,
            value.CreatedAtBoundary,
            value.CreationOrder
        }), Is.EqualTo(source.Records.Select(value => new
        {
            Id = value.Id.Value,
            Definition = value.Definition.ToString(),
            Location = value.LocationId.Value,
            value.CreatedAtBoundary,
            value.CreationOrder
        })));
        Assert.That(clone.ValidateInvariants(1L).IsValid, Is.True);
    }

    [Test]
    public void UnityDailyProfileProducesNoStoreAndRejectsTheDedicatedInjectionPath()
    {
        SimulationRuntime dailyRuntime = new SimulationRuntime(
            new SimulationTime(0L),
            cities: null,
            npcRuntimes: null,
            economyEnabled: false,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1());
        Assert.That(dailyRuntime.StructureStore, Is.Null);

        SpatialAuthorityStore spatial = CreateSpatialAuthority();
        StructureStore populatedStore = new StructureStore(spatial);
        Assert.That(TryCreateStructure(populatedStore,
            CreateRecord("unsupported-daily-structure", "location-a", 1L, 0L),
            1L,
            true,
            out StructureStoreFailure populatedFailure), Is.True, populatedFailure.ToString());
        Assert.Throws<ArgumentException>(() => new SimulationRuntime(
            new SimulationTime(0L),
            cities: null,
            npcRuntimes: null,
            economyEnabled: false,
            spatialAuthorityStore: spatial,
            runtimeAdmissionContext: SimulationRuntimeAdmissionContext.CaptureUnityBootstrapDailyV1(),
            compositionProfile: SimulationRuntimeCompositionProfile.P15AProvingStructure,
            structureStore: populatedStore));

        // TesteSimulacao remains the normal Unity daily factory and cannot
        // select or pass the proving owner.
        AssertNoStructureStoreSurface(typeof(TesteSimulacao));
    }

    [Test]
    public void SeparateP15ACompositionPublishesEmptyStoreAndAllowsOneStructureAfterFirstBoundary()
    {
        P15AProvingStructureComposition profile = P15AProvingStructureComposition.Compose(
            new SimulationTime(12L),
            CreateSpatialAuthority());
        Assert.That(profile.Runtime.StructureStore, Is.SameAs(profile.StructureStore));
        Assert.That(profile.Runtime.Cities, Is.Empty);
        Assert.That(profile.StructureStore.Count, Is.Zero);
        Assert.That(typeof(StructureStore).GetMethod(
            "TryCreateStructure", BindingFlags.Instance | BindingFlags.Public), Is.Null,
            "The owner does not expose a public mutation accepting caller-supplied boundary/publication facts.");

        Assert.That(profile.TryCreateStructure(
            new StructureId("structure-p15a"), new LocationId("location-a"),
            out StructureStoreFailure beforeFirstBoundary), Is.False);
        Assert.That(beforeFirstBoundary.Code, Is.EqualTo(StructureStoreFailureCode.InvalidBoundary));
        Assert.That(profile.StructureStore.Revision, Is.Zero);

        profile.Runtime.AdvanceDay();
        Assert.That(profile.Runtime.CurrentDay, Is.EqualTo(13L));
        Assert.That(profile.TryCreateStructure(
            new StructureId("structure-p15a"), new LocationId("location-a"),
            out StructureStoreFailure created), Is.True, created.ToString());
        Assert.That(profile.StructureStore.Records.Single().CreatedAtBoundary, Is.EqualTo(13L));
        Assert.That(profile.StructureStore.ValidateInvariants(13L).IsValid, Is.True);

        string beforeRepeat = SemanticFingerprint(profile.StructureStore.CaptureSemanticState());
        Assert.That(profile.TryCreateStructure(
            new StructureId("structure-p15a"), new LocationId("location-a"),
            out StructureStoreFailure repeated), Is.False);
        Assert.That(repeated.Code, Is.EqualTo(StructureStoreFailureCode.DuplicateStructureId));
        Assert.That(SemanticFingerprint(profile.StructureStore.CaptureSemanticState()), Is.EqualTo(beforeRepeat));
    }

    [Test]
    public void RuntimeP15ACompositionCannotCreateBeforeItsPublicationMarker()
    {
        SpatialAuthorityStore spatial = CreateSpatialAuthority();
        SimulationRuntime runtime = new SimulationRuntime(
            new SimulationTime(0L),
            cities: null,
            npcRuntimes: null,
            economyEnabled: false,
            spatialAuthorityStore: spatial,
            compositionProfile: SimulationRuntimeCompositionProfile.P15AProvingStructure,
            structureStore: new StructureStore(spatial));

        Assert.That(runtime.TryCreateP15AProvingStructure(
            new StructureId("structure-p15a"), new LocationId("location-a"),
            out StructureStoreFailure unpublished), Is.False);
        Assert.That(unpublished.Code, Is.EqualTo(StructureStoreFailureCode.InitialPublicationIncomplete));
        Assert.That(runtime.StructureStore.Count, Is.Zero);
    }

    [TestCase("missing-location", 13L, "location:structure-p15a")]
    [TestCase("location-a", 14L, "creation-boundary:structure-p15a")]
    public void RuntimeSpatialValidationIncludesStructureOwnerInvariants(
        string locationValue,
        long createdAtBoundary,
        string expectedOwnerViolation)
    {
        P15AProvingStructureComposition profile = P15AProvingStructureComposition.Compose(
            new SimulationTime(12L),
            CreateSpatialAuthority());
        profile.Runtime.AdvanceDay();
        Assert.That(profile.TryCreateStructure(
            new StructureId("structure-p15a"), new LocationId("location-a"), out _), Is.True);

        FieldInfo recordsField = typeof(StructureStore).GetField("recordsById", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(recordsField, Is.Not.Null);
        System.Collections.IDictionary records =
            (System.Collections.IDictionary)recordsField.GetValue(profile.StructureStore);
        records["structure-p15a"] = CreateRecord(
            "structure-p15a",
            locationValue,
            createdAtBoundary,
            0L);

        SimulationRuntimeSpatialInvariantReport report = profile.Runtime.ValidateSpatialInvariants();
        Assert.That(report.Violations, Does.Contain("Structures: " + expectedOwnerViolation));
    }

    private static bool TryCreateStructure(
        StructureStore store,
        StructureRecord candidate,
        long currentBoundary,
        bool published,
        out StructureStoreFailure failure)
    {
        MethodInfo method = typeof(StructureStore).GetMethod(
            "TryCreateStructure",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(method, Is.Not.Null);
        object[] arguments = { candidate, currentBoundary, published, null };
        bool created = (bool)method.Invoke(store, arguments);
        failure = (StructureStoreFailure)arguments[3];
        return created;
    }

    private static void AssertRejectedUnchanged(
        StructureStore store,
        StructureRecord candidate,
        long currentBoundary,
        bool published,
        StructureStoreFailureCode expected)
    {
        string before = SemanticFingerprint(store.CaptureSemanticState());
        Assert.That(TryCreateStructure(store, candidate, currentBoundary, published, out StructureStoreFailure failure), Is.False);
        Assert.That(failure.Code, Is.EqualTo(expected));
        Assert.That(SemanticFingerprint(store.CaptureSemanticState()), Is.EqualTo(before));
    }

    private static string SemanticFingerprint(StructureStoreSemanticState state) => string.Join("|",
        state.Revision,
        string.Join(";", state.Records.Select(record => record.Id.Value + ":" + record.Definition + ":"
            + record.LocationId.Value + ":" + record.CreatedAtBoundary + ":" + record.CreationOrder)));

    private static void AssertNoStructureStoreSurface(Type type)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public
            | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        foreach (FieldInfo field in type.GetFields(flags))
            Assert.That(ReferencesStructureStore(field.FieldType), Is.False, type.Name + " field " + field.Name);
        foreach (PropertyInfo property in type.GetProperties(flags))
            Assert.That(ReferencesStructureStore(property.PropertyType), Is.False, type.Name + " property " + property.Name);
        foreach (MethodBase method in type.GetMethods(flags).Cast<MethodBase>().Concat(type.GetConstructors(flags)))
        {
            foreach (ParameterInfo parameter in method.GetParameters())
                Assert.That(ReferencesStructureStore(parameter.ParameterType), Is.False,
                    type.Name + " method parameter " + method.Name + ":" + parameter.Name);
            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo != null)
                Assert.That(ReferencesStructureStore(methodInfo.ReturnType), Is.False, type.Name + " method return " + method.Name);
        }
    }

    private static bool ReferencesStructureStore(Type type)
    {
        if (type == typeof(StructureStore)) return true;
        if (type.IsArray) return ReferencesStructureStore(type.GetElementType());
        if (type.IsGenericType) return type.GetGenericArguments().Any(ReferencesStructureStore);
        return false;
    }

    private static StructureRecord CreateRecord(string id, string location, long boundary, long order) =>
        new StructureRecord(
            new StructureId(id),
            StructureDefinitionReference.P15AProving,
            new LocationId(location),
            boundary,
            order);

    private static SpatialAuthorityStore CreateSpatialAuthority()
    {
        SpatialAuthorityStore spatial = new SpatialAuthorityStore();
        Assert.That(spatial.TryRegisterHex(new HexRecord(new HexId("hex-a")), out _), Is.True);
        Assert.That(spatial.TryRegisterLocation(
            new LocationRecord(new LocationId("location-a"), new HexId("hex-a")), out _), Is.True);
        return spatial;
    }
}
