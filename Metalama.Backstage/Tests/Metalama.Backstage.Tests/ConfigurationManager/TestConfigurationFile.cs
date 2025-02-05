// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;

namespace Metalama.Backstage.Tests.ConfigurationManager;

[ConfigurationFile( "test.json" )]
internal record TestConfigurationFile : ConfigurationFile
{
    public bool IsModified { get; init; }
}