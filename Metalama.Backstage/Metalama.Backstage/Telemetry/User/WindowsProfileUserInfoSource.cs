// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Metalama.Backstage.Telemetry.User;

internal class WindowsProfileUserInfoSource : UserInfoSource
{
    public override bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo )
    {
        userInfo = null;

        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }
        
        var trimChars = new[] { ' ', '\0', '\t' };

        var profile =
            Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles",
                "DefaultProfile",
                null ) as string;

        if ( string.IsNullOrEmpty( profile ) )
        {
            return false;
        }

        var key =
            Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows NT\CurrentVersion\Windows Messaging Subsystem\Profiles\" + profile );

        if ( key == null )
        {
            return false;
        }

        foreach ( var subKeyName in key.GetSubKeyNames() )
        {
            var subKey = key.OpenSubKey( subKeyName );

            var emailBytes = (byte[]?) subKey?.GetValue( "001f6607" );

            if ( emailBytes != null )
            {
                var emailAddress = Encoding.Unicode.GetString( emailBytes ).Trim( trimChars );

                userInfo = new( emailAddress );

                return true;
            }
        }

        return false;
    }
}