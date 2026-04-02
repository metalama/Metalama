// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Options;
using Metalama.Framework.Project;
using Microsoft.CodeAnalysis.CSharp;
using System;

namespace Metalama.Framework.Engine.CodeModel
{
    public sealed partial class ProjectModel
    {
        internal sealed class ProjectFeaturesImpl : ProjectFeatures
        {
            public override bool SupportsCovariantReturnTypes { get; }

            public ProjectFeaturesImpl( IProjectOptions options )
            {
                this.SupportsCovariantReturnTypes = ComputeSupportsCovariantReturn( options );
            }

            private static bool ComputeSupportsCovariantReturn( IProjectOptions options )
            {
                if ( options.LanguageVersion < LanguageVersion.CSharp9 )
                {
                    return false;
                }

                var allTfms = options.AllTargetFrameworks ?? options.TargetFramework;

                if ( string.IsNullOrEmpty( allTfms ) )
                {
                    // Unknown target framework — assume modern.
                    return true;
                }

                foreach ( var tfm in allTfms.Split( ';' ) )
                {
                    var trimmed = tfm.Trim();

                    if ( trimmed.Length > 0 && !TargetFrameworkSupportsCovariantReturn( trimmed ) )
                    {
                        return false;
                    }
                }

                return true;
            }

            /// <summary>
            /// Determines whether a single target framework moniker (short name) supports covariant return types.
            /// Covariant returns require .NET 5.0+. Older frameworks (.NET Framework, .NET Standard, .NET Core) do not support them.
            /// </summary>
            internal static bool TargetFrameworkSupportsCovariantReturn( string tfm )
            {
                // Must start with "net" followed by a digit.
                if ( tfm.Length <= 3
                     || !tfm.StartsWith( "net", StringComparison.OrdinalIgnoreCase )
                     || !char.IsDigit( tfm[3] ) )
                {
                    // Handles: netstandard2.0, netcoreapp3.1, monoandroid, xamarin.ios, empty, etc.
                    return false;
                }

                // Old-style .NET Framework TFMs have no dot: net472, net48, net461, net35
                var dotIndex = tfm.IndexOf( ".", StringComparison.Ordinal );

                if ( dotIndex < 0 )
                {
                    return false;
                }

                // New-style: net5.0, net8.0-windows, net4.8
                // Parse the major version between "net" and the first dot.
#if NET5_0_OR_GREATER
                if ( int.TryParse( tfm.AsSpan( 3, dotIndex - 3 ), out var major ) )
#else
                if ( int.TryParse( tfm.Substring( 3, dotIndex - 3 ), out var major ) )
#endif
                {
                    return major >= 5;
                }

                return false;
            }
        }
    }
}
