// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Backstage.Telemetry.User;

internal class ConfigurationUserInfoSource : UserInfoSource
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