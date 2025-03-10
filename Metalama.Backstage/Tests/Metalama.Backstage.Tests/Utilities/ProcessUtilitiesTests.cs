// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Utilities;

public sealed class ProcessUtilitiesTests : TestsBase
{
    public ProcessUtilitiesTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void ParentProcessesCanBeRetrieved()
    {
        var logger = this.ServiceProvider.GetLoggerFactory().GetLogger( nameof(ProcessUtilitiesTests) );
        var parentProcesses = ProcessUtilities.GetParentProcesses( logger );

        Assert.NotEmpty( parentProcesses );

        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        Assert.All(
            parentProcesses,
            p =>
            {
                Assert.NotEqual( 0, p.ProcessId );
                Assert.NotNull( p.ProcessName );
                Assert.NotEmpty( p.ProcessName! );
            } );
    }
}