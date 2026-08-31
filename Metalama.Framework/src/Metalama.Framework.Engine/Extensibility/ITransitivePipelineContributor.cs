// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;

namespace Metalama.Framework.Engine.Extensibility;

/// <summary>
/// Represents a transitive (cross-project) pipeline contributor. Typically a reference validator.
/// </summary>
public interface ITransitivePipelineContributor : IPipelineContributor
{
    /// <summary>
    /// Gets the key of the document that this contributor belongs to, which is the one the design-time pipeline files
    /// it under, or the default key when it belongs to none.
    /// </summary>
    /// <remarks>
    /// A key rather than the <see cref="Microsoft.CodeAnalysis.SyntaxTree"/> itself. The pipeline needs nothing from
    /// the tree but the document it identifies, and a contributor is filed in a result that outlives the compilation
    /// the tree came from.
    /// </remarks>
    DocumentKey DocumentKey { get; }

    /// <summary>
    /// Returns the design-time form of this contributor, or <c>null</c> when it has none.
    /// </summary>
    /// <remarks>
    /// The returned object is stored by the design-time pipeline for far longer than the run that produced it, and it
    /// must therefore be durable. The contributor itself is under no such constraint, so this method is where a
    /// compilation-bound state is converted into a serializable identifier. See
    /// <see cref="IDesignTimePipelineResultExtension"/> for the requirement in full.
    /// </remarks>
    IDesignTimePipelineResultExtension? ToDesignTime();
}