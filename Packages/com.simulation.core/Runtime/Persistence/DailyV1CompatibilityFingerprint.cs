using System;
using System.Collections.Generic;

/// <summary>
/// Host-neutral semantic identity shared by the Unity-v1 and .NET-v2
/// compatibility descriptors. Host runtime and assembly identity stay at the
/// host adapter boundary.
/// </summary>
public sealed class DailyV1CompatibilityFingerprint
{
    public const string CalendarId = "simulation-calendar/v1";
    public const int CalendarVersion = 1;

    private DailyV1CompatibilityFingerprint(string configurationFingerprint,
        string contentFingerprint, string providerFingerprint,
        string calendarFingerprint, string p9Fingerprint, string p8Fingerprint)
    {
        ConfigurationFingerprint = configurationFingerprint;
        ContentFingerprint = contentFingerprint;
        ProviderFingerprint = providerFingerprint;
        CalendarFingerprint = calendarFingerprint;
        P9Fingerprint = p9Fingerprint;
        P8Fingerprint = p8Fingerprint;
    }

    public string ConfigurationFingerprint { get; }
    public string ContentFingerprint { get; }
    public string ProviderFingerprint { get; }
    public string CalendarFingerprint { get; }
    public string P9Fingerprint { get; }
    public string P8Fingerprint { get; }

    /// <summary>Combines this shared semantic identity with the fixed .NET host manifest.</summary>
    public P12ACompatibilityDescriptorV2 CreateDotNetV2Descriptor(
        DailyV1HostEnvironmentIdentity hostIdentity)
    {
        if (hostIdentity == null) throw new ArgumentNullException(nameof(hostIdentity));
        return new P12ACompatibilityDescriptorV2(
            "UnityBootstrap-Daily-v1", 1, hostIdentity.HostKind,
            hostIdentity.RuntimeFrameworkDescription, hostIdentity.RuntimeVersion,
            hostIdentity.OperatingSystemDescription, hostIdentity.RuntimeIdentifier,
            hostIdentity.ProcessArchitecture, hostIdentity.TargetFramework,
            hostIdentity.Assemblies, hostIdentity.NumericProfileId,
            hostIdentity.NumericProfileVersion, ConfigurationFingerprint,
            ContentFingerprint, ProviderFingerprint, CalendarId, CalendarVersion,
            CalendarFingerprint, P9Fingerprint, P8Fingerprint);
    }

    public static bool TryCreate(DailyV1EffectiveDefinitions definitions,
        string configurationFingerprint, IReadOnlyList<string> requiredSections,
        IReadOnlyList<string> emptySections, IReadOnlyList<string> npcSectionPrefixes,
        IReadOnlyList<string> citySectionPrefixes, string p9Fingerprint,
        string p8Fingerprint, out DailyV1CompatibilityFingerprint fingerprint)
    {
        fingerprint = null;
        if (definitions == null || definitions.Calendar == null
            || definitions.ContentProjection == null
            || string.IsNullOrWhiteSpace(definitions.ContentFingerprint)
            || string.IsNullOrWhiteSpace(configurationFingerprint)
            || string.IsNullOrWhiteSpace(p9Fingerprint)
            || string.IsNullOrWhiteSpace(p8Fingerprint)
            || requiredSections == null || emptySections == null
            || npcSectionPrefixes == null || citySectionPrefixes == null)
            return false;

        try
        {
            string provider = DailyV1DefinitionIdentity.CreateProviderFingerprint(
                requiredSections, emptySections, npcSectionPrefixes, citySectionPrefixes);
            string calendar = DailyV1DefinitionIdentity.CreateCalendarFingerprint(definitions.Calendar);
            fingerprint = new DailyV1CompatibilityFingerprint(configurationFingerprint,
                definitions.ContentFingerprint, provider, calendar, p9Fingerprint, p8Fingerprint);
            return true;
        }
        catch (ArgumentException)
        {
            fingerprint = null;
            return false;
        }
    }
}
