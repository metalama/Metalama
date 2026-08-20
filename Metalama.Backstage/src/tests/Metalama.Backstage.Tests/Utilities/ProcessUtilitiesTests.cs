// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Infrastructure;
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

    /// <summary>
    /// Verifies that a continuous integration server is detected from its environment variables. The chain of parent
    /// processes does not identify it reliably, because MSBuild reuses its worker nodes across invocations and a
    /// reused node is reparented to the init process. See issue #1859.
    /// </summary>
    [Theory]
    [InlineData( "CI", "true" )]              // GitHub Actions
    [InlineData( "TF_BUILD", "True" )]        // Azure Pipelines
    [InlineData( "TEAMCITY_VERSION", "2025.03.3" )]
    [InlineData( "JENKINS_URL", "https://jenkins.example.com/" )]
    [InlineData( "bamboo_buildKey", "PROJ-PLAN-JOB1" )]
    [InlineData( "GO_PIPELINE_NAME", "my-pipeline" )]
    public void ContinuousIntegrationIsDetectedFromEnvironmentVariable( string variable, string value )
        => Assert.True( ProcessUtilities.HasContinuousIntegrationEnvironmentVariable( GetEnvironment( (variable, value) ) ) );

    [Theory]
    [InlineData( "" )]
    [InlineData( " " )]
    [InlineData( "false" )]
    [InlineData( "False" )]
    [InlineData( "0" )]
    public void ContinuousIntegrationIsNotDetectedFromNegativeEnvironmentVariable( string value )
        => Assert.False( ProcessUtilities.HasContinuousIntegrationEnvironmentVariable( GetEnvironment( ("CI", value) ) ) );

    [Fact]
    public void ContinuousIntegrationIsNotDetectedWithoutEnvironmentVariable()
        => Assert.False( ProcessUtilities.HasContinuousIntegrationEnvironmentVariable( GetEnvironment() ) );

    /// <summary>
    /// Creates an <see cref="IEnvironmentVariableProvider"/> that exposes the given variables and nothing else.
    /// </summary>
    private static IEnvironmentVariableProvider GetEnvironment( params (string Name, string Value)[] variables )
    {
        var provider = new TestEnvironmentVariableProvider();

        foreach ( var variable in variables )
        {
            provider.Environment.Add( variable.Name, variable.Value );
        }

        return provider;
    }
}