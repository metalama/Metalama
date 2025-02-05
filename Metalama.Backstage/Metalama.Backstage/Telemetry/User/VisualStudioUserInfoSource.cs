// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Win32;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Metalama.Backstage.Telemetry.User;

internal class VisualStudioUserInfoSource : UserInfoSource
{
    public override bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo )
    {
        userInfo = null;
        
        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }
        
        const string connectedUserSubKeyPath = @"Software\Microsoft\VSCommon\ConnectedUser";
        const string emailAddressKeyName = "EmailAddress";

        var connectedUserSubKey = Registry.CurrentUser.OpenSubKey( connectedUserSubKeyPath );

        var subKeyNames = connectedUserSubKey?.GetSubKeyNames();

        if ( subKeyNames == null || subKeyNames.Length == 0 )
        {
            return false;
        }

        var subKeysOrder = new int[subKeyNames.Length];

        for ( var i = 0; i < subKeyNames.Length; i++ )
        {
            var match = Regex.Match( subKeyNames[i], @"^IdeUser(?:V(?<version>\d+))?$" );

            if ( !match.Success )
            {
                subKeysOrder[i] = -1;

                continue;
            }

            var versionString = match.Groups["version"].Value;

            if ( string.IsNullOrEmpty( versionString ) )
            {
                subKeysOrder[i] = 0;
            }
            else if ( !int.TryParse( versionString, out subKeysOrder[i] ) )
            {
                subKeysOrder[i] = -1;
            }
        }

        Array.Sort( subKeysOrder, subKeyNames );

        for ( var i = subKeyNames.Length - 1; i >= 0; i-- )
        {
            var cacheSubKeyName = $@"{subKeyNames[i]}\Cache";
            var cacheKey = connectedUserSubKey?.OpenSubKey( cacheSubKeyName );
            var emailAddress = cacheKey?.GetValue( emailAddressKeyName ) as string;

            if ( !string.IsNullOrWhiteSpace( emailAddress ) )
            {
                // ReSharper disable once RedundantSuppressNullableWarningExpression
                userInfo = new( emailAddress! );

                return true;
            }
        }

        return false;
    }
}