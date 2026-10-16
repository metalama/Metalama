// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Options influencing the HTML writing behavior of the test framework.
    /// </summary>
    /// <param name="ProjectPath">The path to the project file. Required by <see cref="IHtmlCodeWriter.WriteAllDiffAsync"/>.</param>
    /// <param name="TargetFramework">The target framework. Required by <see cref="IHtmlCodeWriter.WriteAllDiffAsync"/>.</param>
    /// <param name="AddTitles">Whether to add titles to HTML elements.</param>
    /// <param name="Prolog">Optional HTML prolog content.</param>
    /// <param name="Epilogue">Optional HTML epilogue content.</param>
    public sealed record HtmlCodeWriterOptions(
        string ProjectPath,
        string TargetFramework,
        bool AddTitles = false,
        string? Prolog = null,
        string? Epilogue = null )
    {
        /// <summary>
        /// Gets a value indicating whether to add, to each identifier, the documentation comment id of the symbol it declares
        /// (<c>data-def</c> attribute) or references (<c>data-ref</c> attribute), and to each overriding member the id of the
        /// overridden member (<c>data-overrides</c> attribute). Locals, parameters, type parameters and local functions have
        /// no documentation id, so their identifiers get a key that is unique in the document (<c>data-local-def</c> and
        /// <c>data-local-ref</c> attributes).
        /// </summary>
        public bool AddSymbols { get; init; }
    }
}
