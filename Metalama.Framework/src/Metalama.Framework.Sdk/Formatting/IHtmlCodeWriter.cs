// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.Formatting;

/// <summary>
/// Provides HTML code writing functionality. Implemented by the optional
/// Metalama.Extensions.HtmlWriter package.
/// </summary>
public interface IHtmlCodeWriter : IProjectService
{
    /// <summary>
    /// Writes a document to HTML format.
    /// </summary>
    /// <param name="document">The document to write.</param>
    /// <param name="textWriter">The text writer to write to.</param>
    /// <param name="options">HTML writing options.</param>
    /// <param name="diagnostics">Optional diagnostics to include in the output.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(
        Document document,
        TextWriter textWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? diagnostics = null,
        CancellationToken cancellationToken = default );

    /// <summary>
    /// Writes a diff between two documents to HTML format.
    /// </summary>
    /// <param name="inputDocument">The input (source) document.</param>
    /// <param name="outputDocument">The output (transformed) document.</param>
    /// <param name="inputTextWriter">The text writer for the input document.</param>
    /// <param name="outputTextWriter">The text writer for the output document.</param>
    /// <param name="options">HTML writing options.</param>
    /// <param name="inputDiagnostics">Diagnostics for the input document.</param>
    /// <param name="outputDiagnostics">Diagnostics for the output document.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteDiffAsync(
        Document inputDocument,
        Document outputDocument,
        TextWriter inputTextWriter,
        TextWriter outputTextWriter,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? inputDiagnostics,
        IEnumerable<Diagnostic>? outputDiagnostics,
        CancellationToken cancellationToken );

    /// <summary>
    /// Writes all diffs between input and output compilations to HTML format.
    /// </summary>
    /// <param name="inputCompilation">The input (source) compilation.</param>
    /// <param name="outputCompilation">The output (transformed) compilation.</param>
    /// <param name="options">HTML writing options. Must include <see cref="HtmlCodeWriterOptions.ProjectPath"/> and <see cref="HtmlCodeWriterOptions.TargetFramework"/>.</param>
    /// <param name="additionalDiagnostics">Additional diagnostics to include in the output.</param>
    /// <param name="isSuppressed">A predicate that determines whether a diagnostic is suppressed. If <c>null</c>, no diagnostics are suppressed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAllDiffAsync(
        IPartialCompilation inputCompilation,
        IPartialCompilation outputCompilation,
        HtmlCodeWriterOptions options,
        IEnumerable<Diagnostic>? additionalDiagnostics = null,
        Func<Diagnostic, Compilation, bool>? isSuppressed = null,
        CancellationToken cancellationToken = default );
}
