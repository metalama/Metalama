// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.CodeModel.References;

/// <summary>
/// Creates the durable references of a project, and determines their representation.
/// </summary>
/// <remarks>
/// <para>
/// A durable reference allows an object that outlives a single request to hold a reference to a declaration without
/// keeping a compilation in memory. This requirement applies to design time, where the analysis process is long-lived
/// and Roslyn creates a new compilation after each modification of the source code.
/// </para>
/// <para>
/// A batch compilation processes a single compilation, which lives until the build ends. Converting a reference to an
/// identifier and resolving that identifier through the symbol table does not reduce memory consumption in that
/// scenario, and both operations have a cost.
/// </para>
/// <para>
/// This interface allows the call sites to request a durable reference without determining its representation. See
/// issue #1811.
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
    /// Gets a value indicating whether an identifier-based durable reference may cache the reference returned by its
    /// last resolution.
    /// </summary>
    /// <remarks>
    /// This property is read during each resolution, and not when the reference is created, so that the setting
    /// applies to all durable references of the project, including those deserialized from a transitive manifest.
    /// </remarks>
    bool IsResolutionCacheEnabled { get; }
}
