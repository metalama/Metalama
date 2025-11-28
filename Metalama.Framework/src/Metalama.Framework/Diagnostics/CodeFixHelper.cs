// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics;

/// <summary>
/// Provides helper members for working with code fixes in diagnostics.
/// </summary>
/// <remarks>
/// This class provides constants and a predefined diagnostic definition for adding code fixes to aspects.
/// Code fixes allow aspects to suggest automated corrections to code issues they detect.
/// </remarks>
/// <seealso href="@diagnostics"/>
[CompileTime]
public static class CodeFixHelper
{
    /// <summary>
    /// The name of the diagnostic property that contains code fix titles.
    /// </summary>
    public const string DiagnosticPropertyName = "CodeFixes";

    /// <summary>
    /// The character used to separate multiple code fix titles in the diagnostic property.
    /// </summary>
    public const char DiagnosticPropertyValueSeparator = '\n';

    /// <summary>
    /// A predefined <see cref="DiagnosticDefinition"/> that can be used to add code fixes without reporting a visible diagnostic.
    /// </summary>
    /// <remarks>
    /// This diagnostic uses <see cref="Severity.Hidden"/> so it doesn't appear in the IDE's error list,
    /// but still allows code fixes to be attached. Requires a premium edition of Metalama.
    /// </remarks>
    public static readonly DiagnosticDefinition SuggestionDiagnostic = new( "LAMA0043", Severity.Hidden, "Code fix" );
}