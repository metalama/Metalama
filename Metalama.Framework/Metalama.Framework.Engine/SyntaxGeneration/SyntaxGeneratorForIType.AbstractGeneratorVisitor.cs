// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Visitors;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Metalama.Framework.Engine.SyntaxGeneration;

internal sealed partial class SyntaxGeneratorForIType
{
    private abstract class AbstractGeneratorVisitor<T> : TypeVisitor<T>
    {
        protected SyntaxGeneratorForIType SyntaxGenerator { get; }

        protected AbstractGeneratorVisitor( SyntaxGeneratorForIType syntaxGeneratorForIType )
        {
            this.SyntaxGenerator = syntaxGeneratorForIType;
        }

        protected override T DefaultVisit( IType type ) => throw new AssertionFailedException();

        protected TArg AddInformationTo<TArg>( TArg syntax, ICompilationElement symbol )
            where TArg : ExpressionSyntax
        {
            var generationOptions = this.SyntaxGenerator._generationOptions;

            if ( generationOptions.TriviaMatters )
            {
                syntax = syntax
                    .WithRequiredLeadingTrivia( syntax.GetLeadingTrivia().Insert( 0, SyntaxFactory.ElasticMarker ) )
                    .WithRequiredTrailingTrivia( syntax.GetTrailingTrivia().Add( SyntaxFactory.ElasticMarker ) );
            }

            if ( generationOptions.AddFormattingAnnotations )
            {
                syntax = syntax.WithAdditionalAnnotations( SymbolAnnotation.Create( symbol ) );
            }

            return syntax;
        }

        protected static IdentifierNameSyntax ToIdentifierName( string identifier )
            => (IdentifierNameSyntax) RoslynSyntaxGenerator.IdentifierName( identifier );
    }
}