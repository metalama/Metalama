// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Diagnostics;

[CompileTime]
public static class CodeFixHelper
{
    /// <summary>
    /// The name of the property representing the titles of code fixes.
    /// </summary>
    public const string DiagnosticPropertyName = "CodeFixes";

    /// <summary>
    /// The character separating values in the code fix properties.
    /// </summary>
    public const char DiagnosticPropertyValueSeparator = '\n';
    
    /// <summary>
    /// A <see cref="DiagnosticDefinition"/> that can be used to add code fixes. Requires a premium edition.
    /// </summary>
    public static readonly DiagnosticDefinition SuggestionDiagnostic = new( "LAMA0043", Severity.Hidden, "Code fix" );
}