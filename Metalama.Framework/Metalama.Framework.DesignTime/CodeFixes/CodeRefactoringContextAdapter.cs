// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;

namespace Metalama.Framework.DesignTime.CodeFixes;

internal sealed class CodeRefactoringContextAdapter : ICodeRefactoringContext
{
    private readonly CodeRefactoringContext _context;

    public CodeRefactoringContextAdapter( CodeRefactoringContext context )
    {
        this._context = context;
    }

    public Document Document => this._context.Document;

    public TextSpan Span => this._context.Span;

    public CancellationToken CancellationToken => this._context.CancellationToken;

    // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
    public void RegisterRefactoring( CodeAction action ) => this._context.RegisterRefactoring( action );
}