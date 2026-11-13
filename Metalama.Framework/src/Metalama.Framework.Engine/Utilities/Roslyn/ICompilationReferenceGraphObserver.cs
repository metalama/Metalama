// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.Utilities.Roslyn;

/// <summary>
/// An interface that can be injected into the service provider to get callbacks from the <see cref="CompilationReferenceGraph"/>
/// class. For testing only.
/// </summary>
internal interface ICompilationReferenceGraphObserver : IProjectService
{
    /// <summary>
    /// Method invoked each time the traversal reaches an assembly other than a system assembly, including when the visit stops without enumerating the references of the assembly.
    /// </summary>
    void OnAssemblyVisited( IAssemblySymbol assembly, int depth );

    /// <summary>
    /// Method invoked each time the traversal enumerates the references of an assembly.
    /// </summary>
    void OnAssemblyExpanded( IAssemblySymbol assembly, int depth );
}
