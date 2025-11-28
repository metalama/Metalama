// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Code;
using System;
using System.Collections.Immutable;

namespace Metalama.Framework.Workspaces
{
    /// <summary>
    /// Represents a set of projects. An <see cref="IProjectSet"/> can include several instances of the <see cref="Project"/>
    /// for the same file if they target multiple frameworks, one <see cref="Project"/> instance per framework. You
    /// can create a subset with the <see cref="GetSubset"/> method.
    /// </summary>
    /// <remarks>
    /// This interface is implemented by both <see cref="Workspace"/> and <see cref="Project"/>. Use the <see cref="GetSubset"/>
    /// method to filter projects, or use <see cref="Workspace.ApplyFilter"/> to apply filters that affect introspection queries.
    /// </remarks>
    /// <seealso cref="Workspace"/>
    /// <seealso cref="Project"/>
    /// <seealso cref="ICompilationSetResult"/>
    /// <seealso href="@introspection-api"/>
    [PublicAPI]
    public interface IProjectSet : ICompilationSetResult
    {
        /// <summary>
        /// Gets the projects in the current project set.
        /// </summary>
        ImmutableArray<Project> Projects { get; }

        /// <summary>
        /// Gets the source code compilations for all projects in this set, before Metalama transformations are applied.
        /// </summary>
        /// <seealso cref="ICompilationSetResult.TransformedCode"/>
        ICompilationSet SourceCode { get; }

        /// <summary>
        /// Returns a subset of the current project set based on a filter predicate.
        /// </summary>
        /// <param name="filter">A predicate that determines whether a project should be included in the subset.</param>
        /// <returns>A new <see cref="IProjectSet"/> containing only the projects that match the filter.</returns>
        IProjectSet GetSubset( Predicate<Project> filter );

        /// <summary>
        /// Gets a declaration in the current project set by its serialized identifier.
        /// </summary>
        /// <param name="projectName">The name of the project.</param>
        /// <param name="targetFramework">The target framework, or an empty string.</param>
        /// <param name="declarationId">The serialized identifier of the declaration, obtained with <see cref="IRef.ToSerializableId"/>.</param>
        /// <param name="metalamaOutput">Indicates whether to look for the declaration in Metalama's transformed output (<c>true</c>) or in the source code (<c>false</c>).</param>
        /// <returns>The declaration, or <c>null</c> if not found.</returns>
        IDeclaration? GetDeclaration( string projectName, string targetFramework, string declarationId, bool metalamaOutput );
    }
}