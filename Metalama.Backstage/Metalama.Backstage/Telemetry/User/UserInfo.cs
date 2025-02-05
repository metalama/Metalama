// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Metalama.Backstage.Telemetry.User;

[ConfigurationFile( "userInfo.json" )]
[Description( "User information." )]
public record UserInfo : ConfigurationFile
{
    public UserInfo() { }
    
    public UserInfo( string emailAddress )
    {
        this.EmailAddress = emailAddress;
    }

    [JsonProperty( "emailAddress" )]
    public string? EmailAddress { get; init; }
}