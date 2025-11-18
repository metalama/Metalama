// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities;
using Newtonsoft.Json;
using System;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Represents an assembly reference including its target framework and Roslyn version.
/// </summary>
[JsonObject]
public sealed record TargetedAssemblyReference( string Name, string? Path, string? TargetFramework, Version? TargetRoslynVersion )
{
    public bool IsCorrectRoslynVersion => this.TargetRoslynVersion == null || this.TargetRoslynVersion.Equals( RoslynApiVersion.Current.ToVersion() );

    public static TargetedAssemblyReference FromPath( string path ) => new TargetedAssemblyReference( System.IO.Path.GetFileNameWithoutExtension( path ), path, null, null );
    
    public static TargetedAssemblyReference FromName( string name ) => new TargetedAssemblyReference( name, null, null, null );

    public static TargetedAssemblyReference ParsePipeSeparatedString( string s ) => ParsePipeSeparatedString( s, null );

    public static TargetedAssemblyReference ParsePipeSeparatedString( string s, Func<string, string>? resolvePath )
    {
        var parts = s.Split( '|' );

        var path = parts[0];

        if ( resolvePath != null )
        {
            path = resolvePath( path );
        }

        string? targetFramework = null;

        if ( parts.Length >= 2 && !string.IsNullOrEmpty( parts[1] ) )
        {
            targetFramework = parts[1];
        }

        Version? roslynVersion = null;

        if ( parts.Length >= 3 && !string.IsNullOrEmpty( parts[2] ) )
        {
            roslynVersion = Version.Parse( parts[2] );
        }

        return new TargetedAssemblyReference( System.IO.Path.GetFileNameWithoutExtension( path ), path, targetFramework, roslynVersion );
    }
}