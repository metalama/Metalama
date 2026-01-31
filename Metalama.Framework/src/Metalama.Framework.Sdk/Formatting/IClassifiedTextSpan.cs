// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Metalama.Framework.Engine.Formatting;

/// <summary>
/// Represents a classified text span with its semantic properties.
/// </summary>
public interface IClassifiedTextSpan
{
    /// <summary>
    /// Gets the text span.
    /// </summary>
    TextSpan Span { get; }

    /// <summary>
    /// Gets the classification of the span.
    /// </summary>
    TextSpanClassification Classification { get; }

    /// <summary>
    /// Gets the diagnostic associated with this span, if any.
    /// </summary>
    Diagnostic? Diagnostic { get; }

    /// <summary>
    /// Gets the C# classification(s) for this span (e.g., "keyword", "identifier").
    /// Multiple classifications are separated by semicolons.
    /// </summary>
    string? CSharpClassification { get; }

    /// <summary>
    /// Gets the title/tooltip text for this span.
    /// </summary>
    string? Title { get; }

    /// <summary>
    /// Gets the name of the aspect that generated this code, if applicable.
    /// </summary>
    string? GeneratingAspect { get; }
}
