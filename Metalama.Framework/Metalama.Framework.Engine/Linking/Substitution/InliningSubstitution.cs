// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution
{
    /// <summary>
    /// Substitutes a node using an inliner.
    /// </summary>
    internal sealed class InliningSubstitution : SyntaxNodeSubstitution
    {
        private readonly InliningSpecification _specification;

        public override SyntaxNode ReplacedNode => this._specification.ReplacedNode;

        public InliningSubstitution( CompilationContext compilationContext, InliningSpecification specification ) : base( compilationContext )
        {
            this._specification = specification;
        }

        public override string ToString() => $"{this.ReplacedNode.Kind()} -> {this._specification}";

        public override SyntaxNode Substitute( SyntaxNode currentNode, SubstitutionContext context )
        {
            var statements = new List<StatementSyntax>();
            var syntaxGenerator = context.SyntaxGenerationContext.SyntaxGenerator;

            if ( this._specification.DeclareReturnVariable )
            {
                statements.Add(
                    LocalDeclarationStatement(
                            VariableDeclaration(
                                syntaxGenerator.TypeSyntax( GetReturnType( this._specification.AspectReference.OriginalSymbol ) ),
                                SingletonSeparatedList( VariableDeclarator( this._specification.ReturnVariableIdentifier.AssertNotNull() ) ) ) )
                        .WithOptionalTrailingLineFeed( context.SyntaxGenerationContext )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) );
            }

            // Get substituted body of the target.
            var substitutedBody = context.RewritingDriver.GetSubstitutedBody(
                this._specification.TargetSemantic,
                context.WithInliningContext( this._specification.ContextIdentifier ) );

            // Let the inliner to transform that.
            var inlinedBody = this._specification.Inliner.Inline( context.SyntaxGenerationContext, this._specification, currentNode, substitutedBody );

            statements.Add( inlinedBody );

            if ( this._specification.ReturnLabelIdentifier != null )
            {
                statements.Add(
                    LabeledStatement(
                            Identifier( this._specification.ReturnLabelIdentifier.AssertNotNull() ),
                            EmptyStatement() )
                        .WithOptionalTrailingLineFeed( context.SyntaxGenerationContext )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.EmptyLabeledStatement ) );
            }

            return syntaxGenerator.FormattedBlock( statements )
                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
        }

        private static ITypeSymbol GetReturnType( ISymbol symbol )
        {
            switch ( symbol )
            {
                case IMethodSymbol method:
                    return method.ReturnType;

                case IPropertySymbol property:
                    return property.Type;

                case IEventSymbol @event:
                    return @event.Type;

                default:
                    throw new AssertionFailedException( $"Unsupported: {symbol}" );
            }
        }
    }
}