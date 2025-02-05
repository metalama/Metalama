// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Utilities;

public class ProcessUtilitiesTests : TestsBase
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