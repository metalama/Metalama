// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Builds the durable references of a project, and decides what "durable" means for it.
/// </summary>
/// <remarks>
/// <para>
/// A durable reference exists so that an object outliving a single request does not keep a compilation in memory. That
/// requirement belongs to design time, where the analysis process is long-lived and Roslyn produces a new compilation
/// on every keystroke. A batch compilation has one compilation that outlives every object the run produces, so
/// collapsing a reference to an identifier and resolving it again through the symbol table buys nothing there, and
/// neither half of the round trip is free.
/// </para>
/// <para>
/// The call sites therefore keep asking for a durable reference and stop deciding what one is. See issue #1811.
/// </para>
/// </remarks>
internal interface IDurableRefFactory : IProjectService
{
    /// <summary>
    /// Returns the durable reference that <see cref="IRef.ToDurable"/> returns for a given reference.
    /// </summary>
    IDurableRef<T> FromFullRef<T>( IFullRef<T> fullRef )
        where T : class, ICompilationElement;

    /// <summary>
    /// Returns a durable reference to a given declaration or type, without creating the compilation-bound reference
    /// that <see cref="IDeclaration.ToRef"/> would return.
    /// </summary>
    IDurableRef<T> FromDeclarationOrType<T>( ICompilationElement declarationOrType )
        where T : class, ICompilationElement;

    /// <summary>
    /// Gets a value indicating whether an identifier-based durable reference may remember the reference it last
    /// resolved to.
    /// </summary>
    /// <remarks>
    /// The cache is consulted on every resolution rather than fixed when the reference is built, so that this setting
    /// applies to every durable reference of the project, including the ones read from a transitive manifest.
    /// </remarks>
    bool IsResolutionCacheEnabled { get; }
}
