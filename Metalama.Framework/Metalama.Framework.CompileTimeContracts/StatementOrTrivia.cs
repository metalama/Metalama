// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.CompileTimeContracts
{
    /// <summary>
    /// Represents either a <see cref="StatementSyntax"/>, a <see cref="SyntaxTriviaList"/>, or <c>null</c>.
    /// </summary>
    [PublicAPI]
    public readonly struct StatementOrTrivia
    {
        public object? Content { get; }

        public StatementOrTrivia( StatementSyntax? statement )
        {
            this.Content = statement;
        }

        public StatementOrTrivia( SyntaxTriviaList triviaList )
        {
            this.Content = triviaList;
        }
    }
}