// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CompileTime;

/// <summary>
/// An optional observer interface for monitoring <see cref="SymbolClassifier"/> operations.
/// Used primarily for testing to verify that Quick mode is used correctly.
/// </summary>
[UsedImplicitly( ImplicitUseTargetFlags.Members )]
internal interface ISymbolClassifierObserver : IGlobalService
{
    /// <summary>
    /// Called when <see cref="ISymbolClassifier.GetTemplatingScope"/> or <see cref="ISymbolClassifier.GetTemplatingScopeAndRule"/> is invoked.
    /// </summary>
    /// <param name="context">The classification context used.</param>
    void OnGetTemplatingScope( SymbolClassificationContext context );

    /// <summary>
    /// Called when Quick mode causes an expensive operation to be skipped.
    /// </summary>
    /// <param name="operationName">The name of the skipped operation (e.g., "IsSymbolAvailable", "WellKnownTypeCheck").</param>
    void OnQuickModeSkip( string operationName );

    /// <summary>
    /// Called when an expensive operation is performed (not skipped by Quick mode).
    /// </summary>
    /// <param name="operationName">The name of the operation.</param>
    void OnExpensiveOperation( string operationName );

    /// <summary>
    /// Called when a cache lookup occurs in GetTemplatingScopeCore.
    /// </summary>
    /// <param name="hit">True if the result was found in cache, false if computation was needed.</param>
    void OnCacheLookup( bool hit );

    /// <summary>
    /// Called when an attribute scope cache lookup occurs.
    /// </summary>
    /// <param name="hit">True if the result was found in cache, false if computation was needed.</param>
    void OnAttributeScopeCacheLookup( bool hit );
}