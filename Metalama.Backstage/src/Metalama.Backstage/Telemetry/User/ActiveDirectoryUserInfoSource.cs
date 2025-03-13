// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Diagnostics.CodeAnalysis;
using System.DirectoryServices;
using System.Runtime.InteropServices;

namespace Metalama.Backstage.Telemetry.User;

internal sealed class ActiveDirectoryUserInfoSource : UserInfoSource
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