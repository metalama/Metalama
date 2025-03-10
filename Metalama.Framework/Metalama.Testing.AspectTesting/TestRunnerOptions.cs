// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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