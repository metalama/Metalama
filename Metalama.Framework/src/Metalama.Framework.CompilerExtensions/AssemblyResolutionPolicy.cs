// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;
using System.Reflection;

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Implements the rules that decide which assembly already loaded in the process may satisfy an assembly resolution
/// request handled by the resource extractor.
/// </summary>
/// <remarks>
/// This type contains no state and performs no input or output, so that the resolution rules can be verified by unit
/// tests. It is compiled both into <c>Metalama.Framework.CompilerExtensions</c> and into the unit test project.
/// </remarks>
internal static class AssemblyResolutionPolicy
{
    /// <summary>
    /// Determines whether <paramref name="candidate"/> matches <paramref name="requestedAssemblyName"/> and has exactly
    /// the requested version.
    /// </summary>
    public static bool MatchesExactVersion( AssemblyName requestedAssemblyName, AssemblyName candidate )
        => AssemblyName.ReferenceMatchesDefinition( requestedAssemblyName, candidate ) && requestedAssemblyName.Version == candidate.Version;

    /// <summary>
    /// Determines whether <paramref name="candidate"/> matches <paramref name="requestedAssemblyName"/> and has the
    /// requested version or a higher one.
    /// </summary>
    public static bool MatchesSameOrHigherVersion( AssemblyName requestedAssemblyName, AssemblyName candidate )

        // The Version operator <= throws on .NET Framework when the first operand is null, so the null case is tested explicitly.
        => AssemblyName.ReferenceMatchesDefinition( requestedAssemblyName, candidate )
           && (requestedAssemblyName.Version == null || requestedAssemblyName.Version <= candidate.Version);

    /// <summary>
    /// Selects, among the assemblies already loaded in the process, the one that must satisfy a request for
    /// <paramref name="requestedAssemblyName"/>.
    /// </summary>
    /// <param name="requestedAssemblyName">The name of the requested assembly.</param>
    /// <param name="candidates">The names of the assemblies already loaded in the process.</param>
    /// <param name="isEmbeddedInCurrentBuild">
    /// A value indicating whether an assembly of the same simple name as <paramref name="requestedAssemblyName"/> is
    /// embedded in the current build of Metalama.
    /// </param>
    /// <returns>
    /// The index in <paramref name="candidates"/> of the assembly that must satisfy the request, or <c>-1</c> if no
    /// already-loaded assembly may satisfy it.
    /// </returns>
    /// <remarks>
    /// A version higher than the requested one is accepted only for assemblies that are not embedded in the current
    /// build, typically the assemblies provided by the host process, because our own assemblies may request a lower
    /// version of Roslyn than the one that the host has loaded. An assembly that is embedded in the current build must
    /// be bound to the exact version embedded beside it: it is built together with that exact version, and none of the
    /// assemblies embedded in Metalama, in particular <c>Metalama.Backstage</c>, promises API compatibility across
    /// builds. Several builds of Metalama can be active in the same process, for instance in Visual Studio when two
    /// projects reference different versions of Metalama, and binding one build to the assembly embedded in another
    /// one throws <c>TypeLoadException</c> as soon as a type that the other build has removed is used (issue #1833).
    /// </remarks>
    public static int SelectAlreadyLoadedAssembly(
        AssemblyName requestedAssemblyName,
        IReadOnlyList<AssemblyName> candidates,
        bool isEmbeddedInCurrentBuild )
    {
        // An assembly of exactly the requested version is always preferred.
        for ( var i = 0; i < candidates.Count; i++ )
        {
            if ( MatchesExactVersion( requestedAssemblyName, candidates[i] ) )
            {
                return i;
            }
        }

        if ( isEmbeddedInCurrentBuild )
        {
            return -1;
        }

        for ( var i = 0; i < candidates.Count; i++ )
        {
            if ( MatchesSameOrHigherVersion( requestedAssemblyName, candidates[i] ) )
            {
                return i;
            }
        }

        return -1;
    }
}
