// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace Metalama.Framework.DesignTime.CodeFixes;

public interface ICodeFixContext
{
    Document Document { get; }

    ImmutableArray<Diagnostic> Diagnostics { get; }

    TextSpan Span { get; }

    CancellationToken CancellationToken { get; }

    void RegisterCodeFix( CodeAction action, ImmutableArray<Diagnostic> diagnostics );
}