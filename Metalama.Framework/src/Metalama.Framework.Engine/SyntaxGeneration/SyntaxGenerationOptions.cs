// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Services;
using Microsoft.CodeAnalysis;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public sealed record SyntaxGenerationOptions : IProjectService
{
    private readonly CodeFormattingOptions _codeFormattingOptions;

    /// <summary>
    /// Gets a value indicating whether the syntax tree produced under these options is
    /// intended to be textualized to C# source. When <c>false</c> (i.e.
    /// <see cref="CodeFormattingOptions.None"/>), the generator skips emission of elastic
    /// spacing trivia and <c>NormalizeWhitespace</c> calls; the resulting tree is
    /// therefore not safe to <see cref="SyntaxNode.ToFullString"/>, because tokens may be
    /// left adjacent with no separator. Directive trivia is always preserved regardless
    /// of this flag.
    /// </summary>
    /// <remarks>
    /// This predicate also gates whether <c>NormalizeWhitespace(elasticTrivia: true)</c>
    /// is run during generation. Normalization is required whenever the tree will be
    /// textualized (including in <see cref="CodeFormattingOptions.Formatted"/> mode),
    /// because Roslyn's <c>Formatter</c> reflows only elastic trivia and preserves
    /// non-elastic trivia verbatim — it does not synthesize new trivia between tokens
    /// that have none. <c>NormalizeWhitespace(elasticTrivia: true)</c> is what sprays
    /// those elastic markers through the tree.
    /// </remarks>
    internal bool WillBeTextualized => this._codeFormattingOptions != CodeFormattingOptions.None;

    /// <summary>
    /// Gets a value indicating whether the syntax tree produced under these options will
    /// be post-processed by the <c>CodeFormatter</c> (simplification + reformat).
    /// When <c>true</c>, generation attaches <c>Simplifier.Annotation</c> to candidate
    /// nodes so the post-pass can reduce redundant namespace qualifications, casts, and
    /// similar over-specifications. Only <c>true</c> for
    /// <see cref="CodeFormattingOptions.Formatted"/>.
    /// </summary>
    internal bool WillBeFormatted => this._codeFormattingOptions == CodeFormattingOptions.Formatted;

    internal SyntaxGenerationOptions( CodeFormattingOptions options )
    {
        this._codeFormattingOptions = options;
    }

    /// <summary>
    /// Gets options for creation of fully formatted code.
    /// </summary>
    public static SyntaxGenerationOptions Formatted { get; } = new( CodeFormattingOptions.Formatted );

    internal static SyntaxGenerationOptions Unformatted { get; } = new( CodeFormattingOptions.None );
}