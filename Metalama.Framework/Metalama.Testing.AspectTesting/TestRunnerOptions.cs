// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Configuration;

namespace Metalama.Testing.AspectTesting;

[ConfigurationFile( "testRunner.json", "testRunner" )]
[PublicAPI]
public record TestRunnerOptions : ConfigurationFile
{
    public bool LaunchDiffTool { get; init; } = true;

    public int MaxDiffToolInstances { get; init; } = 1;
}