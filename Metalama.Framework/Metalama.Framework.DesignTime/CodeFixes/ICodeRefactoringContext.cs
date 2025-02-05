// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;

namespace Metalama.Framework.DesignTime.CodeFixes;

public interface ICodeRefactoringContext
{
    Document Document { get; }

    TextSpan Span { get; }

    CancellationToken CancellationToken { get; }

    void RegisterRefactoring( CodeAction action );
}