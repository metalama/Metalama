// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    [PublicAPI]
    public interface IProjectSet : ICompilationSetResult
    {
        /// <summary>
        /// Gets the projects in the current <see cref="ProjectSet"/>.
        /// </summary>
        ImmutableArray<Project> Projects { get; }

        ICompilationSet SourceCode { get; }

        /// <summary>
        /// Returns a subset of the current subset.
        /// </summary>
        /// <param name="filter">A predicate that determines if a project must be a part of the new subset.</param>
        /// <returns></returns>
        IProjectSet GetSubset( Predicate<Project> filter );

        /// <summary>
        /// Gets a declaration in the current subset. 
        /// </summary>
        /// <param name="projectName">Path of the project.</param>
        /// <param name="targetFramework">Target framework, or an empty string.</param>
        /// <param name="declarationId">Serialized identifier of the declaration obtained  with <see cref="IRef.ToSerializableId"/>.</param>
        /// <param name="metalamaOutput"></param>
        /// <returns></returns>
        IDeclaration? GetDeclaration( string projectName, string targetFramework, string declarationId, bool metalamaOutput );
    }
}