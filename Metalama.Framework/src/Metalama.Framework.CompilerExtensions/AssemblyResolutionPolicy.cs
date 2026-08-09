// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Linq;
using System.Reflection;

namespace Metalama.Framework.CompilerExtensions;

/// <summary>
/// Implements the rules that decide which assembly may satisfy an assembly resolution request handled by the
/// resource extractor.
/// </summary>
/// <remarks>
/// This type contains no state and performs no input or output, so that the resolution rules can be verified by unit
/// tests. It is compiled both into <c>Metalama.Framework.CompilerExtensions</c> and into the unit test project.
/// </remarks>
internal static class AssemblyResolutionPolicy
{
    private static readonly string[] _assembliesShippedWithMetalamaCompiler = ["Metalama.Backstage", "Metalama.Compiler.Interfaces"];

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
    /// Determines whether a request for the assembly named <paramref name="assemblyName"/> may be satisfied by an
    /// assembly of a higher version that is already loaded in the process, instead of by the exact requested version.
    /// </summary>
    /// <param name="assemblyName">The simple name of the requested assembly.</param>
    /// <param name="isEmbeddedInCurrentBuild">
    /// A value indicating whether an assembly of the same simple name is embedded in the current build of Metalama.
    /// </param>
    public static bool AcceptsHigherVersionOfAlreadyLoadedAssembly( string assemblyName, bool isEmbeddedInCurrentBuild )
        => !isEmbeddedInCurrentBuild || _assembliesShippedWithMetalamaCompiler.Contains( assemblyName );
}
