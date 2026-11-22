// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Metalama.Framework.Engine.Options;

/// <summary>
/// Represents an assembly reference including its target framework and Roslyn version.
/// </summary>
[JsonObject]
public sealed record TargetedAssemblyReference( string Name, string? Path, string? TargetFramework, Version? TargetRoslynVersion )
{
    /// <summary>
    /// Gets the target framework of the current process, which <see cref="SatisfiesCurrentProcess"/> compares with <see cref="TargetFramework"/>.
    /// </summary>
    internal static string CurrentTargetFramework { get; } =
        RuntimeInformation.FrameworkDescription.StartsWith( ".NET Framework", StringComparison.Ordinal ) ? "net472" : "net10.0";

    /// <summary>
    /// Gets a value indicating whether the current process can load the assembly. The value is <c>true</c> when <see cref="TargetFramework"/>
    /// is <c>null</c> or equals the target framework of the current process, and when <see cref="TargetRoslynVersion"/> is <c>null</c>
    /// or lower than or equal to the Roslyn version of the current process.
    /// </summary>
    /// <remarks>
    /// Several references of the same extension can satisfy the current process. <see cref="SelectForCurrentProcess"/> selects the reference
    /// that targets the highest Roslyn version among them.
    /// </remarks>
    public bool SatisfiesCurrentProcess
        => (this.TargetRoslynVersion == null || this.TargetRoslynVersion <= RoslynApiVersion.Current.ToVersion())
           && (this.TargetFramework == null || this.TargetFramework == CurrentTargetFramework);

    /// <summary>
    /// Gets the name of the extension that the assembly belongs to.
    /// </summary>
    /// <remarks>
    /// An extension can build one assembly per Roslyn version and give each assembly a different name that ends with the Roslyn version,
    /// for instance <c>Metalama.Extensions.Validation.Engine.5.0.0</c> and <c>Metalama.Extensions.Validation.Engine.5.12.0</c>.
    /// When <see cref="Name"/> ends with a dot followed by <see cref="TargetRoslynVersion"/>, this method returns the name without this suffix.
    /// Otherwise, it returns <see cref="Name"/>.
    /// </remarks>
    internal string GetExtensionName()
    {
        if ( this.TargetRoslynVersion != null )
        {
            var suffix = "." + this.TargetRoslynVersion;

            if ( this.Name.Length > suffix.Length && this.Name.EndsWith( suffix, StringComparison.OrdinalIgnoreCase ) )
            {
                return this.Name.Substring( 0, this.Name.Length - suffix.Length );
            }
        }

        return this.Name;
    }

    /// <summary>
    /// Selects the references that the current process must load.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A reference whose <see cref="TargetRoslynVersion"/> is <c>null</c> is selected when it satisfies the current process
    /// (see <see cref="SatisfiesCurrentProcess"/>).
    /// </para>
    /// <para>
    /// The references that declare a <see cref="TargetRoslynVersion"/> are grouped by the name that <see cref="GetExtensionName"/> returns.
    /// In each group, the method selects the references that satisfy the current process and that target the highest Roslyn version
    /// among them. An extension built for an older Roslyn version is therefore loaded when the extension has no assembly for the Roslyn
    /// version of the current process.
    /// </para>
    /// <para>The method preserves the order of the references.</para>
    /// </remarks>
    public static IEnumerable<TargetedAssemblyReference> SelectForCurrentProcess( IEnumerable<TargetedAssemblyReference> references )
    {
        var referenceList = references.ToReadOnlyList();

        var selectedRoslynVersions = referenceList
            .Where( r => r.TargetRoslynVersion != null && r.SatisfiesCurrentProcess )
            .GroupBy( r => r.GetExtensionName(), StringComparer.OrdinalIgnoreCase )
            .ToDictionary( g => g.Key, g => g.Max( r => r.TargetRoslynVersion! )!, StringComparer.OrdinalIgnoreCase );

        return referenceList.Where(
            r => r.SatisfiesCurrentProcess
                 && (r.TargetRoslynVersion == null || r.TargetRoslynVersion.Equals( selectedRoslynVersions[r.GetExtensionName()] )) );
    }

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