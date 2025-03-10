// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Tools;

public sealed class BackstageToolsExecutorTests : TestsBase
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