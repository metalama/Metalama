// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Formatting
{
    /// <summary>
    /// Options influencing the HTML writing behavior of the test framework.
    /// </summary>
    /// <param name="AddTitles">Whether to add titles to HTML elements.</param>
    /// <param name="Prolog">Optional HTML prolog content.</param>
    /// <param name="Epilogue">Optional HTML epilogue content.</param>
    /// <param name="ProjectPath">The path to the project file. Used by <see cref="IHtmlCodeWriter.WriteAllDiffAsync"/>.</param>
    /// <param name="TargetFramework">The target framework. Used by <see cref="IHtmlCodeWriter.WriteAllDiffAsync"/>.</param>
    public sealed record HtmlCodeWriterOptions(
        bool AddTitles = false,
        string? Prolog = null,
        string? Epilogue = null,
        string? ProjectPath = null,
        string? TargetFramework = null );
}