// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;

namespace Metalama.Testing.UnitTesting;

internal sealed class TestCompileTimeDomainFactory : ICompileTimeDomainFactory
{
    private readonly GlobalServiceProvider _serviceProvider;

    public TestCompileTimeDomainFactory( GlobalServiceProvider serviceProvider )
    {
        this._serviceProvider = serviceProvider;
    }

    public CompileTimeDomain CreateDomain()
#if NET5_0_OR_GREATER
    {
        var domain = new UnloadableCompileTimeDomain( this._serviceProvider );
        domain.UnloadError += _ => MemoryDumpHelper.CaptureMiniDumpOnce();

        return domain;
    }
#else
        => new( this._serviceProvider );
#endif
}