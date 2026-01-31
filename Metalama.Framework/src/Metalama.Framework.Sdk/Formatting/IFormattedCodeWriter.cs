// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Formatting;

/// <summary>
/// Provides functionality to get classified text spans from a document.
/// </summary>
public interface IFormattedCodeWriter : IProjectService
{
    /// <summary>
    /// Gets classified text spans for a document.
    /// </summary>
    /// <param name="document">The document to classify.</param>
    /// <param name="areNodesAnnotated">Whether the syntax nodes are already annotated.</param>
    /// <param name="diagnostics">Optional diagnostics to include.</param>
    /// <param name="addTitles">Whether to add title/tooltip information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An enumerable of classified text spans.</returns>
    Task<IEnumerable<IClassifiedTextSpan>> GetClassifiedTextSpansAsync(
        Document document,
        bool areNodesAnnotated = false,
        IEnumerable<Diagnostic>? diagnostics = null,
        bool addTitles = false,
        CancellationToken cancellationToken = default );
}
