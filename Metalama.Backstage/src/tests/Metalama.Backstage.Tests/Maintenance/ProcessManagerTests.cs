// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tools;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Maintenance;

public sealed class ProcessManagerTests : TestsBase
{
    public ProcessManagerTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void KillList_IncludesBackstageWorker()
    {
        // Regression test for #1685: 'metalama kill' must also terminate the Backstage Worker.
        // The Worker is launched via 'dotnet', so it must be matched as a DotNet module.
        var spec = ProcessManagerBase.ProcessesToKill.Single( p => p.Name == BackstageTool.Worker.Name );

        Assert.True( spec.IsDotNet, "The Backstage Worker runs under 'dotnet' and must be matched as a DotNet module." );
        Assert.True( spec.CanShutdownOrKill, "The Backstage Worker must be killable." );
    }

    [Fact]
    public void KillList_IncludesBackstageDesktop()
    {
        // Regression test for #1685: 'metalama kill' must also terminate the Backstage Desktop tray app,
        // which is a standalone '.exe'.
        var spec = ProcessManagerBase.ProcessesToKill.Single( p => p.Name == BackstageTool.DesktopWindows.Name );

        Assert.True( spec.IsStandaloneProcess, "The Backstage Desktop app is a standalone process." );
        Assert.True( spec.CanShutdownOrKill, "The Backstage Desktop app must be killable." );
    }
}
