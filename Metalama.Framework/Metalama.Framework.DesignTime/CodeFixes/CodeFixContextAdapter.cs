// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeFixes;

internal sealed class CodeFixContextAdapter : ICodeFixContext
{
    private readonly CodeFixContext _context;

    public CodeFixContextAdapter( CodeFixContext context )
    {
        this._context = context;
    }

    public Document Document => this._context.Document;

    public ImmutableArray<Diagnostic> Diagnostics => this._context.Diagnostics;

    public TextSpan Span => this._context.Span;

    public CancellationToken CancellationToken => this._context.CancellationToken;

    public void RegisterCodeFix( CodeAction action, ImmutableArray<Diagnostic> diagnostics )
    {
        // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
        this._context.RegisterCodeFix( action, diagnostics );
    }
}