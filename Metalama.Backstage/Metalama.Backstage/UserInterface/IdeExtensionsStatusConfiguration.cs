// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Metalama.Backstage.UserInterface;

[ConfigurationFile( "ideExtensionsStatus.json" )]
[Description( "IDE extension status." )]
public record IdeExtensionsStatusConfiguration : ConfigurationFile
{
    [JsonProperty( "vs" )]
    public bool IsVisualStudioExtensionInstalled { get; init; }
}