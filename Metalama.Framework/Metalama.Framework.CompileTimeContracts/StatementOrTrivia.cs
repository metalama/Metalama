// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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