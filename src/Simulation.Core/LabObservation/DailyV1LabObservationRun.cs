using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

public enum DailyV1LabObservationStartFailure : byte
{
    None = 0,
    InvalidFreshRoot = 1,
    InvalidHostIdentity = 2,
    StartupRejected = 3,
    NoSaveNotPublished = 4,
    PublishedSessionInvalid = 5
}

public enum DailyV1LabObservationDisposition : byte
{
    RejectedBeforeAdvance = 0,
    AdvanceUnsuccessful = 1,
    AdvancedObservationUnavailable = 2,
    Captured = 3
}

public enum DailyV1LabObservationDiagnosticCode : byte
{
    None = 0,
    RunStopped = 1,
    Disposed = 2,
    WrongThread = 3,
    SessionChanged = 4,
    RuntimeAdvanceRejected = 5,
    RuntimeAdvanceFailed = 6,
    CompletedBoundaryUnavailable = 7,
    SourceEvidenceUnavailable = 8,
    InvalidSourceProjection = 9
}

public sealed class DailyV1LabObservationAdvanceResult
{
    public DailyV1LabObservationDisposition Disposition { get; }
    public DailyV1LabObservationDiagnosticCode DiagnosticCode { get; }
    public SimulationRuntimeAdvanceFailure? AdvanceFailure { get; }
    public DailyV1AtlasObservation Observation { get; }

    internal DailyV1LabObservationAdvanceResult(
        DailyV1LabObservationDisposition disposition,
        DailyV1LabObservationDiagnosticCode diagnosticCode,
        SimulationRuntimeAdvanceFailure? advanceFailure,
        DailyV1AtlasObservation observation)
    {
        Disposition = disposition;
        DiagnosticCode = diagnosticCode;
        AdvanceFailure = advanceFailure;
        Observation = observation;
    }
}

public sealed class DailyV1AtlasHexObservation
{
    public string HexId { get; }
    public int? Q { get; }
    public int? R { get; }
    public string TerrainId { get; }
    public string TerrainRevision { get; }

    internal DailyV1AtlasHexObservation(string hexId, int? q, int? r,
        string terrainId, string terrainRevision)
    {
        HexId = hexId;
        Q = q;
        R = r;
        TerrainId = terrainId;
        TerrainRevision = terrainRevision;
    }
}

public sealed class DailyV1AtlasLocationObservation
{
    public string LocationId { get; }
    public string AnchorHexId { get; }

    internal DailyV1AtlasLocationObservation(string locationId, string anchorHexId)
    {
        LocationId = locationId;
        AnchorHexId = anchorHexId;
    }
}

public sealed class DailyV1AtlasCityObservation
{
    public string CityId { get; }
    public string Label { get; }
    public string P8LocationId { get; }

    internal DailyV1AtlasCityObservation(string cityId, string label, string p8LocationId)
    {
        CityId = cityId;
        Label = label;
        P8LocationId = p8LocationId;
    }
}

public sealed class DailyV1AtlasMarketStockObservation
{
    public string CityId { get; }
    public string ItemDefinitionId { get; }
    public int Quantity { get; }

    internal DailyV1AtlasMarketStockObservation(
        string cityId, string itemDefinitionId, int quantity)
    {
        CityId = cityId;
        ItemDefinitionId = itemDefinitionId;
        Quantity = quantity;
    }
}

/// <summary>
/// Detached immutable factual values copied from one admitted completed
/// Daily-v1 boundary. It contains no owner, store, session, P12 vector, token,
/// mutation epoch, callback, or persistence control.
/// </summary>
public sealed class DailyV1AtlasObservation
{
    private readonly ReadOnlyCollection<DailyV1AtlasHexObservation> hexes;
    private readonly ReadOnlyCollection<DailyV1AtlasLocationObservation> locations;
    private readonly ReadOnlyCollection<DailyV1AtlasCityObservation> cities;
    private readonly ReadOnlyCollection<DailyV1AtlasMarketStockObservation> marketStockRows;

    public string WorldId { get; }
    public long AbsoluteDay { get; }
    public long CompletedSequence { get; }
    public string EffectiveInputFingerprint { get; }
    public long SpatialRevision { get; }
    public IReadOnlyList<DailyV1AtlasHexObservation> Hexes => hexes;
    public IReadOnlyList<DailyV1AtlasLocationObservation> Locations => locations;
    public long IdentityIndexRevision { get; }
    public long AnchorRevision { get; }
    public IReadOnlyList<DailyV1AtlasCityObservation> Cities => cities;
    public string ScopedCityId { get; }
    public string ScopedItemDefinitionId { get; }
    public long MarketRevision { get; }
    public IReadOnlyList<DailyV1AtlasMarketStockObservation> MarketStockRows => marketStockRows;

    internal DailyV1AtlasObservation(
        string worldId,
        long absoluteDay,
        long completedSequence,
        string effectiveInputFingerprint,
        long spatialRevision,
        IList<DailyV1AtlasHexObservation> hexes,
        IList<DailyV1AtlasLocationObservation> locations,
        long identityIndexRevision,
        long anchorRevision,
        IList<DailyV1AtlasCityObservation> cities,
        string scopedCityId,
        string scopedItemDefinitionId,
        long marketRevision,
        IList<DailyV1AtlasMarketStockObservation> marketStockRows)
    {
        WorldId = worldId;
        AbsoluteDay = absoluteDay;
        CompletedSequence = completedSequence;
        EffectiveInputFingerprint = effectiveInputFingerprint;
        SpatialRevision = spatialRevision;
        this.hexes = new ReadOnlyCollection<DailyV1AtlasHexObservation>(
            new List<DailyV1AtlasHexObservation>(hexes));
        this.locations = new ReadOnlyCollection<DailyV1AtlasLocationObservation>(
            new List<DailyV1AtlasLocationObservation>(locations));
        IdentityIndexRevision = identityIndexRevision;
        AnchorRevision = anchorRevision;
        this.cities = new ReadOnlyCollection<DailyV1AtlasCityObservation>(
            new List<DailyV1AtlasCityObservation>(cities));
        ScopedCityId = scopedCityId;
        ScopedItemDefinitionId = scopedItemDefinitionId;
        MarketRevision = marketRevision;
        this.marketStockRows = new ReadOnlyCollection<DailyV1AtlasMarketStockObservation>(
            new List<DailyV1AtlasMarketStockObservation>(marketStockRows));
    }
}

/// <summary>
/// Fixed Core-owned Daily-v1 observation run. Startup uses only the promoted
/// production input provider and fixed .NET application ports. Each call
/// advances the retained exact session once and returns a detached read cut.
/// </summary>
public sealed class DailyV1LabObservationRun : IDisposable
{
    private readonly Thread ownerThread;
    private readonly int ownerThreadId;
    private readonly DailyV1EffectiveDefinitions definitions;
    private DailyV1Application application;
    private DailyV1ActiveRuntimeSession session;
    private readonly WorldId worldId;
    private long lastAbsoluteDay;
    private long lastCompletedSequence;
    private int operationActive;
    private int stopped;
    private int disposed;

    private DailyV1LabObservationRun(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session,
        DailyV1EffectiveDefinitions definitions)
    {
        this.application = application;
        this.session = session;
        this.definitions = definitions;
        worldId = session.WorldId;
        lastAbsoluteDay = session.SimulationTime.AbsoluteDay;
        lastCompletedSequence = session.ExecutionBoundaryState.CompletedCoreSequence;
        ownerThread = Thread.CurrentThread;
        ownerThreadId = ownerThread.ManagedThreadId;
    }

    /// <summary>
    /// Starts the one fixed selected Daily-v1 profile on an empty private
    /// continuation root. The identity must be captured by the approved
    /// DailyV1DotNetHostEnvironmentIdentityFactory using the real host entry
    /// point. The facade supplies no caller callbacks or host ports.
    /// </summary>
    public static bool TryStart(
        string trustedFreshNoSaveRoot,
        DailyV1HostEnvironmentIdentity anchoredDotNetIdentity,
        out DailyV1LabObservationRun run,
        out DailyV1LabObservationStartFailure failure)
    {
        run = null;
        failure = DailyV1LabObservationStartFailure.InvalidFreshRoot;
        if (!TryValidateFreshRoot(trustedFreshNoSaveRoot, out string fullRoot))
            return false;

        failure = DailyV1LabObservationStartFailure.InvalidHostIdentity;
        if (anchoredDotNetIdentity == null)
            return false;

        DailyV1EffectiveDefinitions definitions;
        try
        {
            definitions = DailyV1LabSelectedReferenceInputs.CreateDefinitions();
            if (!DailyV1CompatibilityDescriptorFactory.TryCreateDotNetV2(
                    definitions, anchoredDotNetIdentity,
                    out _, out _))
                return false;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException)
        {
            return false;
        }

        failure = DailyV1LabObservationStartFailure.StartupRejected;
        DailyV1Application candidateApplication = new DailyV1Application();
        DailyV1ApplicationHost fixedHost = new DailyV1ApplicationHost(
            fullRoot,
            SystemAtomicFileSlotOperations.Instance,
            stageCompleted: null,
            logOutput: null,
            dotNetIdentity: anchoredDotNetIdentity);

        DailyV1ApplicationStartPreparation preparation;
        P12AResult persistenceResult;
        string diagnostic;
        try
        {
            if (!candidateApplication.TryStartDotNetOnExistingApplication(
                    definitions, fixedHost,
                    out preparation, out persistenceResult, out diagnostic))
                return false;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is InvalidOperationException
            || exception is OverflowException
            || exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SecurityException)
        {
            return false;
        }

        failure = DailyV1LabObservationStartFailure.NoSaveNotPublished;
        if (preparation == null
            || persistenceResult == null
            || persistenceResult.Code != P12AResultCode.NoSave
            || !ReferenceEquals(preparation.PersistenceResult, persistenceResult)
            || preparation.Kind != DailyV1ApplicationStartPreparationKind.GenesisPrepared
            || preparation.CoreSession == null)
            return false;

        failure = DailyV1LabObservationStartFailure.PublishedSessionInvalid;
        DailyV1ActiveRuntimeSession published = candidateApplication.CurrentRuntimeSession;
        if (!ReferenceEquals(published, preparation.CoreSession)
            || !ReferenceEquals(published.Definitions, definitions)
            || published.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || published.P12CRoots == null
            || !published.P12CRoots.IsFor(published)
            || !WorldId.TryParse(published.WorldId?.Value, out _)
            || !ReferenceEquals(candidateApplication.BoundHost, fixedHost)
            || fixedHost.UnityV1Identity != null
            || fixedHost.StageCompleted != null
            || fixedHost.LogOutput != null
            || !ReferenceEquals(fixedHost.FileOperations,
                SystemAtomicFileSlotOperations.Instance))
            return false;

        run = new DailyV1LabObservationRun(candidateApplication, published, definitions);
        failure = DailyV1LabObservationStartFailure.None;
        return true;
    }

    /// <summary>Advances one real selected day and captures its same-cut facts.</summary>
    public DailyV1LabObservationAdvanceResult AdvanceAndObserve()
    {
        if (Volatile.Read(ref disposed) != 0)
            return Result(DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                DailyV1LabObservationDiagnosticCode.Disposed);
        if (!ReferenceEquals(ownerThread, Thread.CurrentThread)
            || ownerThreadId != Thread.CurrentThread.ManagedThreadId)
        {
            Interlocked.Exchange(ref stopped, 1);
            return Result(DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                DailyV1LabObservationDiagnosticCode.WrongThread);
        }
        if (Volatile.Read(ref stopped) != 0)
            return Result(DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                DailyV1LabObservationDiagnosticCode.RunStopped);
        if (Interlocked.CompareExchange(ref operationActive, 1, 0) != 0)
        {
            Interlocked.Exchange(ref stopped, 1);
            return Result(DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                DailyV1LabObservationDiagnosticCode.RuntimeAdvanceRejected);
        }

        try
        {
            DailyV1Application currentApplication = application;
            DailyV1ActiveRuntimeSession currentSession = session;
            if (currentApplication == null || currentSession == null
                || !ReferenceEquals(currentApplication.CurrentRuntimeSession, currentSession)
                || !ReferenceEquals(currentSession.WorldId, worldId)
                || currentSession.SimulationTime == null
                || currentSession.SimulationTime.AbsoluteDay != lastAbsoluteDay
                || currentSession.ExecutionBoundaryState.CompletedCoreSequence != lastCompletedSequence
                || lastAbsoluteDay == long.MaxValue
                || lastCompletedSequence == long.MaxValue)
            {
                Interlocked.Exchange(ref stopped, 1);
                return Result(DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                    DailyV1LabObservationDiagnosticCode.SessionChanged);
            }

            if (!currentApplication.TryBeginRuntimeAdvanceReservation(
                    currentSession,
                    out DailyV1ApplicationSessionGate<DailyV1ActiveRuntimeSession>.RuntimeAdvanceOperation operation,
                    out SimulationRuntimeAdvanceFailure reservationFailure))
            {
                Interlocked.Exchange(ref stopped, 1);
                return new DailyV1LabObservationAdvanceResult(
                    DailyV1LabObservationDisposition.RejectedBeforeAdvance,
                    DailyV1LabObservationDiagnosticCode.RuntimeAdvanceRejected,
                    reservationFailure,
                    null);
            }

            DailyV1LabObservationAdvanceResult completed =
                new DailyV1LabObservationAdvanceResult(
                    DailyV1LabObservationDisposition.AdvancedObservationUnavailable,
                    DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable,
                    null,
                    null);
            bool advanceAttempted = false;
            bool advanceSucceeded = false;
            try
            {
                using (operation)
                {
                    try
                    {
                        advanceAttempted = true;
                        if (!currentApplication.TryAdvanceDayUnderRuntimeAdvanceReservation(
                                currentSession, operation,
                                out SimulationRuntimeAdvanceFailure advanceFailure))
                        {
                            completed = new DailyV1LabObservationAdvanceResult(
                                DailyV1LabObservationDisposition.AdvanceUnsuccessful,
                                DailyV1LabObservationDiagnosticCode.RuntimeAdvanceFailed,
                                advanceFailure,
                                null);
                        }
                        else
                        {
                            advanceSucceeded = true;
                            if (!DailyV1AtlasObservationReader.TryCapture(
                                    currentApplication,
                                    currentSession,
                                    definitions,
                                    worldId,
                                    lastAbsoluteDay + 1L,
                                    lastCompletedSequence + 1L,
                                    out DailyV1AtlasObservation observation,
                                    out DailyV1LabObservationDiagnosticCode diagnosticCode))
                            {
                                completed = new DailyV1LabObservationAdvanceResult(
                                    DailyV1LabObservationDisposition.AdvancedObservationUnavailable,
                                    diagnosticCode,
                                    null,
                                    null);
                            }
                            else
                            {
                                completed = new DailyV1LabObservationAdvanceResult(
                                    DailyV1LabObservationDisposition.Captured,
                                    DailyV1LabObservationDiagnosticCode.None,
                                    null,
                                    observation);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        completed = advanceAttempted && !advanceSucceeded
                            ? new DailyV1LabObservationAdvanceResult(
                                DailyV1LabObservationDisposition.AdvanceUnsuccessful,
                                DailyV1LabObservationDiagnosticCode.RuntimeAdvanceFailed,
                                SimulationRuntimeAdvanceFailure.RuntimeFaulted,
                                null)
                            : new DailyV1LabObservationAdvanceResult(
                                DailyV1LabObservationDisposition.AdvancedObservationUnavailable,
                                DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable,
                                null,
                                null);
                    }
                }
            }
            catch (Exception)
            {
                // A completed read is not publishable until the operation
                // reservation has been disposed successfully.
                completed = advanceAttempted && !advanceSucceeded
                    ? new DailyV1LabObservationAdvanceResult(
                        DailyV1LabObservationDisposition.AdvanceUnsuccessful,
                        DailyV1LabObservationDiagnosticCode.RuntimeAdvanceFailed,
                        SimulationRuntimeAdvanceFailure.RuntimeFaulted,
                        null)
                    : new DailyV1LabObservationAdvanceResult(
                        DailyV1LabObservationDisposition.AdvancedObservationUnavailable,
                        DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable,
                        null,
                        null);
            }

            if (completed.Disposition == DailyV1LabObservationDisposition.Captured)
            {
                lastAbsoluteDay = completed.Observation.AbsoluteDay;
                lastCompletedSequence = completed.Observation.CompletedSequence;
            }
            else
            {
                Interlocked.Exchange(ref stopped, 1);
            }
            return completed;
        }
        finally
        {
            Volatile.Write(ref operationActive, 0);
        }
    }

    /// <summary>
    /// Drops this facade's application/session references on the creating
    /// owner thread. It does not dispose or replace the Core session itself.
    /// </summary>
    public void Dispose()
    {
        if (!ReferenceEquals(ownerThread, Thread.CurrentThread)
            || ownerThreadId != Thread.CurrentThread.ManagedThreadId)
            throw new InvalidOperationException(
                "The Daily-v1 observation run must release its references on its owner thread.");
        if (Volatile.Read(ref operationActive) != 0)
            throw new InvalidOperationException(
                "The Daily-v1 observation run cannot be released during an advance.");
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        Interlocked.Exchange(ref stopped, 1);
        session = null;
        application = null;
    }

    private static DailyV1LabObservationAdvanceResult Result(
        DailyV1LabObservationDisposition disposition,
        DailyV1LabObservationDiagnosticCode diagnosticCode) =>
        new DailyV1LabObservationAdvanceResult(
            disposition, diagnosticCode, null, null);

    private static bool TryValidateFreshRoot(string candidate, out string fullRoot)
    {
        fullRoot = null;
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        try
        {
            string full = Path.GetFullPath(candidate);
            string root = Path.GetPathRoot(full);
            if (string.IsNullOrWhiteSpace(root))
                return false;

            string trimmedFull = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string trimmedRoot = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(trimmedFull, trimmedRoot, StringComparison.OrdinalIgnoreCase)
                || File.Exists(full))
                return false;

            if (Directory.Exists(full))
            {
                if ((File.GetAttributes(full) & FileAttributes.ReparsePoint) != 0)
                    return false;
                using (IEnumerator<string> entries = Directory.EnumerateFileSystemEntries(full).GetEnumerator())
                {
                    if (entries.MoveNext()) return false;
                }
            }

            fullRoot = full;
            return true;
        }
        catch (Exception exception) when (exception is ArgumentException
            || exception is IOException
            || exception is UnauthorizedAccessException
            || exception is SecurityException
            || exception is NotSupportedException)
        {
            return false;
        }
    }
}

internal static class DailyV1AtlasObservationReader
{
    private const string SelectedCityDefinitionId = "CAMPO_VERDE";
    private const string SelectedItemDefinitionId = "TRIGO";
    private const int MaximumHexes = 4096;
    private const int MaximumLocations = 4096;
    private const int MaximumCities = 1024;
    private const int MaximumStringLength = 512;

    internal static bool TryCapture(
        DailyV1Application application,
        DailyV1ActiveRuntimeSession session,
        DailyV1EffectiveDefinitions expectedDefinitions,
        WorldId expectedWorldId,
        long expectedAbsoluteDay,
        long expectedCompletedSequence,
        out DailyV1AtlasObservation observation,
        out DailyV1LabObservationDiagnosticCode diagnosticCode)
    {
        observation = null;
        diagnosticCode = DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable;
        if (application == null || session == null || expectedDefinitions == null
            || expectedWorldId == null
            || !ReferenceEquals(application.CurrentRuntimeSession, session)
            || !ReferenceEquals(session.WorldId, expectedWorldId)
            || !ReferenceEquals(session.Definitions, expectedDefinitions)
            || session.AdmissionContext?.Profile
                != SimulationRuntimeAdmissionProfile.UnityBootstrapDailyV1
            || session.RuntimeAdvanceBoundary
                is not DailyV1CoreSelectedRuntimeAdvanceBoundary boundary)
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.SessionChanged;
            return false;
        }

        DailyV1ApplicationHost host = application.BoundHost;
        if (host == null || host.DotNetIdentity == null || host.UnityV1Identity != null
            || host.StageCompleted != null || host.LogOutput != null
            || !ReferenceEquals(host.FileOperations, SystemAtomicFileSlotOperations.Instance))
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable;
            return false;
        }

        DailyV1ExecutionBoundaryState state = session.ExecutionBoundaryState;
        if (state == null || !state.AdmissionContext.IsOwnedByCurrentThread())
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable;
            return false;
        }

        if (state.ActiveOperationCount != 0 || state.AdvanceLeaseHeld
            || !state.HasSealedSelectedOwnerSectionCensusRegistry
            || !state.HasRuntimeProtocolOperationInventory
            || state.IsSelectedRuntimeCensusProtocolFaulted)
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable;
            return false;
        }

        DailyCaptureEligibilityToken token = state.CurrentCaptureToken;
        if (token == null
            || token.BoundaryProvenance != DailyCaptureBoundaryProvenance.CompletedAdvance
            || !ReferenceEquals(token.WorldId, expectedWorldId)
            || token.AbsoluteDay != expectedAbsoluteDay
            || token.CompletedCoreSequence != expectedCompletedSequence
            || session.SimulationTime == null
            || session.SimulationTime.AbsoluteDay != expectedAbsoluteDay
            || state.CompletedCoreSequence != expectedCompletedSequence
            || !state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false))
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.CompletedBoundaryUnavailable;
            return false;
        }

        if (!boundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false,
                out IReadOnlyList<OwnerSectionCensusSnapshot> before,
                out long beforeEpoch,
                out _)
            || beforeEpoch != token.MutationEpoch
            || !SameCensus(before, token.OwnerSections))
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable;
            return false;
        }

        if (!TryBuildDetachedObservation(
                session, expectedDefinitions, expectedWorldId,
                expectedAbsoluteDay, expectedCompletedSequence,
                token, before, beforeEpoch,
                out DailyV1AtlasObservation candidate))
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.InvalidSourceProjection;
            return false;
        }

        if (!ReferenceEquals(application.CurrentRuntimeSession, session)
            || !ReferenceEquals(session.WorldId, expectedWorldId)
            || !ReferenceEquals(state.CurrentCaptureToken, token)
            || state.ActiveOperationCount != 0
            || state.AdvanceLeaseHeld
            || !state.HasValidCompletedDailyBoundaryAdmission(boundary, sessionReplacement: false)
            || !boundary.TryCaptureDailyBoundaryAdmissionEvidence(
                sessionReplacement: false,
                out IReadOnlyList<OwnerSectionCensusSnapshot> after,
                out long afterEpoch,
                out _)
            || afterEpoch != beforeEpoch
            || !SameCensus(before, after))
        {
            diagnosticCode = DailyV1LabObservationDiagnosticCode.SourceEvidenceUnavailable;
            return false;
        }

        observation = candidate;
        diagnosticCode = DailyV1LabObservationDiagnosticCode.None;
        return true;
    }

    internal static bool TryCopyScopedMarketRows(
        DailyV1MarketOwner market,
        DailyV1EffectiveDefinitions definitions,
        string cityRuntimeId,
        DailyV1ItemDefinition item,
        out IReadOnlyList<DailyV1AtlasMarketStockObservation> rows)
    {
        rows = null;
        if (market == null || definitions == null || string.IsNullOrWhiteSpace(cityRuntimeId)
            || item == null || !ReferenceEquals(market.Definitions, definitions)
            || !definitions.Items.TryGetValue(item.DefinitionId, out DailyV1ItemDefinition admittedItem)
            || !ReferenceEquals(admittedItem, item))
            return false;

        HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
        int targetCount = 0;
        DailyV1MarketItemState targetOwnerRow = null;
        foreach (DailyV1MarketItemState ownerRow in market.Items)
        {
            if (ownerRow == null || ownerRow.Item == null
                || !IsValidIdentity(ownerRow.Item.DefinitionId, allowNull: false)
                || !definitions.Items.TryGetValue(ownerRow.Item.DefinitionId,
                    out DailyV1ItemDefinition exactItem)
                || !ReferenceEquals(exactItem, ownerRow.Item)
                || ownerRow.Amount < 0
                || !ids.Add(ownerRow.Item.DefinitionId))
                return false;

            if (ReferenceEquals(ownerRow.Item, item))
            {
                targetCount++;
                targetOwnerRow = ownerRow;
            }
        }

        if (targetCount > 1) return false;
        DailyV1MarketItemState presenceSensitive = market.GetItem(item);
        if ((presenceSensitive == null) != (targetCount == 0))
            return false;
        if (targetCount == 0)
        {
            rows = new ReadOnlyCollection<DailyV1AtlasMarketStockObservation>(
                new List<DailyV1AtlasMarketStockObservation>());
            return true;
        }

        if (presenceSensitive == null || targetOwnerRow == null
            || !ReferenceEquals(presenceSensitive.Item, item)
            || presenceSensitive.Amount != targetOwnerRow.Amount
            || presenceSensitive.Amount < 0)
            return false;

        rows = new ReadOnlyCollection<DailyV1AtlasMarketStockObservation>(
            new List<DailyV1AtlasMarketStockObservation>
            {
                new DailyV1AtlasMarketStockObservation(
                    cityRuntimeId, item.DefinitionId, presenceSensitive.Amount)
            });
        return true;
    }

    private static bool TryBuildDetachedObservation(
        DailyV1ActiveRuntimeSession session,
        DailyV1EffectiveDefinitions definitions,
        WorldId expectedWorldId,
        long expectedAbsoluteDay,
        long expectedCompletedSequence,
        DailyCaptureEligibilityToken token,
        IReadOnlyList<OwnerSectionCensusSnapshot> ownerSections,
        long mutationEpoch,
        out DailyV1AtlasObservation observation)
    {
        observation = null;
        if (session.P12CRoots == null
            || !session.P12CRoots.IsFor(session)
            || !ReferenceEquals(session.P12CRoots.ExecutionBoundaryState,
                session.ExecutionBoundaryState)
            || !ReferenceEquals(session.OwnerGraph, session.ExecutionBoundaryState.SelectedRuntimeOwnerGraph)
            || !ReferenceEquals(session.OwnerGraph?.Definitions, definitions))
            return false;

        SpatialAuthorityStore spatial = session.P12CRoots.SpatialAuthority;
        DailyV1SelectedRuntimeOwnerGraph graph = session.OwnerGraph;
        DailyV1RuntimeIdentityIndex identities = graph.RuntimeIdentityIndex;
        LegacySpatialAnchorBindingStore anchors = graph.LegacySpatialAnchors;
        if (spatial == null || identities == null || anchors == null
            || !ReferenceEquals(anchors.SpatialLocationLookup, spatial))
            return false;

        SpatialAuthorityInvariantReport spatialReport = spatial.ValidateInvariants();
        SpatialAnchorBindingInvariantReport anchorReport = anchors.ValidateInvariants();
        if (spatialReport == null || !spatialReport.IsValid
            || anchorReport == null || !anchorReport.IsValid)
            return false;

        if (expectedAbsoluteDay <= 0L || expectedCompletedSequence <= 0L
            || token.AbsoluteDay != expectedAbsoluteDay
            || token.CompletedCoreSequence != expectedCompletedSequence
            || token.MutationEpoch != mutationEpoch
            || !WorldId.TryParse(expectedWorldId.Value, out _)
            || !IsLowerSha256(session.ProfileFingerprint)
            || !IsLowerSha256(definitions.ContentFingerprint))
            return false;

        if (!TryGetWitness(ownerSections, P12P8AImageSectionContract.HexSectionId,
                spatial, spatial.Hexes.Count, spatial.Revision, out _)
            || !TryGetWitness(ownerSections, P12P8AImageSectionContract.LocationSectionId,
                spatial, spatial.Locations.Count, spatial.Revision, out _)
            || !TryGetWitness(ownerSections, LegacySpatialAnchorBindingCensusProvider.SectionId,
                anchors, anchors.Count, anchors.Revision, out _))
            return false;

        OwnerSectionCensusWitness cityIdentity = identities.GetOwnerSectionCensusWitness(
            DailyV1RuntimeIdentityKind.City);
        if (!TryGetWitness(ownerSections, P12CRuntimeIdentitySectionContract.CitiesSectionId,
                identities, cityIdentity.Cardinality, cityIdentity.Revision, out _)
            || cityIdentity.Cardinality != graph.Cities.Count
            || cityIdentity.Revision != identities.CensusRevision
            || definitions.Cities == null
            || graph.Cities.Count != definitions.Cities.Count
            || graph.Cities.Count > MaximumCities)
            return false;

        List<DailyV1CityRuntimeOwners> cityOwners = new List<DailyV1CityRuntimeOwners>(graph.Cities.Count);
        Dictionary<string, DailyV1CityRuntimeOwners> citiesByRuntimeId =
            new Dictionary<string, DailyV1CityRuntimeOwners>(StringComparer.Ordinal);
        Dictionary<string, DailyV1CityRuntimeOwners> citiesByDefinition =
            new Dictionary<string, DailyV1CityRuntimeOwners>(StringComparer.Ordinal);
        foreach (DailyV1CityRuntimeOwners city in graph.Cities)
        {
            if (city == null || string.IsNullOrWhiteSpace(city.RuntimeId)
                || !IsValidIdentity(city.RuntimeId, allowNull: false)
                || city.Definition == null || !ReferenceEquals(city.Definitions, definitions)
                || !definitions.Cities.TryGetValue(city.Definition.DefinitionId,
                    out DailyV1CityDefinition exactCityDefinition)
                || !ReferenceEquals(exactCityDefinition, city.Definition)
                || !identities.HasIdentity(city.RuntimeId, DailyV1RuntimeIdentityKind.City)
                || !citiesByRuntimeId.TryAdd(city.RuntimeId, city)
                || !citiesByDefinition.TryAdd(city.Definition.DefinitionId, city))
                return false;
            string label = city.Definition.Values?.cityName;
            if (!IsValidIdentity(label, allowNull: true))
                return false;
            cityOwners.Add(city);
        }

        foreach (KeyValuePair<string, DailyV1CityDefinition> entry in definitions.Cities)
        {
            if (entry.Value == null
                || !citiesByDefinition.TryGetValue(entry.Key, out DailyV1CityRuntimeOwners owner)
                || !ReferenceEquals(owner.Definition, entry.Value))
                return false;
        }
        cityOwners.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.RuntimeId, right.RuntimeId));

        if (!definitions.Cities.TryGetValue(SelectedCityDefinitionId,
                out DailyV1CityDefinition scopedCityDefinition)
            || !citiesByDefinition.TryGetValue(SelectedCityDefinitionId,
                out DailyV1CityRuntimeOwners scopedCity)
            || !ReferenceEquals(scopedCity.Definition, scopedCityDefinition)
            || scopedCity.Market == null
            || !ReferenceEquals(scopedCity.Market.Definitions, definitions)
            || !definitions.Items.TryGetValue(SelectedItemDefinitionId,
                out DailyV1ItemDefinition scopedItem))
            return false;

        string encodedCityKey;
        try
        {
            encodedCityKey = P12DCityOwnerImageSectionCodec.EncodeCityKeyForSection(
                scopedCity.RuntimeId);
        }
        catch (ArgumentException)
        {
            return false;
        }

        if (!TryGetWitness(ownerSections,
                DailyV1SelectedOwnerSectionCensusContract.CityMarketStockRowsSectionPrefix
                    + encodedCityKey,
                scopedCity.Market,
                scopedCity.Market.Items.Count,
                scopedCity.Market.Revision,
                out _))
            return false;

        if (!TryCopyGeography(spatial,
                out List<DailyV1AtlasHexObservation> hexes,
                out List<DailyV1AtlasLocationObservation> locations))
            return false;

        if (!TryCopyCityAnchors(anchors, spatial, citiesByRuntimeId,
                out Dictionary<string, string> p8LocationsByCity))
            return false;

        List<DailyV1AtlasCityObservation> cities =
            new List<DailyV1AtlasCityObservation>(cityOwners.Count);
        foreach (DailyV1CityRuntimeOwners city in cityOwners)
        {
            p8LocationsByCity.TryGetValue(city.RuntimeId, out string p8LocationId);
            cities.Add(new DailyV1AtlasCityObservation(
                city.RuntimeId,
                city.Definition.Values.cityName,
                p8LocationId));
        }

        if (!TryCopyScopedMarketRows(
                scopedCity.Market, definitions, scopedCity.RuntimeId, scopedItem,
                out IReadOnlyList<DailyV1AtlasMarketStockObservation> marketRows)
            || !TryComputeEffectiveInputFingerprint(
                session.ProfileFingerprint, definitions.ContentFingerprint,
                out string effectiveInputFingerprint))
            return false;

        observation = new DailyV1AtlasObservation(
            expectedWorldId.Value,
            expectedAbsoluteDay,
            expectedCompletedSequence,
            effectiveInputFingerprint,
            spatial.Revision,
            hexes,
            locations,
            identities.CensusRevision,
            anchors.Revision,
            cities,
            scopedCity.RuntimeId,
            scopedItem.DefinitionId,
            scopedCity.Market.Revision,
            new List<DailyV1AtlasMarketStockObservation>(marketRows));
        return true;
    }

    private static bool TryCopyGeography(
        SpatialAuthorityStore spatial,
        out List<DailyV1AtlasHexObservation> hexes,
        out List<DailyV1AtlasLocationObservation> locations)
    {
        hexes = null;
        locations = null;
        IReadOnlyList<HexRecord> ownerHexes = spatial.Hexes;
        IReadOnlyList<LocationRecord> ownerLocations = spatial.Locations;
        if (ownerHexes == null || ownerLocations == null
            || ownerHexes.Count == 0 || ownerLocations.Count == 0
            || ownerHexes.Count > MaximumHexes || ownerLocations.Count > MaximumLocations)
            return false;

        HashSet<string> hexIds = new HashSet<string>(StringComparer.Ordinal);
        HashSet<HexCoordinate> coordinates = new HashSet<HexCoordinate>();
        List<DailyV1AtlasHexObservation> copiedHexes =
            new List<DailyV1AtlasHexObservation>(ownerHexes.Count);
        foreach (HexRecord hex in ownerHexes)
        {
            if (hex?.Id == null || !IsValidIdentity(hex.Id.Value, allowNull: false)
                || !hexIds.Add(hex.Id.Value))
                return false;

            if (hex.Coordinate.HasValue != (hex.TerrainReference != null))
                return false;

            if (!hex.Coordinate.HasValue)
            {
                copiedHexes.Add(new DailyV1AtlasHexObservation(
                    hex.Id.Value, null, null, null, null));
                continue;
            }

            HexCoordinate coordinate = hex.Coordinate.Value;
            string terrainId = hex.TerrainReference.TerrainDefinitionId?.Value;
            string terrainRevision = hex.TerrainReference.AuthoredRevisionToken;
            if (!coordinates.Add(coordinate)
                || !IsValidIdentity(terrainId, allowNull: false)
                || !IsValidIdentity(terrainRevision, allowNull: false))
                return false;
            copiedHexes.Add(new DailyV1AtlasHexObservation(
                hex.Id.Value, coordinate.Q, coordinate.R, terrainId, terrainRevision));
        }

        HashSet<string> locationIds = new HashSet<string>(StringComparer.Ordinal);
        List<DailyV1AtlasLocationObservation> copiedLocations =
            new List<DailyV1AtlasLocationObservation>(ownerLocations.Count);
        foreach (LocationRecord location in ownerLocations)
        {
            if (location?.Id == null || location.AnchorHexId == null
                || !IsValidIdentity(location.Id.Value, allowNull: false)
                || !IsValidIdentity(location.AnchorHexId.Value, allowNull: false)
                || !locationIds.Add(location.Id.Value)
                || !hexIds.Contains(location.AnchorHexId.Value))
                return false;
            copiedLocations.Add(new DailyV1AtlasLocationObservation(
                location.Id.Value, location.AnchorHexId.Value));
        }

        copiedHexes.Sort((left, right) => StringComparer.Ordinal.Compare(left.HexId, right.HexId));
        copiedLocations.Sort((left, right) =>
            StringComparer.Ordinal.Compare(left.LocationId, right.LocationId));
        hexes = copiedHexes;
        locations = copiedLocations;
        return true;
    }

    private static bool TryCopyCityAnchors(
        LegacySpatialAnchorBindingStore anchors,
        SpatialAuthorityStore spatial,
        Dictionary<string, DailyV1CityRuntimeOwners> citiesByRuntimeId,
        out Dictionary<string, string> p8LocationsByCity)
    {
        p8LocationsByCity = new Dictionary<string, string>(StringComparer.Ordinal);
        HashSet<string> boundLocations = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<SpatialAnchorBinding> bindings = anchors.Bindings;
        if (bindings == null || bindings.Count != anchors.Count)
            return false;

        foreach (SpatialAnchorBinding binding in bindings)
        {
            if (binding?.OwnerId == null || binding.LocationId == null
                || binding.OwnerId.Kind != SpatialAnchorOwnerKind.City
                || !IsValidIdentity(binding.OwnerId.Value, allowNull: false)
                || !IsValidIdentity(binding.LocationId.Value, allowNull: false)
                || !citiesByRuntimeId.ContainsKey(binding.OwnerId.Value)
                || !spatial.TryGet(binding.LocationId, out _)
                || !p8LocationsByCity.TryAdd(binding.OwnerId.Value, binding.LocationId.Value)
                || !boundLocations.Add(binding.LocationId.Value))
                return false;
        }
        return p8LocationsByCity.Count == bindings.Count;
    }

    private static bool TryGetWitness(
        IReadOnlyList<OwnerSectionCensusSnapshot> sections,
        string sectionId,
        object ownerIdentity,
        int cardinality,
        long revision,
        out OwnerSectionCensusSnapshot witness)
    {
        witness = null;
        if (sections == null || string.IsNullOrWhiteSpace(sectionId)
            || ownerIdentity == null || cardinality < 0 || revision < 0L)
            return false;
        foreach (OwnerSectionCensusSnapshot candidate in sections)
        {
            if (candidate == null
                || !string.Equals(candidate.SectionId, sectionId, StringComparison.Ordinal))
                continue;
            if (witness != null) return false;
            witness = candidate;
        }
        return witness != null
            && ReferenceEquals(witness.OwnerInstanceIdentity, ownerIdentity)
            && witness.Cardinality == cardinality
            && witness.Revision == revision;
    }

    internal static bool SameCensus(
        IReadOnlyList<OwnerSectionCensusSnapshot> left,
        IReadOnlyList<OwnerSectionCensusSnapshot> right)
    {
        if (left == null || right == null || left.Count != right.Count)
            return false;
        Dictionary<string, OwnerSectionCensusSnapshot> rightById =
            new Dictionary<string, OwnerSectionCensusSnapshot>(StringComparer.Ordinal);
        foreach (OwnerSectionCensusSnapshot section in right)
        {
            if (section == null || !rightById.TryAdd(section.SectionId, section))
                return false;
        }
        foreach (OwnerSectionCensusSnapshot section in left)
        {
            if (section == null
                || !rightById.TryGetValue(section.SectionId,
                    out OwnerSectionCensusSnapshot matched)
                || section.SchemaVersion != matched.SchemaVersion
                || section.Role != matched.Role
                || !ReferenceEquals(section.OwnerInstanceIdentity, matched.OwnerInstanceIdentity)
                || section.Cardinality != matched.Cardinality
                || section.Revision != matched.Revision)
                return false;
        }
        return rightById.Count == left.Count;
    }

    private static bool TryComputeEffectiveInputFingerprint(
        string profileFingerprint,
        string contentFingerprint,
        out string fingerprint)
    {
        fingerprint = null;
        if (!IsLowerSha256(profileFingerprint) || !IsLowerSha256(contentFingerprint))
            return false;
        try
        {
            UTF8Encoding utf8 = new UTF8Encoding(false, true);
            using (MemoryStream framed = new MemoryStream())
            {
                WriteFrame(framed, utf8.GetBytes("lab-observation-effective/v1"));
                WriteFrame(framed, utf8.GetBytes(profileFingerprint));
                WriteFrame(framed, utf8.GetBytes(contentFingerprint));
                using (SHA256 sha = SHA256.Create())
                    fingerprint = LowerHex(sha.ComputeHash(framed.ToArray()));
            }
            return true;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    private static void WriteFrame(Stream stream, byte[] bytes)
    {
        uint length = checked((uint)bytes.Length);
        stream.WriteByte((byte)(length >> 24));
        stream.WriteByte((byte)(length >> 16));
        stream.WriteByte((byte)(length >> 8));
        stream.WriteByte((byte)length);
        stream.Write(bytes, 0, bytes.Length);
    }

    private static string LowerHex(byte[] bytes)
    {
        const string digits = "0123456789abcdef";
        char[] result = new char[bytes.Length * 2];
        for (int i = 0; i < bytes.Length; i++)
        {
            result[i * 2] = digits[bytes[i] >> 4];
            result[i * 2 + 1] = digits[bytes[i] & 0x0f];
        }
        return new string(result);
    }

    private static bool IsLowerSha256(string value)
    {
        if (value == null || value.Length != 64) return false;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')))
                return false;
        }
        return true;
    }

    private static bool IsValidIdentity(string value, bool allowNull)
    {
        if (value == null) return allowNull;
        if (value.Length == 0 || value.Length > MaximumStringLength
            || string.IsNullOrWhiteSpace(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (char.IsControl(c)) return false;
            if (char.IsHighSurrogate(c))
            {
                if (i + 1 >= value.Length || !char.IsLowSurrogate(value[i + 1]))
                    return false;
                i++;
            }
            else if (char.IsLowSurrogate(c))
            {
                return false;
            }
        }
        return true;
    }
}
