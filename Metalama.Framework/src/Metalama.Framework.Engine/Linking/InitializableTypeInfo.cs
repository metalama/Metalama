// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Linking;

/// <summary>
/// Caches per-type information about the <c>Initialize</c> method on types implementing
/// <c>IInitializable</c>, including per-constructor resolution of <c>InitializationContext</c> parameters.
/// </summary>
internal sealed class InitializableTypeInfo
{
    /// <summary>
    /// Gets the type that implements <c>IInitializable</c>.
    /// </summary>
    public INamedTypeSymbol Type { get; }

    /// <summary>
    /// Gets the <c>Initialize</c> method (the most-derived one in the hierarchy).
    /// </summary>
    public IMethodSymbol InitializeMethod { get; }

    /// <summary>
    /// Per-constructor: the name of the <c>InitializationContext</c> parameter to supply
    /// as a named argument, or <c>null</c> if no context argument should be appended.
    /// </summary>
    private readonly IReadOnlyDictionary<IMethodSymbol, string?> _constructorContextParamName;

    public InitializableTypeInfo(
        INamedTypeSymbol type,
        IMethodSymbol initializeMethod,
        IReadOnlyDictionary<IMethodSymbol, string?> constructorContextParamName )
    {
        this.Type = type;
        this.InitializeMethod = initializeMethod;
        this._constructorContextParamName = constructorContextParamName;
    }

    /// <summary>
    /// Returns the name of the <c>InitializationContext</c> parameter to supply as a named
    /// argument for the given constructor, or <c>null</c> if no context argument should be appended.
    /// </summary>
    public string? GetContextParamName( IMethodSymbol constructor ) => this._constructorContextParamName.TryGetValue( constructor, out var name ) ? name : null;
}