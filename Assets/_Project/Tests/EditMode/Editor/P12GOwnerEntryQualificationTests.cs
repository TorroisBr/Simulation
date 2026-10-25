using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed partial class P12GPoliticalBindingRestoreTests
{
    private const string QualificationSiteId = "p12g-qualification-unregistered-site";
    private const string QualificationLocationId = "p12g-qualification-unregistered-location";
    private const string QualificationLocalPlaceId = "p12g-qualification-unregistered-place";

    [Test]
    public void IntegratedMilitaryAndSiteKnowledgeOwnersPersistRestoreAndContinueWithExactParity()
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G owner qualification anchor");
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G owner qualification source",
            seedAdditionalOwners: SeedMilitaryAndSiteKnowledgeOwners);
        SyntheticSession control = CreateSyntheticSession(anchor, "P12-G owner qualification control",
            seedAdditionalOwners: SeedMilitaryAndSiteKnowledgeOwners);

        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
            Is.True, sourceAdvance.ToString());
        Assert.That(control.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure controlAdvance),
            Is.True, controlAdvance.ToString());
        DailyCaptureEligibilityToken sourceToken = GetCompletedToken(source.Session.Runtime);
        DailyCaptureEligibilityToken controlToken = GetCompletedToken(control.Session.Runtime);
        AssertMilitaryAndKnowledgeOwners(source.Session, sourceToken);
        AssertMilitaryAndKnowledgeOwners(control.Session, controlToken);
        AssertDailyV1Parity(source.Session, sourceToken, control.Session, controlToken,
            "independent integrated-source/control before durable restore");

        byte[] sourceImageBytes = EncodeCompletedImage(source.Session, anchor.NormalContext);
        P12AContinuationImage image = DecodeCompletedImage(sourceImageBytes, source.Session, anchor.NormalContext);
        AssertImageContainsIntegratedOwnerRows(image, source.Session.Runtime.NpcRuntimes.First().RuntimeId);

        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                image,
                image.Header.Compatibility,
                anchor.Configuration,
                anchor.Runtime.Configuration,
                anchor.Composition.Calendar,
                anchor.Composition.SelectedDefinitions,
                anchor.NormalContext,
                out SimulationActiveSession restored,
                out P12GDailyV1RestoreFailure restoreFailure,
                out string restoreDiagnostic),
            Is.True, restoreFailure + ": " + restoreDiagnostic);
        Assert.That(restored, Is.Not.Null);
        Assert.That(restored.Composition.SelectedDefinitions,
            Is.SameAs(anchor.Composition.SelectedDefinitions),
            "Detached restore must consume the canonical typed graph selected by the admitted profile.");
        DailyCaptureEligibilityToken restoredToken = GetCompletedToken(restored.Runtime);
        Assert.That(restoredToken.BoundaryProvenance,
            Is.EqualTo(DailyCaptureBoundaryProvenance.RestoredContinuation));
        AssertMilitaryAndKnowledgeOwners(restored, restoredToken);
        AssertDailyV1Parity(source.Session, sourceToken, restored, restoredToken,
            "source/image-restored at the captured boundary");
        AssertDailyV1Parity(restored, restoredToken, control.Session, controlToken,
            "image-restored/control at the captured boundary");
        Assert.That(EncodeCompletedImage(restored, anchor.NormalContext), Is.EqualTo(sourceImageBytes),
            "Every persisted owner payload, alias group, root and revision must be reproduced exactly after image restore.");

        for (int boundary = 1; boundary <= 2; boundary++)
        {
            Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextSource),
                Is.True, nextSource.ToString());
            Assert.That(restored.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextRestored),
                Is.True, nextRestored.ToString());
            Assert.That(control.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure nextControl),
                Is.True, nextControl.ToString());
            DailyCaptureEligibilityToken nextSourceToken = GetCompletedToken(source.Session.Runtime);
            DailyCaptureEligibilityToken nextRestoredToken = GetCompletedToken(restored.Runtime);
            DailyCaptureEligibilityToken nextControlToken = GetCompletedToken(control.Session.Runtime);
            AssertMilitaryAndKnowledgeOwners(source.Session, nextSourceToken);
            AssertMilitaryAndKnowledgeOwners(restored, nextRestoredToken);
            AssertDailyV1Parity(source.Session, nextSourceToken, restored, nextRestoredToken,
                "exact military/site-knowledge continuation T+" + boundary);
            AssertDailyV1Parity(source.Session, nextSourceToken, control.Session, nextControlToken,
                "independent deterministic control T+" + boundary);
            Assert.That(EncodeCompletedImage(source.Session, anchor.NormalContext),
                Is.EqualTo(EncodeCompletedImage(restored, anchor.NormalContext)),
                "Complete Daily-v1 serialized images must remain byte-identical at T+" + boundary + ".");
            Assert.That(EncodeCompletedImage(source.Session, anchor.NormalContext),
                Is.EqualTo(EncodeCompletedImage(control.Session, anchor.NormalContext)),
                "Independent same-input executions must remain byte-identical at T+" + boundary + ".");
        }
    }

    [TestCase(QualificationImageCorruption.MissingForceReference)]
    [TestCase(QualificationImageCorruption.SplitForceOwnerAlias)]
    public void IntegratedOwnerImageRejectionIsAtomicAndValidImageCanBeRetried(
        QualificationImageCorruption corruption)
    {
        AnchorFixture anchor = CreateAnchorFixture("P12-G owner rejection anchor " + corruption);
        SyntheticSession source = CreateSyntheticSession(anchor, "P12-G owner rejection source " + corruption,
            seedAdditionalOwners: SeedMilitaryAndSiteKnowledgeOwners);
        Assert.That(source.Session.Runtime.TryAdvanceDay(out SimulationRuntimeAdvanceFailure sourceAdvance),
            Is.True, sourceAdvance.ToString());
        DailyCaptureEligibilityToken sourceToken = GetCompletedToken(source.Session.Runtime);
        byte[] validBytes = EncodeCompletedImage(source.Session, anchor.NormalContext);
        P12AContinuationImage validImage = DecodeCompletedImage(validBytes, source.Session, anchor.NormalContext);
        string sourceSignature = CaptureTypedParitySignature(source.Session, sourceToken);

        P12AContinuationImage rejectedImage = corruption == QualificationImageCorruption.MissingForceReference
            ? ReplaceConflictBindingWithMissingForce(validImage)
            : ReplaceForceAlias(validImage);
        if (corruption == QualificationImageCorruption.SplitForceOwnerAlias)
        {
            Assert.That(P12ADailyV1Census.TryCreateForImage(rejectedImage,
                    out _, out P12AResult aliasResult), Is.False,
                "A changed shared ArmedForce alias must fail the authoritative image census before encoding.");
            Assert.That(aliasResult?.Code, Is.EqualTo(P12AResultCode.OwnerGraphRejected));
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                    rejectedImage,
                    rejectedImage.Header.Compatibility,
                    anchor.Configuration,
                    anchor.Runtime.Configuration,
                    anchor.Composition.Calendar,
                    anchor.Composition.SelectedDefinitions,
                    anchor.NormalContext,
                    out SimulationActiveSession aliasRejected,
                    out P12GDailyV1RestoreFailure aliasFailure,
                    out string aliasDiagnostic),
                Is.False, "The integrated G restore boundary must reject a split shared ArmedForce alias.");
            Assert.That(aliasRejected, Is.Null);
            Assert.That(aliasFailure, Is.EqualTo(P12GDailyV1RestoreFailure.SourceCaptureFailed));
            Assert.That(aliasDiagnostic, Does.Contain("owner manifest"));
            Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True);
            Assert.That(CaptureTypedParitySignature(source.Session, sourceToken), Is.EqualTo(sourceSignature));
            Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                    validImage,
                    validImage.Header.Compatibility,
                    anchor.Configuration,
                    anchor.Runtime.Configuration,
                    anchor.Composition.Calendar,
                    anchor.Composition.SelectedDefinitions,
                    anchor.NormalContext,
                    out SimulationActiveSession aliasRetry,
                    out P12GDailyV1RestoreFailure aliasRetryFailure,
                    out string aliasRetryDiagnostic),
                Is.True, aliasRetryFailure + ": " + aliasRetryDiagnostic);
            Assert.That(EncodeCompletedImage(aliasRetry, anchor.NormalContext), Is.EqualTo(validBytes));
            return;
        }
        P12AContinuationImage roundTripRejectedImage = EncodeThenDecodeImage(rejectedImage);
        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                roundTripRejectedImage,
                roundTripRejectedImage.Header.Compatibility,
                anchor.Configuration,
                anchor.Runtime.Configuration,
                anchor.Composition.Calendar,
                anchor.Composition.SelectedDefinitions,
                anchor.NormalContext,
                out SimulationActiveSession rejected,
                out P12GDailyV1RestoreFailure rejectionFailure,
                out string rejectionDiagnostic),
            Is.False, corruption + " must reject during detached graph validation.");
        Assert.That(rejected, Is.Null);
        Assert.That(rejectionFailure, Is.Not.EqualTo(P12GDailyV1RestoreFailure.None));
        Assert.That(rejectionDiagnostic, Is.Not.Empty);
        Assert.That(source.Session.Runtime.TryValidateCompletedDailyCaptureToken(sourceToken, out _), Is.True,
            "A rejected detached graph must not invalidate or mutate the source completed boundary.");
        Assert.That(CaptureTypedParitySignature(source.Session, sourceToken), Is.EqualTo(sourceSignature),
            "A rejected detached graph must preserve all source-boundary values.");

        Assert.That(P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionFromImage(
                validImage,
                validImage.Header.Compatibility,
                anchor.Configuration,
                anchor.Runtime.Configuration,
                anchor.Composition.Calendar,
                anchor.Composition.SelectedDefinitions,
                anchor.NormalContext,
                out SimulationActiveSession retried,
                out P12GDailyV1RestoreFailure retryFailure,
                out string retryDiagnostic),
            Is.True, retryFailure + ": " + retryDiagnostic);
        Assert.That(retried, Is.Not.Null);
        DailyCaptureEligibilityToken retriedToken = GetCompletedToken(retried.Runtime);
        AssertMilitaryAndKnowledgeOwners(retried, retriedToken);
        Assert.That(EncodeCompletedImage(retried, anchor.NormalContext), Is.EqualTo(validBytes),
            "A valid retry after rejection must reproduce the exact previously valid owner image.");
    }

    [Test]
    public void GenericP9SiteProfileValidatesButDailyV1AdmissionDoesNotPublishIt()
    {
        SimulationConfigData authored = AssetDatabase.LoadAssetAtPath<SimulationConfigData>(DailyConfigPath);
        Assert.That(authored, Is.Not.Null);
        SimulationConfigData candidate = UnityEngine.Object.Instantiate(authored);
        ExplorableSiteData genericSite = ScriptableObject.CreateInstance<ExplorableSiteData>();
        genericSite.id = "p12g-qualification-generic-p9-site";
        genericSite.siteName = "Qualification Generic Site";
        genericSite.kind = ExplorableSiteKind.Generic;
        candidate.explorableSites.Add(new ExplorableSiteConfig
        {
            site = genericSite,
            anchorCity = candidate.Cities[0],
            travelDaysFromAnchor = 1
        });
        try
        {
            Assert.DoesNotThrow(() => SimulationGenesisPipeline.ValidateProfile(candidate),
                "A generic authored P9 site remains valid under the general profile contract.");

            GameObject bootstrapObject = new GameObject("P12-G generic P9 site Daily rejection");
            bootstrapObjects.Add(bootstrapObject);
            TesteSimulacao bootstrap = bootstrapObject.AddComponent<TesteSimulacao>();
            SerializedObject serializedBootstrap = new SerializedObject(bootstrap);
            serializedBootstrap.FindProperty("simulationConfig").objectReferenceValue = candidate;
            serializedBootstrap.FindProperty("runtimeAdmissionProfile").enumValueIndex =
                (int)SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1;
            serializedBootstrap.ApplyModifiedPropertiesWithoutUndo();
            InvalidOperationException admissionFailure = Assert.Throws<InvalidOperationException>(() => bootstrap.Start());
            Assert.That(admissionFailure.Message,
                Does.Contain("requires the admitted exact-empty ExplorableSite/P10 topology profile"),
                "The Core genesis composer must reject the excluded site root before publishing the selected Daily-v1 owner graph.");

            Assert.That(bootstrap.Bootstrap, Is.Null,
                "A non-empty generic site profile cannot publish through the selected Daily-v1 admission.");
            Assert.That(bootstrap.Runtime, Is.Null);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(candidate);
            UnityEngine.Object.DestroyImmediate(genericSite);
        }
    }

    private static void SeedMilitaryAndSiteKnowledgeOwners(
        P12DDailyV1OwnerPackage stagedD,
        P12EDailyV1OwnerPackage stagedE,
        P12FDailyV1OwnerPackage stagedF)
    {
        PersonRuntime commander = stagedD.Persons.Persons.First(person => person?.PersonId != null);
        PersonRuntime officer = stagedD.Persons.Persons.First(person => person?.PersonId != null
            && person.PersonId != commander.PersonId);
        ArmedForceId rootId = new ArmedForceId("p12g-qualification-force-root");
        ArmedForceId childId = new ArmedForceId("p12g-qualification-force-child");
        Assert.That(stagedE.ArmedForces.TryRegister(new ArmedForceRecord(
            rootId, "Qualification Root", 0L, commanderPersonId: commander.PersonId), out _), Is.True);
        Assert.That(stagedE.ArmedForces.TryRegister(new ArmedForceRecord(childId,
            "Qualification Child", 0L, rootId), out _), Is.True);
        ContingentId contingentId = new ContingentId("p12g-qualification-contingent");
        Assert.That(stagedE.ContingentManpower.TryRegisterContingent(new ContingentRecord(
            contingentId, childId, 0L,
            new ContingentOriginReference("qualification", "source"),
            "standing-service",
            new[] { new ArmedForceCharacteristic("branch", "field") }),
            out ContingentManpowerFailure contingentFailure), Is.True, contingentFailure.ToString());
        Assert.That(stagedE.ArmedForces.TryAddRelevantPerson(rootId, officer.PersonId,
            "officer", out _, out _), Is.True);
        LocationId selectedLocation = new LocationId(
            stagedE.ArmedForcePositions.SpatialAuthorityStore.Locations.Single().Id.Value);
        Assert.That(stagedE.ArmedForcePositions.TrySetPosition(childId,
            SpatialReference.ForLocation(selectedLocation), out ArmedForceSpatialFailure positionFailure),
            Is.True, positionFailure.ToString());

        ConflictId conflictId = new ConflictId("p12g-qualification-conflict");
        ConflictSideId conflictA = new ConflictSideId("qualification-conflict-a");
        ConflictSideId conflictB = new ConflictSideId("qualification-conflict-b");
        Assert.That(stagedE.Conflicts.TryRegister(new PersistentConflictRecord(
            conflictId, 0L,
            sides: new[] { new ConflictStateSide(conflictId, conflictA, "A"),
                new ConflictStateSide(conflictId, conflictB, "B") },
            participantBindings: new[]
            {
                new ConflictParticipantBinding(new ConflictParticipantBindingId("qualification-conflict-root"),
                    conflictId, conflictA, rootId),
                new ConflictParticipantBinding(new ConflictParticipantBindingId("qualification-conflict-child"),
                    conflictId, conflictB, childId)
            }), out PersistentStateFailure conflictFailure), Is.True, conflictFailure.ToString());

        WarId warId = new WarId("p12g-qualification-war");
        WarSideId warA = new WarSideId("qualification-war-a");
        WarSideId warB = new WarSideId("qualification-war-b");
        Assert.That(stagedE.Wars.TryRegister(new PersistentWarRecord(
            warId, 0L, conflictId,
            sides: new[] { new WarStateSide(warId, warA, "A"), new WarStateSide(warId, warB, "B") },
            participantBindings: new[]
            {
                new WarParticipantBinding(new WarParticipantBindingId("qualification-war-root"), warId, warA, rootId),
                new WarParticipantBinding(new WarParticipantBindingId("qualification-war-child"), warId, warB, childId)
            }), out PersistentStateFailure warFailure), Is.True, warFailure.ToString());

        BattleId battleId = new BattleId("p12g-qualification-battle");
        BattleSideId battleA = new BattleSideId("qualification-battle-a");
        BattleSideId battleB = new BattleSideId("qualification-battle-b");
        Assert.That(stagedE.Battles.TryRegister(new PersistentBattleRecord(
            battleId, 0L, conflictId, warId,
            sides: new[] { new BattleStateSide(battleId, battleA, "A"),
                new BattleStateSide(battleId, battleB, "B") },
            participantBindings: new[]
            {
                new BattleParticipantBinding(new BattleParticipantBindingId("qualification-battle-root"), battleId, battleA, rootId),
                new BattleParticipantBinding(new BattleParticipantBindingId("qualification-battle-child"), battleId, battleB, childId)
            },
            locationReference: SpatialReference.ForLocation(selectedLocation)),
            out PersistentStateFailure battleFailure), Is.True, battleFailure.ToString());

        NpcRuntime npc = stagedD.Npcs.First(value => value != null);
        Assert.That(npc.ExistingExplorableSiteKnowledge.RecordObservation(new ExplorableSiteKnowledgeObservation(
            QualificationSiteId, QualificationLocationId, 0L, 1L, ExplorableSiteKnowledgeSource.DirectObservation)), Is.True);
        Assert.That(npc.ExistingAdventureSiteIntelKnowledge.RecordObservation(new AdventureOppositionObservation(
            QualificationSiteId, QualificationLocalPlaceId, "unregistered-opposition",
            AdventureOppositionObservedState.Active, 0L, 1L, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(npc.ExistingAdventureSiteIntelKnowledge.RecordObservation(new AdventureNotableItemObservation(
            QualificationSiteId, QualificationLocalPlaceId, "unregistered-notable",
            "unregistered-item-definition", 0L, 1L, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(npc.ExistingAdventureSiteIntelKnowledge.RecordObservation(new AdventureCommonResourceObservation(
            QualificationSiteId, QualificationLocalPlaceId, "unregistered-resource-item", 13, "herb",
            0L, 1L, AdventureIntelSource.DirectObservation)), Is.True);
        Assert.That(npc.ExistingAdventureSiteIntelKnowledge.RecordObservation(new AdventureAccessObservation(
            QualificationSiteId, QualificationLocalPlaceId, PlaceAccessState.Accessible,
            0L, 1L, AdventureIntelSource.DirectObservation)), Is.True);

        Assert.That(stagedD.RuntimeIdentities.TryGetExplorableSite(QualificationSiteId, out _), Is.False,
            "Factual belief rows deliberately do not install a site identity root.");
        Assert.That(stagedD.RuntimeIdentities.TryGetLocalPlace(QualificationLocalPlaceId, out _), Is.False,
            "Factual belief rows deliberately do not install a local-place identity root.");
    }

    private static void AssertMilitaryAndKnowledgeOwners(
        SimulationActiveSession session,
        DailyCaptureEligibilityToken token)
    {
        SimulationRuntime runtime = session.Runtime;
        OwnerSectionCensusSnapshot[] rows = token.OwnerSections.ToArray();
        AssertSection(rows, ArmedForceStoreCensusProvider.ForcesSectionId, runtime.ArmedForceStore, 2, 4L);
        AssertSection(rows, ArmedForceStoreCensusProvider.ContingentsSectionId, runtime.ArmedForceStore, 1, 4L);
        AssertSection(rows, ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId, runtime.ArmedForceStore, 1, 4L);
        AssertSection(rows, ContingentManpowerCensusProvider.SectionId, runtime.ContingentManpowerStateStore, 1, 1L);
        AssertSection(rows, ArmedForceSpatialCensusProvider.SectionId, runtime.ArmedForceSpatialStateStore, 1, 1L);
        AssertSection(rows, PersistentConflictCensusProvider.SectionId, runtime.ConflictStore, 1, 1L);
        AssertSection(rows, PersistentWarCensusProvider.SectionId, runtime.WarStore, 1, 1L);
        AssertSection(rows, PersistentBattleCensusProvider.SectionId, runtime.BattleStore, 1, 1L);
        Assert.That(runtime.ConflictStore.ArmedForceStore, Is.SameAs(runtime.ArmedForceStore));
        Assert.That(runtime.WarStore.ArmedForceStore, Is.SameAs(runtime.ArmedForceStore));
        Assert.That(runtime.WarStore.ConflictStore, Is.SameAs(runtime.ConflictStore));
        Assert.That(runtime.BattleStore.ArmedForceStore, Is.SameAs(runtime.ArmedForceStore));
        Assert.That(runtime.BattleStore.ConflictStore, Is.SameAs(runtime.ConflictStore));
        Assert.That(runtime.BattleStore.WarStore, Is.SameAs(runtime.WarStore));

        NpcRuntime npc = runtime.NpcRuntimes.First(value => value != null);
        string site = NpcKnowledgeCensusProvider.SectionIdFor(0, npc.RuntimeId);
        string places = NpcKnowledgeCensusProvider.SectionIdFor(1, npc.RuntimeId);
        string connections = NpcKnowledgeCensusProvider.SectionIdFor(2, npc.RuntimeId);
        string opposition = NpcKnowledgeCensusProvider.SectionIdFor(3, npc.RuntimeId);
        string notable = NpcKnowledgeCensusProvider.SectionIdFor(4, npc.RuntimeId);
        string resources = NpcKnowledgeCensusProvider.SectionIdFor(5, npc.RuntimeId);
        string access = NpcKnowledgeCensusProvider.SectionIdFor(6, npc.RuntimeId);
        DailyV1NpcRuntimeOwners selectedOwners = npc.SelectedRuntimeOwners;
        Assert.That(selectedOwners, Is.Not.Null,
            "The selected Daily-v1 restore fixture must publish its exact Core NPC owner graph.");
        Assert.That(npc.ExistingExplorableSiteKnowledge.SelectedOwner,
            Is.SameAs(selectedOwners.ExplorableSiteKnowledge));
        Assert.That(npc.ExistingAdventureSiteIntelKnowledge.SelectedOwner,
            Is.SameAs(selectedOwners.AdventureSiteIntelKnowledge));
        AssertSection(rows, site, selectedOwners.ExplorableSiteKnowledge, 1, 1L);
        AssertSection(rows, places, npc.ExistingLocalTopologyKnowledge, 0, 0L);
        AssertSection(rows, connections, npc.ExistingLocalTopologyKnowledge, 0, 0L);
        AssertSection(rows, opposition, selectedOwners.AdventureSiteIntelKnowledge, 1, 4L);
        AssertSection(rows, notable, selectedOwners.AdventureSiteIntelKnowledge, 1, 4L);
        AssertSection(rows, resources, selectedOwners.AdventureSiteIntelKnowledge, 1, 4L);
        AssertSection(rows, access, selectedOwners.AdventureSiteIntelKnowledge, 1, 4L);
        Assert.That(rows.Single(row => row.SectionId == places).OwnerInstanceIdentity,
            Is.SameAs(rows.Single(row => row.SectionId == connections).OwnerInstanceIdentity));
        Assert.That(rows.Single(row => row.SectionId == places).Role, Is.EqualTo(OwnerSectionRole.Required));
        Assert.That(rows.Single(row => row.SectionId == connections).Role, Is.EqualTo(OwnerSectionRole.Required));
    }

    private static void AssertSection(
        IReadOnlyList<OwnerSectionCensusSnapshot> rows,
        string sectionId,
        object expectedOwner,
        int cardinality,
        long revision)
    {
        OwnerSectionCensusSnapshot row = rows.Single(value => value.SectionId == sectionId);
        Assert.That(row.Role, Is.EqualTo(OwnerSectionRole.Required), sectionId);
        Assert.That(row.OwnerInstanceIdentity, Is.SameAs(expectedOwner), sectionId + " exact owner instance");
        Assert.That(row.Cardinality, Is.EqualTo(cardinality), sectionId + " cardinality");
        Assert.That(row.Revision, Is.EqualTo(revision), sectionId + " revision");
    }

    private static void AssertImageContainsIntegratedOwnerRows(P12AContinuationImage image, string npcRuntimeId)
    {
        string[] ids =
        {
            ArmedForceStoreCensusProvider.ForcesSectionId,
            ArmedForceStoreCensusProvider.ContingentsSectionId,
            ArmedForceStoreCensusProvider.RelevantPersonReferencesSectionId,
            ContingentManpowerCensusProvider.SectionId,
            ArmedForceSpatialCensusProvider.SectionId,
            PersistentConflictCensusProvider.SectionId,
            PersistentWarCensusProvider.SectionId,
            PersistentBattleCensusProvider.SectionId
        };
        foreach (string id in ids)
            Assert.That(image.Sections.Single(section => section.Id == id).Rows, Is.Not.Empty, id);
        Assert.That(image.Sections.Single(section => section.Id == PersistentConflictCensusProvider.SectionId)
            .Rows.Single().PayloadLength, Is.GreaterThan(0));
        Assert.That(image.Sections.Single(section => section.Id == PersistentWarCensusProvider.SectionId)
            .Rows.Single().PayloadLength, Is.GreaterThan(0));
        Assert.That(image.Sections.Single(section => section.Id == PersistentBattleCensusProvider.SectionId)
            .Rows.Single().PayloadLength, Is.GreaterThan(0));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(0, npcRuntimeId))
            .Rows, Has.Count.EqualTo(1));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(3, npcRuntimeId))
            .Rows, Has.Count.EqualTo(1));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(4, npcRuntimeId))
            .Rows, Has.Count.EqualTo(1));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(5, npcRuntimeId))
            .Rows, Has.Count.EqualTo(1));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(6, npcRuntimeId))
            .Rows, Has.Count.EqualTo(1));
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(1, npcRuntimeId))
            .Rows, Is.Empty);
        Assert.That(image.Sections.Single(section => section.Id == NpcKnowledgeCensusProvider.SectionIdFor(2, npcRuntimeId))
            .Rows, Is.Empty);
    }

    private static byte[] EncodeCompletedImage(
        SimulationActiveSession session,
        SimulationRuntimeAdmissionContext normalContext)
    {
        DailyV1ExecutionBoundaryState executionState = session.Runtime.ExecutionBoundaryState;
        Assert.That(executionState.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        bool captured;
        P12AContinuationImage image;
        P12GDailyV1RestoreFailure failure;
        string diagnostic;
        try
        {
            captured = P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                session, normalContext,
                out image,
                out _,
                out failure,
                out diagnostic);
        }
        finally
        {
            Assert.That(executionState.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        }
        Assert.That(captured, Is.True, failure + ": " + diagnostic);
        Assert.That(P12ADailyV1Census.TryCreateForImage(image,
            out P12ADailyV1Census census, out P12AResult censusResult),
            Is.True, censusResult?.Code.ToString());
        Assert.That(P12AContinuationCodec.TryEncode(image, census.Manifest,
            out byte[] bytes, out P12AResult encodeResult), Is.True, encodeResult?.Code.ToString());
        return bytes;
    }

    private static P12AContinuationImage DecodeCompletedImage(
        byte[] bytes,
        SimulationActiveSession session,
        SimulationRuntimeAdmissionContext normalContext)
    {
        DailyV1ExecutionBoundaryState executionState = session.Runtime.ExecutionBoundaryState;
        Assert.That(executionState.TryBeginOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        bool captured;
        P12AContinuationImage sourceImage;
        P12GDailyV1RestoreFailure sourceFailure;
        string sourceDiagnostic;
        try
        {
            captured = P12GDailyV1RestoreCoordinator.TryCreateRestoredSessionAndCaptureImage(
                session, normalContext, out sourceImage, out _,
                out sourceFailure, out sourceDiagnostic);
        }
        finally
        {
            Assert.That(executionState.TryEndOperation(DailyV1OperationScopeKind.ContinuationSave), Is.True);
        }
        Assert.That(captured, Is.True, sourceFailure + ": " + sourceDiagnostic);
        Assert.That(P12ADailyV1Census.TryCreateForImage(sourceImage,
            out P12ADailyV1Census census, out P12AResult censusResult), Is.True, censusResult?.Code.ToString());
        Assert.That(P12AContinuationCodec.TryDecode(bytes, census.Manifest,
            sourceImage.Header.Compatibility, out P12AContinuationImage decoded,
            out P12AResult decodeResult), Is.True, decodeResult?.Code.ToString());
        return decoded;
    }

    private static P12AContinuationImage EncodeThenDecodeImage(P12AContinuationImage image)
    {
        Assert.That(P12ADailyV1Census.TryCreateForImage(image,
            out P12ADailyV1Census census, out P12AResult censusResult), Is.True, censusResult?.Code.ToString());
        Assert.That(P12AContinuationCodec.TryEncode(image, census.Manifest,
            out byte[] bytes, out P12AResult encodeResult), Is.True, encodeResult?.Code.ToString());
        Assert.That(P12AContinuationCodec.TryDecode(bytes, census.Manifest,
            image.Header.Compatibility, out P12AContinuationImage decoded,
            out P12AResult decodeResult), Is.True, decodeResult?.Code.ToString());
        return decoded;
    }

    private static P12AContinuationImage ReplaceConflictBindingWithMissingForce(P12AContinuationImage image)
    {
        P12EPersistentConflictWarBattleOwnerCodec.TryDecode(
            image.Sections.Where(section => section.Id == PersistentConflictCensusProvider.SectionId
                || section.Id == PersistentWarCensusProvider.SectionId
                || section.Id == PersistentBattleCensusProvider.SectionId).ToArray(),
            out PersistentConflictOwnerSnapshot conflicts,
            out PersistentWarOwnerSnapshot wars,
            out PersistentBattleOwnerSnapshot battles,
            out P12AResult decodeResult);
        Assert.That(decodeResult, Is.Null);
        PersistentConflictOwnerSnapshotRecord original = conflicts.Records.Single();
        PersistentConflictOwnerBindingSnapshot originalBinding = original.ParticipantBindings.First();
        PersistentConflictOwnerBindingSnapshot missingBinding = new PersistentConflictOwnerBindingSnapshot(
            originalBinding.BindingIdValue, originalBinding.ConflictIdValue,
            originalBinding.SideIdValue, "p12g-qualification-missing-force");
        PersistentConflictOwnerSnapshotRecord replacement = new PersistentConflictOwnerSnapshotRecord(
            original.ConflictIdValue, original.CreatedAbsoluteDay, original.LifecycleState,
            original.EndedAbsoluteDay, original.Sides,
            new[] { missingBinding }.Concat(original.ParticipantBindings.Skip(1)));
        PersistentConflictOwnerSnapshot changed = new PersistentConflictOwnerSnapshot(
            conflicts.SchemaVersion, conflicts.RecordCount, conflicts.Revision, new[] { replacement });
        Assert.That(P12EPersistentConflictWarBattleOwnerCodec.TryEncode(changed, wars, battles,
            out IReadOnlyList<P12AOwnerSection> encoded, out P12AResult encodeResult),
            Is.True, encodeResult?.Code.ToString());
        return ReplaceSections(image, encoded);
    }

    private static P12AContinuationImage ReplaceForceAlias(P12AContinuationImage image)
    {
        P12AOwnerSection section = image.Sections.Single(value =>
            value.Id == ArmedForceStoreCensusProvider.ContingentsSectionId);
        P12AOwnerSection replacement = new P12AOwnerSection(section.Id, section.Major, section.Minor,
            section.Role, "qualification-split-force-alias", section.Revision, section.Rows);
        return ReplaceSections(image, new[] { replacement });
    }

    private static P12AContinuationImage ReplaceSections(
        P12AContinuationImage image,
        IEnumerable<P12AOwnerSection> replacements)
    {
        Dictionary<string, P12AOwnerSection> byId = replacements.ToDictionary(value => value.Id, StringComparer.Ordinal);
        P12AOwnerSection[] sections = image.Sections.Select(section =>
            byId.TryGetValue(section.Id, out P12AOwnerSection replacement) ? replacement : section).ToArray();
        return new P12AContinuationImage(image.Header, image.CausalRoots, sections);
    }

    public enum QualificationImageCorruption
    {
        MissingForceReference,
        SplitForceOwnerAlias
    }
}
