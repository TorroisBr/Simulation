using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

#if !NET8_0
#error The fixed Daily-v1 .NET host identity adapter must target net8.0.
#endif

/// <summary>
/// Captures the fixed Daily-v1 .NET host identity from the explicit host entry
/// type and this adapter's compile-time type-anchor manifest. This deliberately
/// does not inspect the process-wide loaded assembly set.
/// </summary>
public static class DailyV1DotNetHostEnvironmentIdentityFactory
{
    // Keep the compile-time anchors in one reviewed manifest. Runtime and all
    // selected owners are intentionally compiled into Simulation.Core by the
    // approved assembly cut, so the Core host and persistence anchors share its
    // one assembly identity. Add an anchor here only when the approved source
    // boundary assigns one of those components to a distinct assembly.
    private static readonly Type[] FixedTypeAnchors =
    {
        typeof(DailyV1DotNetHostEnvironmentIdentityFactory),
        typeof(DailyV1ApplicationHost),
        typeof(P12ACompatibilityDescriptorV2),
        typeof(P12ANumericProfileIdentity)
    };

    private const string AcceptedFrameworkName = ".NETCoreApp,Version=v8.0";

    public static DailyV1HostEnvironmentIdentity Capture(Type hostEntryPointType)
    {
        if (hostEntryPointType == null)
            throw new ArgumentNullException(nameof(hostEntryPointType));

        Assembly hostEntryAssembly = hostEntryPointType.Assembly;
        TargetFrameworkAttribute targetFramework =
            hostEntryAssembly.GetCustomAttribute<TargetFrameworkAttribute>();
        if (targetFramework == null
            || !string.Equals(targetFramework.FrameworkName, AcceptedFrameworkName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The fixed Daily-v1 .NET host entry assembly must declare .NETCoreApp,Version=v8.0.");
        }

        List<Assembly> explicitHostAssemblies = new List<Assembly>(FixedTypeAnchors.Length + 1)
        {
            hostEntryAssembly
        };
        HashSet<Assembly> seenAssemblies = new HashSet<Assembly> { hostEntryAssembly };
        for (int index = 0; index < FixedTypeAnchors.Length; index++)
        {
            Assembly assembly = FixedTypeAnchors[index].Assembly;
            if (seenAssemblies.Add(assembly))
                explicitHostAssemblies.Add(assembly);
        }

        List<P12AAssemblyIdentityV2> assemblies = new List<P12AAssemblyIdentityV2>(explicitHostAssemblies.Count);
        List<string> mvids = new List<string>(explicitHostAssemblies.Count);
        for (int index = 0; index < explicitHostAssemblies.Count; index++)
        {
            Assembly assembly = explicitHostAssemblies[index];
            if (assembly.IsDynamic)
                throw new InvalidOperationException("Dynamic assemblies cannot identify the fixed Daily-v1 host.");

            AssemblyName name = assembly.GetName();
            Version version = name.Version;
            if (string.IsNullOrWhiteSpace(name.Name) || version == null)
                throw new InvalidOperationException("An anchored host assembly has incomplete identity metadata.");

            string mvid = assembly.ManifestModule.ModuleVersionId.ToString("D");
            assemblies.Add(new P12AAssemblyIdentityV2(name.Name, version.ToString(), mvid));
            mvids.Add(mvid);
        }

        if (!P12ANumericProfileIdentity.TryCreateCoreClr(mvids, out string numericProfileId, out int numericProfileVersion))
            throw new InvalidOperationException("The explicit .NET host assembly set cannot establish the approved CoreCLR numeric profile.");

        return new DailyV1HostEnvironmentIdentity(
            1,
            RuntimeInformation.FrameworkDescription,
            Environment.Version.ToString(),
            RuntimeInformation.OSDescription,
            RuntimeInformation.RuntimeIdentifier,
            RuntimeInformation.ProcessArchitecture.ToString(),
            "net8.0",
            assemblies,
            numericProfileId,
            numericProfileVersion);
    }
}
