// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities;
using Newtonsoft.Json;
using System;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Represents an assembly reference including its target framework and Roslyn version.
/// </summary>
[JsonObject]
public sealed record TargetedAssemblyReference( string Name, string? Path, string? TargetFramework, Version? TargetRoslynVersion )
{
    private static readonly string _targetFramework =
        RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net8.0";

    public bool SatisfiesCurrentProcess
        => (this.TargetRoslynVersion == null || this.TargetRoslynVersion.Equals( RoslynApiVersion.Current.ToVersion() ))
           && (this.TargetFramework == null || this.TargetFramework == _targetFramework);

    public static TargetedAssemblyReference FromPath( string path ) => new( System.IO.Path.GetFileNameWithoutExtension( path ), path, null, null );

    public static TargetedAssemblyReference FromName( string name ) => new( name, null, null, null );

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
            if ( !Version.TryParse( parts[2], out roslynVersion ) )
            {
                throw new ArgumentOutOfRangeException( nameof(s), $"Cannot parse the Roslyn version number in '{s}'." );
            }
        }

        return new TargetedAssemblyReference( System.IO.Path.GetFileNameWithoutExtension( path ), path, targetFramework, roslynVersion );
    }
}