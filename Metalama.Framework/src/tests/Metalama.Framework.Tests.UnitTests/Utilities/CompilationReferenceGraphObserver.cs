// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Tests.UnitTests.Utilities;

/// <summary>
/// An implementation of <see cref="ICompilationReferenceGraphObserver"/> that counts the visits and the enumerations of references.
/// </summary>
internal sealed class CompilationReferenceGraphObserver : ICompilationReferenceGraphObserver
{
    private readonly Dictionary<string, int> _expansionCounts = new();

    /// <summary>
    /// Gets the number of visits of assemblies other than system assemblies.
    /// </summary>
    public int VisitCount { get; private set; }

    /// <summary>
    /// Gets the number of times the references of the given assembly were enumerated.
    /// </summary>
    public int GetExpansionCount( string assemblyName ) => this._expansionCounts.TryGetValue( assemblyName, out var count ) ? count : 0;

    /// <inheritdoc/>
    public void OnAssemblyVisited( IAssemblySymbol assembly, int depth ) => this.VisitCount++;

    /// <inheritdoc/>
    public void OnAssemblyExpanded( IAssemblySymbol assembly, int depth ) => this._expansionCounts[assembly.Name] = this.GetExpansionCount( assembly.Name ) + 1;
}
