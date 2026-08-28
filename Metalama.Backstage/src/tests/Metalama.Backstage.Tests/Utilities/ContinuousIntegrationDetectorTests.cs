// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Utilities;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Utilities;

/// <summary>
/// Tests of <see cref="ContinuousIntegrationDetector"/>.
/// </summary>
/// <remarks>
/// The detection requires two independent facts, an environment variable and the process of an agent, so that no
/// single environment variable grants an unattended license. Each server is therefore tested three times: with both
/// facts, with the variable alone and with the process alone.
/// </remarks>
public sealed class ContinuousIntegrationDetectorTests : TestsBase
{
    public ContinuousIntegrationDetectorTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// The servers, with an environment variable and an agent process that identify each of them. The agent process is
    /// given as a parent process, which the detection method of every server accepts.
    /// </summary>
    private static readonly (string Name, string Variable, string Value, string ProcessName)[] _servers =
    [
        ("GitHub Actions", "GITHUB_ACTIONS", "true", "runner.worker"),
        ("Azure Pipelines", "TF_BUILD", "True", "agent.worker"),
        ("TeamCity", "TEAMCITY_VERSION", "2025.03.3", "java"),
        ("Jenkins", "JENKINS_URL", "https://jenkins.example.com/", "java"),
        ("Atlassian Bamboo", "bamboo_buildKey", "PROJ-PLAN-JOB1", "bamboo"),
        ("GoCD", "GO_PIPELINE_NAME", "my-pipeline", "java"),
        ("GitLab CI/CD", "GITLAB_CI", "true", "gitlab-runner"),
        ("CircleCI", "CIRCLECI", "true", "circleci-agent"),
        ("Buildkite", "BUILDKITE", "true", "buildkite-agent"),
        ("Travis CI", "TRAVIS", "true", "sshd: travis [priv]"),
        ("Semaphore", "SEMAPHORE", "true", "agent")
    ];

    public static TheoryData<string, string, string, string> Servers { get; } = CreateServersData();

    public static TheoryData<string, string> ServerVariables { get; } = CreateServerVariablesData();

    public static TheoryData<string> ServerProcesses { get; } = CreateServerProcessesData();

    private static TheoryData<string, string, string, string> CreateServersData()
    {
        var data = new TheoryData<string, string, string, string>();

        foreach ( var server in _servers )
        {
            data.Add( server.Name, server.Variable, server.Value, server.ProcessName );
        }

        return data;
    }

    private static TheoryData<string, string> CreateServerVariablesData()
    {
        var data = new TheoryData<string, string>();

        foreach ( var server in _servers )
        {
            data.Add( server.Variable, server.Value );
        }

        return data;
    }

    private static TheoryData<string> CreateServerProcessesData()
    {
        var data = new TheoryData<string>();

        foreach ( var processName in _servers.Select( s => s.ProcessName ).Distinct( StringComparer.OrdinalIgnoreCase ) )
        {
            data.Add( processName );
        }

        return data;
    }

    [Theory]
    [MemberData( nameof(Servers) )]
    public void ServerIsDetectedFromVariableAndParentProcess( string serverName, string variable, string value, string processName )
        => Assert.Equal( serverName, this.Detect( [(variable, value)], parentProcesses: [processName] ) );

    /// <summary>
    /// Verifies that an environment variable alone does not identify a server. A user sets a variable at no cost, so a
    /// variable that was sufficient on its own would be a way to obtain an unattended license.
    /// </summary>
    [Theory]
    [MemberData( nameof(ServerVariables) )]
    public void ServerIsNotDetectedFromVariableAlone( string variable, string value )
        => Assert.Null( this.Detect( [(variable, value)] ) );

    /// <summary>
    /// Verifies that an agent process alone does not identify a server. A self-hosted agent installed on a development
    /// machine runs outside of any job, and an interactive build on that machine is attended.
    /// </summary>
    [Theory]
    [MemberData( nameof(ServerProcesses) )]
    public void ServerIsNotDetectedFromProcessAlone( string processName )
        => Assert.Null( this.Detect( parentProcesses: [processName], runningProcesses: [processName] ) );

    /// <summary>
    /// Verifies that the agent of GitHub Actions is found among the processes of the machine when it is not a parent
    /// of the current process. MSBuild reuses its worker nodes across invocations, and a reused node is reparented to
    /// the init process when the invocation that started it ends. See issue #1859.
    /// </summary>
    [Fact]
    public void GitHubActionsIsDetectedFromRunnerRunningOnTheMachine()
        => Assert.Equal(
            "GitHub Actions",
            this.Detect( [("GITHUB_ACTIONS", "true")], parentProcesses: ["dotnet", "init"], runningProcesses: ["Runner.Listener"] ) );

    /// <summary>
    /// Verifies that the generic <c>CI</c> variable, which most servers set, does not identify a server on its own. It
    /// is the variable that a user is the most likely to know and the least costly to set.
    /// </summary>
    [Fact]
    public void ContinuousIntegrationVariableAloneIsNotSufficient()
        => Assert.Null( this.Detect( [("CI", "true")], parentProcesses: ["runner.worker"], runningProcesses: ["Runner.Listener"] ) );

    /// <summary>
    /// Verifies that a negative value of an environment variable counts as an absent variable, because a tool that
    /// wants to deny the condition sets the variable to a negative value instead of removing it.
    /// </summary>
    [Theory]
    [InlineData( "" )]
    [InlineData( " " )]
    [InlineData( "false" )]
    [InlineData( "False" )]
    [InlineData( "0" )]
    public void ServerIsNotDetectedFromNegativeVariable( string value )
        => Assert.Null( this.Detect( [("GITHUB_ACTIONS", value)], parentProcesses: ["runner.worker"] ) );

    /// <summary>
    /// Verifies that the processes of the machine are not enumerated when no environment variable matches, and that
    /// the enumeration is performed at most once otherwise.
    /// </summary>
    [Fact]
    public void ProcessesOfTheMachineAreEnumeratedLazilyAndOnce()
    {
        var enumerationCount = 0;

        string[] GetRunningProcesses()
        {
            enumerationCount++;

            return [];
        }

        Assert.Null( this.Detect( getRunningProcesses: GetRunningProcesses ) );
        Assert.Equal( 0, enumerationCount );

        Assert.Null( this.Detect( [("GITHUB_ACTIONS", "true"), ("TF_BUILD", "true")], getRunningProcesses: GetRunningProcesses ) );
        Assert.Equal( 1, enumerationCount );
    }

    /// <summary>
    /// Verifies that the parent processes are not enumerated when no environment variable matches, and that the
    /// enumeration is performed at most once otherwise.
    /// </summary>
    [Fact]
    public void ParentProcessesAreEnumeratedLazilyAndOnce()
    {
        var enumerationCount = 0;

        string[] GetParentProcesses()
        {
            enumerationCount++;

            return [];
        }

        Assert.Null( this.Detect( getParentProcesses: GetParentProcesses ) );
        Assert.Equal( 0, enumerationCount );

        Assert.Null( this.Detect( [("GITHUB_ACTIONS", "true"), ("TF_BUILD", "true")], getParentProcesses: GetParentProcesses ) );
        Assert.Equal( 1, enumerationCount );
    }

    /// <summary>
    /// Verifies that a failure to enumerate the processes of the machine does not identify a server. Reporting a
    /// server on that ground would make the failure of the enumeration a way to obtain an unattended license.
    /// </summary>
    [Fact]
    public void ServerIsNotDetectedWhenTheProcessesOfTheMachineCannotBeEnumerated()
        => Assert.Null(
            this.Detect( [("GITHUB_ACTIONS", "true")], getRunningProcesses: () => throw new InvalidOperationException( "Test." ) ) );

    /// <summary>
    /// Detects the continuous integration server from the given facts.
    /// </summary>
    /// <param name="variables">The environment variables that are set.</param>
    /// <param name="parentProcesses">The names of the parent processes of the current process.</param>
    /// <param name="runningProcesses">The names of the processes running on the machine.</param>
    /// <param name="getRunningProcesses">Supplies the names of the processes running on the machine, for a test that
    /// counts the enumerations or makes the enumeration fail.</param>
    /// <param name="getParentProcesses">Supplies the names of the parent processes, for a test that counts the
    /// enumerations.</param>
    private string? Detect(
        (string Name, string Value)[]? variables = null,
        string[]? parentProcesses = null,
        string[]? runningProcesses = null,
        Func<string[]>? getRunningProcesses = null,
        Func<string[]>? getParentProcesses = null )
    {
        var environmentVariableProvider = new TestEnvironmentVariableProvider();

        foreach ( var variable in variables ?? [] )
        {
            environmentVariableProvider.Environment.Add( variable.Name, variable.Value );
        }

        var context = new ContinuousIntegrationContext(
            environmentVariableProvider,
            getParentProcesses ?? (() => parentProcesses ?? []),
            this.ServiceProvider.GetLoggerFactory().GetLogger( nameof(ContinuousIntegrationDetectorTests) ),
            getRunningProcesses ?? (() => runningProcesses ?? []) );

        return ContinuousIntegrationDetector.GetServerName( context );
    }
}
