using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using NUnit.Framework;

public sealed class DailyV1DotNetHostEnvironmentIdentityFactoryTests
{
    [Test]
    public void CapturesOnlyExplicitAssembliesAndPinsNet8CoreClrIdentity()
    {
        DailyV1HostEnvironmentIdentity identity =
            DailyV1DotNetHostEnvironmentIdentityFactory.Capture(typeof(DailyV1DotNetHostEnvironmentIdentityFactoryTests));

        Assembly[] expectedAssemblies =
        {
            typeof(DailyV1DotNetHostEnvironmentIdentityFactoryTests).Assembly,
            typeof(DailyV1DotNetHostEnvironmentIdentityFactory).Assembly,
            typeof(DailyV1ApplicationHost).Assembly
        };

        Assert.That(identity.HostKind, Is.EqualTo(1));
        Assert.That(identity.TargetFramework, Is.EqualTo("net8.0"));
        Assert.That(identity.Assemblies.Select(value => value.SimpleName),
            Is.EquivalentTo(expectedAssemblies.Select(value => value.GetName().Name).Distinct()));
        Assert.That(identity.Assemblies.Count, Is.EqualTo(expectedAssemblies.Select(value => value.FullName).Distinct().Count()));
        Assert.That(identity.Assemblies.All(value => Guid.TryParseExact(value.Mvid, "D", out _)), Is.True);
        Assert.That(identity.Assemblies.Select(value => value.Mvid),
            Is.EquivalentTo(expectedAssemblies.Select(value => value.ManifestModule.ModuleVersionId.ToString("D").ToLowerInvariant()).Distinct()));
        Assert.That(identity.NumericProfileId, Does.StartWith("p12.numeric.ieee754-binary32-binary64/canonical-le-wire-v1/"));
        Assert.That(identity.NumericProfileVersion, Is.EqualTo(1));
        Assert.That(identity.Assemblies.Select(value => value.SimpleName),
            Does.Not.Contain("System.Private.CoreLib"));
        Assert.That(identity.RuntimeFrameworkDescription, Is.EqualTo(RuntimeInformation.FrameworkDescription));
        Version runtimeVersion = Environment.Version;
        string expectedRuntimeVersion = new Version(runtimeVersion.Major, runtimeVersion.Minor,
            Math.Max(0, runtimeVersion.Build), Math.Max(0, runtimeVersion.Revision)).ToString();
        Assert.That(identity.RuntimeVersion, Is.EqualTo(expectedRuntimeVersion));
        Assert.That(identity.OperatingSystemDescription, Is.EqualTo(RuntimeInformation.OSDescription.Trim().Normalize()));
        Assert.That(identity.RuntimeIdentifier, Is.EqualTo(RuntimeInformation.RuntimeIdentifier.ToLowerInvariant()));
        Assert.That(identity.ProcessArchitecture, Is.EqualTo(RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant()));
    }

    [Test]
    public void RejectsMissingOrNonNet8HostEntryFramework()
    {
        Assert.Throws<ArgumentNullException>(() => DailyV1DotNetHostEnvironmentIdentityFactory.Capture(null));
        Assert.Throws<InvalidOperationException>(() => DailyV1DotNetHostEnvironmentIdentityFactory.Capture(typeof(DailyV1ApplicationHost)));

        AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName("DailyV1HostIdentityWithoutTargetFramework"), AssemblyBuilderAccess.Run);
        Type entryPoint = assembly.DefineDynamicModule("main")
            .DefineType("HostEntryPoint", TypeAttributes.Public | TypeAttributes.Class)
            .CreateType();

        Assert.That(entryPoint.Assembly.GetCustomAttribute<System.Runtime.Versioning.TargetFrameworkAttribute>(), Is.Null);
        Assert.Throws<InvalidOperationException>(() => DailyV1DotNetHostEnvironmentIdentityFactory.Capture(entryPoint));
    }
}
