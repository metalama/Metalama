// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Utilities;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Utilities;

public sealed class ProcessUtilitiesTests : TestsBase
{
    public ProcessUtilitiesTests( ITestOutputHelper logger ) : base( logger ) { }

    private ILogger GetLogger() => this.ServiceProvider.GetLoggerFactory().GetLogger( nameof(ProcessUtilitiesTests) );

    [Fact]
    public void ParentProcessesCanBeRetrieved()
    {
        var logger = this.GetLogger();
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

    /// <summary>
    /// Regression test for #1729: the Visual Studio host processes (the main <c>devenv</c> process and its
    /// out-of-process Roslyn analysis host) must be treated as attended even though the out-of-process host runs in a
    /// non-interactive session. Otherwise telemetry, including exception reports, is silently disabled in the Roslyn
    /// analysis process (unlike Rider, which is whitelisted).
    /// </summary>
    [Theory]
    [InlineData( ProcessKind.RoslynCodeAnalysisService )]
    [InlineData( ProcessKind.DevEnv )]
    public void VisualStudioHostProcessIsAttendedEvenWhenSessionIsNonInteractive( ProcessKind processKind )
        => Assert.False(
            ProcessUtilities.IsProcessUnattended( processKind, isNonInteractiveSession: true, Array.Empty<string>(), this.GetLogger() ) );

    [Fact]
    public void NonInteractiveBuildProcessIsUnattended()
        => Assert.True(
            ProcessUtilities.IsProcessUnattended( ProcessKind.Compiler, isNonInteractiveSession: true, Array.Empty<string>(), this.GetLogger() ) );

    [Fact]
    public void ProcessWithCiAgentParentIsUnattended()

        // "java" is the parent of TeamCity, Bamboo, Jenkins, etc.
        => Assert.True(
            ProcessUtilities.IsProcessUnattended( ProcessKind.Other, isNonInteractiveSession: false, new[] { "java" }, this.GetLogger() ) );

    [Fact]
    public void RiderProcessIsAttendedDespiteCiAgentParent()

        // Rider can run with "java" as a parent process but must still be treated as attended.
        => Assert.False(
            ProcessUtilities.IsProcessUnattended( ProcessKind.Rider, isNonInteractiveSession: false, new[] { "java", "rider" }, this.GetLogger() ) );

    [Fact]
    public void InteractiveDesktopProcessIsAttended()
        => Assert.False(
            ProcessUtilities.IsProcessUnattended( ProcessKind.Other, isNonInteractiveSession: false, Array.Empty<string>(), this.GetLogger() ) );
}