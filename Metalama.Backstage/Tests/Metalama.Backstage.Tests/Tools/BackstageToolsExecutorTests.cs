// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Tools;

public class BackstageToolsExecutorTests : TestsBase
{
    public BackstageToolsExecutorTests( ITestOutputHelper logger ) : base( logger ) { }

    private void Test( BackstageTool tool )
    {
        var executor = this.ServiceProvider.GetRequiredBackstageService<IBackstageToolsExecutor>();
        _ = executor.Start( tool, "test" );

        Assert.Single( this.ProcessExecutor.StartedProcesses );
    }
    
    [Fact]
    public void WorkerToolExecutes() => this.Test( BackstageTool.Worker );
    
    [Fact]
    public void DesktopWindowsToolExecutes() => this.Test( BackstageTool.DesktopWindows );
}