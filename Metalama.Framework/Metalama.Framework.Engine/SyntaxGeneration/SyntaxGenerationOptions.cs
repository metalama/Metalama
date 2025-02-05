// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.SyntaxGeneration;

public sealed record SyntaxGenerationOptions : IProjectService
{
    private readonly CodeFormattingOptions _codeFormattingOptions;

    // We must normalize whitespace even if we later run the formatter because the formatter requires existing whitespace.
    internal bool NormalizeWhitespace => this._codeFormattingOptions != CodeFormattingOptions.None;

    internal bool TriviaMatters => this._codeFormattingOptions != CodeFormattingOptions.None;

    internal bool AddFormattingAnnotations => this._codeFormattingOptions == CodeFormattingOptions.Formatted;

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