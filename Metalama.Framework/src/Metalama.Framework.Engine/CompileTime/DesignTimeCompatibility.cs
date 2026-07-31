// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// Determines whether an assembly compiled by another version of Metalama can be consumed by the design-time (IDE)
/// support of the current version.
/// </summary>
/// <remarks>
/// <para>
/// This is a narrower question than whether the assembly can be consumed at all. The compile-time pipeline reads the
/// embedded compile-time project of an older version and applies its aspects, which is why a solution mixing versions
/// builds correctly. The design-time pipeline additionally needs to reach that version's design-time entry point, and
/// that is what breaks: #1605 rotated every GUID of <c>Metalama.Framework.DesignTime.Contracts</c> and the
/// <see cref="AppDomain"/> slot through which its entry point manager registers itself, so a version below
/// <see cref="MinimumSupportedVersion"/> belongs to a generation that the current one can never observe.
/// </para>
/// <para>
/// Lives in the engine rather than in the design-time assembly because both need it: the compile-time pipeline to
/// warn the user, and the design-time pipeline to stop asking for an entry point that can never answer.
/// </para>
/// </remarks>
public static class DesignTimeCompatibility
{
    /// <summary>
    /// Gets the lowest Metalama version whose compiled assemblies the design-time support of the current version can
    /// consume, i.e. the first version of the current generation of the design-time contracts.
    /// </summary>
    public static Version MinimumSupportedVersion { get; } = new( 2026, 1 );

    /// <summary>
    /// Determines whether an assembly compiled by a given version of Metalama can be consumed at design time.
    /// </summary>
    /// <remarks>
    /// A version that cannot be parsed is assumed to be supported, so that an unexpected format degrades into the
    /// existing behaviour rather than into a warning the user cannot act on.
    /// </remarks>
    public static bool IsSupportedAtDesignTime( string? packageVersion )
        => !TryParsePackageVersion( packageVersion, out var version ) || IsSupportedAtDesignTime( version );

    /// <summary>
    /// Determines whether an assembly compiled by a given version of Metalama can be consumed at design time.
    /// </summary>
    public static bool IsSupportedAtDesignTime( Version version ) => version >= MinimumSupportedVersion;

    /// <summary>
    /// Parses the numeric part of a package version such as <c>2026.1.21.1065-6c7dfbeb</c>.
    /// </summary>
    private static bool TryParsePackageVersion( string? packageVersion, out Version version )
    {
        version = null!;

        if ( string.IsNullOrEmpty( packageVersion ) )
        {
            return false;
        }

        var prereleaseIndex = packageVersion!.IndexOf( "-", StringComparison.Ordinal );
        var numericPart = prereleaseIndex < 0 ? packageVersion : packageVersion.Substring( 0, prereleaseIndex );

        return Version.TryParse( numericPart, out version! );
    }
}
