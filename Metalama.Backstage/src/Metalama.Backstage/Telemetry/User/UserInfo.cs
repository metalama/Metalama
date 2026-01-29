// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Telemetry.User;

[ConfigurationFile( "userInfo.json" )]
[Description( "User information." )]
public sealed record UserInfo : ConfigurationFile
{
    public UserInfo() { }

    public UserInfo( string emailAddress )
    {
        this.EmailAddress = emailAddress;
    }

    [JsonPropertyName( "emailAddress" )]
    public string? EmailAddress { get; init; }
}
