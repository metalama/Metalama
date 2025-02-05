// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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