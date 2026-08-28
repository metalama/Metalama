// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Utilities;

/// <summary>
/// Recognizes the continuous integration server on which the current process runs.
/// </summary>
/// <remarks>
/// <para>
/// Each server has its own detection method, which combines an environment variable that is specific to that server
/// with the processes of its agent. Two independent facts are required because the detection grants an unattended
/// license. An environment variable alone would not do: a user sets a variable at no cost, so any single documented
/// variable would become a way to obtain a license. The agent process corroborates the variable.
/// </para>
/// <para>
/// The detection is a heuristic and not a security boundary. It runs on a machine that the user controls, and a user
/// who is determined to forge a continuous integration environment can still do so. The purpose of the second fact is
/// to ensure that no single environment variable is sufficient.
/// </para>
/// <para>
/// The generic <c>CI</c> variable, which most servers set, is intentionally not a criterion. It is the variable that a
/// user is the most likely to know and the least costly to set, and it identifies no agent process that could
/// corroborate it. A server whose agent process cannot be named is not detected either.
/// </para>
/// </remarks>
internal static class ContinuousIntegrationDetector
{
    /// <summary>
    /// Gets the name of the continuous integration server on which the current process runs, or <c>null</c> if the
    /// current process does not run on a continuous integration server.
    /// </summary>
    public static string? GetServerName( ContinuousIntegrationContext context )
        => _servers.FirstOrDefault( s => s.IsDetected( context ) )?.Name;

    private static readonly string[] _gitHubActionsProcesses = ["Runner.Worker", "Runner.Listener"];

    /// <summary>
    /// Determines whether the current process is a part of a GitHub Actions job.
    /// </summary>
    /// <remarks>
    /// <c>Runner.Listener</c> is the process that waits for a job, and <c>Runner.Worker</c> is the process that runs
    /// it. Both run for the whole duration of the job, on a GitHub-hosted runner and on a self-hosted runner alike, so
    /// they are found even when the current process is no longer a descendant of the runner. A self-hosted runner
    /// installed on a development machine keeps <c>Runner.Listener</c> running outside of any job, but
    /// <c>GITHUB_ACTIONS</c> is then not set, so an interactive build on that machine is not affected.
    /// </remarks>
    private static bool IsGitHubActions( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "GITHUB_ACTIONS" ) && context.IsProcessRunning( _gitHubActionsProcesses );

    private static readonly string[] _azurePipelinesProcesses = ["Agent.Worker", "Agent.Listener"];

    /// <summary>
    /// Determines whether the current process is a part of an Azure Pipelines job.
    /// </summary>
    /// <remarks>
    /// The agent of Azure Pipelines has the same architecture as the runner of GitHub Actions: a listener process that
    /// waits for a job and a worker process that runs it.
    /// </remarks>
    private static bool IsAzurePipelines( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "TF_BUILD" ) && context.IsProcessRunning( _azurePipelinesProcesses );

    private static readonly string[] _teamCityProcesses = ["java"];

    /// <summary>
    /// Determines whether the current process is a part of a TeamCity build.
    /// </summary>
    /// <remarks>
    /// The build agent of TeamCity runs on the Java virtual machine, so the only process that corroborates the
    /// environment variable is <c>java</c>. This is a weak corroboration, because a development machine also runs Java
    /// programs, but it is the strongest one available for this server.
    /// </remarks>
    private static bool IsTeamCity( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "TEAMCITY_VERSION" ) && context.IsProcessRunning( _teamCityProcesses );

    private static readonly string[] _jenkinsProcesses = ["java"];

    /// <summary>
    /// Determines whether the current process is a part of a Jenkins build.
    /// </summary>
    /// <remarks>
    /// The controller and the agent of Jenkins run on the Java virtual machine. See the remarks of
    /// <see cref="IsTeamCity"/> regarding the strength of this corroboration.
    /// </remarks>
    private static bool IsJenkins( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "JENKINS_URL" ) && context.IsProcessRunning( _jenkinsProcesses );

    private static readonly string[] _bambooProcesses = ["bamboo", "java"];

    /// <summary>
    /// Determines whether the current process is a part of an Atlassian Bamboo build.
    /// </summary>
    /// <remarks>
    /// The agent of Bamboo runs on the Java virtual machine, and its process is named <c>bamboo</c> or <c>java</c>
    /// depending on the way it is installed. See the remarks of <see cref="IsTeamCity"/> regarding the strength of this
    /// corroboration.
    /// </remarks>
    private static bool IsBamboo( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "bamboo_buildKey" ) && context.IsProcessRunning( _bambooProcesses );

    private static readonly string[] _goCdProcesses = ["java"];

    /// <summary>
    /// Determines whether the current process is a part of a GoCD build.
    /// </summary>
    /// <remarks>
    /// The agent of GoCD runs on the Java virtual machine. See the remarks of <see cref="IsTeamCity"/> regarding the
    /// strength of this corroboration.
    /// </remarks>
    private static bool IsGoCd( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "GO_PIPELINE_NAME" ) && context.IsProcessRunning( _goCdProcesses );

    private static readonly string[] _gitLabProcesses = ["gitlab-runner"];

    /// <summary>
    /// Determines whether the current process is a part of a GitLab CI/CD job.
    /// </summary>
    private static bool IsGitLab( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "GITLAB_CI" ) && context.IsProcessRunning( _gitLabProcesses );

    private static readonly string[] _circleCiProcesses = ["circleci-agent"];

    /// <summary>
    /// Determines whether the current process is a part of a CircleCI job.
    /// </summary>
    /// <remarks>
    /// A CircleCI job usually runs in a container, and the containerized environment is recognized before this method
    /// is reached on Linux.
    /// </remarks>
    private static bool IsCircleCi( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "CIRCLECI" ) && context.IsProcessRunning( _circleCiProcesses );

    private static readonly string[] _buildkiteProcesses = ["buildkite-agent"];

    /// <summary>
    /// Determines whether the current process is a part of a Buildkite job.
    /// </summary>
    private static bool IsBuildkite( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "BUILDKITE" ) && context.IsProcessRunning( _buildkiteProcesses );

    private static readonly string[] _travisProcesses = ["sshd: travis [priv]"];

    /// <summary>
    /// Determines whether the current process is a part of a Travis CI job.
    /// </summary>
    /// <remarks>
    /// The build of Travis CI runs under a secure shell session of the <c>travis</c> user. That session is a parent of
    /// the current process and is not identified among the processes of the machine, so only the parent processes are
    /// examined.
    /// </remarks>
    private static bool IsTravisCi( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "TRAVIS" ) && context.HasParentProcess( _travisProcesses );

    private static readonly string[] _semaphoreProcesses = ["agent"];

    /// <summary>
    /// Determines whether the current process is a part of a Semaphore job.
    /// </summary>
    /// <remarks>
    /// The agent of Semaphore is named <c>agent</c>. That name is too common to be searched among all the processes of
    /// the machine, so only the parent processes are examined.
    /// </remarks>
    private static bool IsSemaphore( ContinuousIntegrationContext context )
        => context.IsEnvironmentVariableSet( "SEMAPHORE" ) && context.HasParentProcess( _semaphoreProcesses );

    /// <summary>
    /// The servers that this class recognizes. The order is not significant, because a machine runs the agent of a
    /// single server in practice and the name is reported for information only.
    /// </summary>
    private static readonly ContinuousIntegrationServer[] _servers =
    [
        new( "GitHub Actions", _gitHubActionsProcesses, IsGitHubActions ),
        new( "Azure Pipelines", _azurePipelinesProcesses, IsAzurePipelines ),
        new( "TeamCity", _teamCityProcesses, IsTeamCity ),
        new( "Jenkins", _jenkinsProcesses, IsJenkins ),
        new( "Atlassian Bamboo", _bambooProcesses, IsBamboo ),
        new( "GoCD", _goCdProcesses, IsGoCd ),
        new( "GitLab CI/CD", _gitLabProcesses, IsGitLab ),
        new( "CircleCI", _circleCiProcesses, IsCircleCi ),
        new( "Buildkite", _buildkiteProcesses, IsBuildkite ),
        new( "Travis CI", _travisProcesses, IsTravisCi ),
        new( "Semaphore", _semaphoreProcesses, IsSemaphore )
    ];

    /// <summary>
    /// Gets the names of the agent processes of all the servers. The search of the parent processes stops when it
    /// reaches one of them, so that it does not walk the chain further than necessary.
    /// </summary>
    public static ISet<string> AgentProcessNames { get; }
        = new HashSet<string>( _servers.SelectMany( s => s.AgentProcessNames ), StringComparer.OrdinalIgnoreCase );

    /// <summary>
    /// A continuous integration server that <see cref="ContinuousIntegrationDetector"/> recognizes.
    /// </summary>
    /// <param name="Name">The name of the server, reported in the log.</param>
    /// <param name="AgentProcessNames">The names of the processes of the agent of the server.</param>
    /// <param name="IsDetected">Determines whether the current process runs on this server.</param>
    private sealed record ContinuousIntegrationServer(
        string Name,
        string[] AgentProcessNames,
        Func<ContinuousIntegrationContext, bool> IsDetected );
}
