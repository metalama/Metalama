// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Reflection;

namespace Metalama.DesignTime.Hosting;

/// <summary>
/// Loads analyzer assemblies the way the IDE loads them on the current runtime.
/// </summary>
/// <remarks>
/// <para>
/// On .NET, <see cref="Create"/> returns an <c>IsolatedAnalyzerAssemblyLoader</c>, which loads the assemblies of each
/// directory into their own <c>AssemblyLoadContext</c>, as Roslyn does on .NET. On .NET Framework, it returns a
/// <c>DesktopAnalyzerAssemblyLoader</c>, which loads them into the current application domain, as Visual Studio does.
/// </para>
/// <para>
/// Both loaders resolve the dependencies of an analyzer assembly from the other analyzer references whose path was passed
/// to <see cref="AddDependencyLocation"/>. Metalama requires this, because <c>Metalama.Framework.CompilerExtensions.dll</c>
/// depends on <c>Metalama.Compiler.Interface.dll</c>, which is a separate analyzer reference.
/// </para>
/// </remarks>
public abstract class HostAnalyzerAssemblyLoader : IAnalyzerAssemblyLoader
{
    private protected HostAnalyzerAssemblyLoader() { }

    /// <summary>
    /// Creates the loader that matches the current runtime.
    /// </summary>
    public static HostAnalyzerAssemblyLoader Create()
#if NET
        => new IsolatedAnalyzerAssemblyLoader();
#else
        => new DesktopAnalyzerAssemblyLoader();
#endif

    /// <summary>
    /// Gets the number of isolation contexts that the loader created, which is the number of distinct analyzer directories
    /// that it loaded assemblies from. It is always zero on .NET Framework, where assemblies are not isolated.
    /// </summary>
    public abstract int LoadContextCount { get; }

    /// <inheritdoc/>
    public abstract void AddDependencyLocation( string fullPath );

    /// <inheritdoc/>
    public abstract Assembly LoadFromPath( string fullPath );
}
