// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Utilities;

public record ToolInvocationOptions(
    ImmutableDictionary<string, string?>? EnvironmentVariables = null,
    bool Silent = false,
    ImmutableArray<string> BlockedEnvironmentVariables = default,
    ToolInvocationRetry? Retry = null )
{
    public static ToolInvocationOptions Default { get; } = new();

    // Some environment variables are set by the Microsoft.Build package and must not be passed to the child process.
    public ImmutableArray<string> BlockedEnvironmentVariables { get; init; } =
        BlockedEnvironmentVariables.IsDefault ? ImmutableArray.Create( "DOTNET_ROOT_X64", "MSBUILD_EXE_PATH", "MSBuildSDKsPath" ) : BlockedEnvironmentVariables;

    public ImmutableArray<Regex> ErrorPatterns { get; init; } = ImmutableArray.Create( new Regex( @"\: error\b" ) );

    public ImmutableArray<Regex> WarningPatterns { get; init; } = ImmutableArray.Create( new Regex( @"\: warning\b" ) );

    public ImmutableArray<Regex> SuccessPatterns { get; init; } = ImmutableArray.Create( new Regex( "Passed! " ) );

    public ImmutableArray<Regex> ImportantMessagePatterns { get; init; } = ImmutableArray.Create( new Regex( "Test run for " ) );

    public ImmutableArray<Regex> SilentPatterns { get; init; } = ImmutableArray<Regex>.Empty;

    public ImmutableArray<ReplacePattern> ReplacePatterns { get; init; } = ImmutableArray<ReplacePattern>.Empty;
}