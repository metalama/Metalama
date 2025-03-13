// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;

namespace Metalama.Backstage.Tests.ConfigurationManager;

[ConfigurationFile( "test.json" )]
internal sealed record TestConfigurationFile : ConfigurationFile
{
    public bool IsModified { get; init; }
}