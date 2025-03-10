// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry.User;

internal sealed class ConfigurationUserInfoSource : UserInfoSource
{
    private readonly IConfigurationManager _configurationManager;

    public ConfigurationUserInfoSource( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
    }

    public override bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo )
    {
        userInfo = this._configurationManager.Get<UserInfo>();

        return userInfo.EmailAddress != null;
    }

    public void SaveEmailAddress( string emailAddress ) => this._configurationManager.Update<UserInfo>( i => i with { EmailAddress = emailAddress } );
}