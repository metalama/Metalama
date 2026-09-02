// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;

namespace Metalama.Framework.CompilerExtensions
{
    /// <summary>
    /// Maps the Roslyn version of the host to the name of the embedded payload variant that serves it. The variants
    /// are declared in <c>eng/RoslynVersions</c> and their range is bounded by <c>RoslynApiMinVersion</c> in
    /// <c>Directory.Packages.props</c>. No variant serves a host whose Roslyn version is below that bound.
    /// </summary>
    internal static class RoslynVariantPolicy
    {
        /// <summary>
        /// Gets the lowest Roslyn version served by an embedded payload variant. It is the value of
        /// <c>RoslynApiMinVersion</c> in <c>Directory.Packages.props</c>.
        /// </summary>
        public static Version MinimumSupportedRoslynVersion { get; } = new Version( 5, 0 );

        /// <summary>
        /// Gets the name of the embedded payload variant that serves a given host Roslyn version.
        /// </summary>
        /// <param name="roslynVersion">The Roslyn version of the host.</param>
        /// <param name="variantName">When this method returns <c>true</c>, the name of the variant directory.</param>
        /// <returns><c>true</c> if a variant serves <paramref name="roslynVersion"/>, otherwise <c>false</c>.</returns>
        public static bool TryGetVariantName( Version roslynVersion, out string variantName )
        {
            if ( roslynVersion >= new Version( 5, 10 ) )
            {
                variantName = "5.10.0";

                return true;
            }
            else if ( roslynVersion >= MinimumSupportedRoslynVersion )
            {
                variantName = "5.0.0";

                return true;
            }
            else
            {
                variantName = "4.12.0";

                return true;
            }
        }
    }
}
