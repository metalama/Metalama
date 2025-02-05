// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Telemetry.User;

internal class ActiveDirectoryUserInfoSource : UserInfoSource
{
    public override bool TryGetUserInfo( [NotNullWhen( true )] out UserInfo? userInfo )
    {
        userInfo = null;

        if ( !RuntimeInformation.IsOSPlatform( OSPlatform.Windows ) )
        {
            return false;
        }

        if ( string.IsNullOrEmpty( Environment.UserDomainName ) )
        {
            return false;
        }

        using var searcher = new DirectorySearcher();
        searcher.Filter = $"(samaccountname={Environment.UserName})";
        searcher.PropertiesToLoad.Add( "mail" );
        SearchResult? results;

        try
        {
            results = searcher.FindOne();
        }
        catch ( COMException )
        {
            results = null;
        }

        if ( results == null )
        {
            return false;
        }
        
        static string? GetValue( ResultPropertyValueCollection property )
        {
            if ( property.Count == 1 )
            {
                return property[0] as string;
            }
            else
            {
                return null;
            }
        }

        var email = GetValue( results.Properties["mail"] );

        if ( string.IsNullOrWhiteSpace( email ) )
        {
            return false;
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        userInfo = new UserInfo( email! );

        return true;
    }
}