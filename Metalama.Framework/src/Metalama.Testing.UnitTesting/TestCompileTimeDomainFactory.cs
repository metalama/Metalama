// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Testing.UnitTesting;

/// <summary>
/// The <see cref="ICompileTimeDomainFactory"/> of unit tests, which differs from the production one only in creating an
/// unloadable domain where the platform supports it.
/// </summary>
/// <remarks>
/// <para>
/// The domain selection logic deliberately lives in <see cref="CompileTimeDomainFactoryBase"/> rather than here. It was
/// duplicated, and the copies drifted, so a production fix was invisible to every unit test. See #1749.
/// </para>
/// <para>
/// On .NET, all domains of the process load shared assemblies, which are the extension assemblies and their dependencies, from a
/// single <c>SharedAssemblyLoadContext</c>. This type does not exist on .NET Framework. Each test creates new domains, so without this context every test would load and
/// compile the same extension assemblies again. See #2254.
/// </para>
/// </remarks>
internal sealed class TestCompileTimeDomainFactory : CompileTimeDomainFactoryBase
{
#if NET5_0_OR_GREATER
    /// <summary>
    /// The context from which all test domains of the process load shared assemblies.
    /// </summary>
    private static readonly SharedAssemblyLoadContext _sharedContext = new();
#endif

    private readonly GlobalServiceProvider _serviceProvider;

    public TestCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    protected override CompileTimeDomain CreateDomainCore()
    {
#if NET5_0_OR_GREATER
        var unloadableDomain = new UnloadableCompileTimeDomain( this._serviceProvider, _sharedContext );
        unloadableDomain.UnloadError += _ => MemoryDumpHelper.CaptureMiniDumpOnce();

        return unloadableDomain;
#else
        return new CompileTimeDomain( this._serviceProvider );
#endif
    }
}
