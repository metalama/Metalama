// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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