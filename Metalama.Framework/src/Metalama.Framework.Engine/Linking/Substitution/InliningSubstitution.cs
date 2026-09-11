// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking.Inlining;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking.Substitution
{
    /// <summary>
    /// Substitutes a node using an inliner.
    /// </summary>
    internal sealed partial class InliningSubstitution : SyntaxNodeSubstitution
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

            statements.Add( this.RenameCollidingLabels( inlinedBody ) );

            if ( this._specification.ReturnLabelIdentifier != null )
            {
                statements.Add(
                    LabeledStatement(
                            SyntaxFactoryEx.SafeIdentifier( this._specification.ReturnLabelIdentifier.AssertNotNull() ),
                            EmptyStatement() )
                        .WithOptionalTrailingLineFeed( context.SyntaxGenerationContext )
                        .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.EmptyLabeledStatement ) );
            }

            return syntaxGenerator.FormattedBlock( statements )
                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
        }

        /// <summary>
        /// Renames the labels that the inlined body declares and that the destination body declares under the same
        /// name, and returns the rewritten body.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The inlined body is spliced into the statement list of the destination body. Two declarations of one label
        /// name therefore reach one declaration space, which the compiler reports as CS0140, or one of them shadows
        /// the other, which the compiler reports as CS0158. Renaming the labels of the inlined body removes both.
        /// </para>
        /// <para>
        /// The new name is derived from the identifier of the inlining, which is unique within the destination body
        /// and is allocated deterministically. The lexical scope factory is not used, because it is constructed by the
        /// injection step and is not available to the linking step.
        /// </para>
        /// </remarks>
        private StatementSyntax RenameCollidingLabels( StatementSyntax inlinedBody )
        {
            var inlinedLabels = GetDeclaredLabels( inlinedBody );

            if ( inlinedLabels.Count == 0 )
            {
                return inlinedBody;
            }

            // The replaced node belongs to the destination body of the intermediate compilation, so the declaration
            // that contains it gives the labels that the destination declares. The labels that the replaced node
            // itself contains are excluded, because the inlined body takes the place of that node and those labels do
            // not survive it. The exclusion matters for the implicit reference to the last override, whose replaced
            // node is the whole body of the destination. When the declaration cannot be reached, every label of the
            // inlined body is renamed, because a collision cannot then be ruled out.
            var destinationDeclaration = this._specification.ReplacedNode.FirstAncestorOrSelf<MemberDeclarationSyntax>();

            var destinationLabels = destinationDeclaration != null
                ? GetDeclaredLabels( destinationDeclaration, this._specification.ReplacedNode )
                : null;

            Dictionary<string, string>? renamedLabels = null;

            foreach ( var inlinedLabel in inlinedLabels )
            {
                if ( destinationLabels != null && !destinationLabels.Contains( inlinedLabel ) )
                {
                    continue;
                }

                renamedLabels ??= new Dictionary<string, string>( StringComparer.Ordinal );

                var newName = $"{inlinedLabel}_{this._specification.ContextIdentifier.InliningId.AssertNotNull()}";

                while ( inlinedLabels.Contains( newName )
                        || destinationLabels?.Contains( newName ) == true
                        || renamedLabels.ContainsValue( newName ) )
                {
                    newName += "_";
                }

                renamedLabels.Add( inlinedLabel, newName );
            }

            if ( renamedLabels == null )
            {
                return inlinedBody;
            }

            return (StatementSyntax) new LabelRenamingRewriter( renamedLabels ).Visit( inlinedBody ).AssertNotNull();
        }

        /// <summary>
        /// Gets the names of the labels that a node declares, excluding the labels that the linker itself generated.
        /// </summary>
        /// <remarks>
        /// A generated label is excluded because the linker allocates its name and because the return label of an
        /// inlining is added after the inlined body while the <c>goto</c> statements that target it are inside that
        /// body. Renaming it inside the body alone would separate the two.
        /// </remarks>
        private static HashSet<string> GetDeclaredLabels( SyntaxNode node, SyntaxNode? excludedNode = null )
        {
            var labels = new HashSet<string>( StringComparer.Ordinal );

            foreach ( var labeledStatement in node.DescendantNodes().OfType<LabeledStatementSyntax>() )
            {
                if ( labeledStatement.GetLinkerGeneratedFlags().HasFlagFast( LinkerGeneratedFlags.EmptyLabeledStatement ) )
                {
                    continue;
                }

                if ( excludedNode != null && excludedNode.Span.Contains( labeledStatement.Span ) )
                {
                    continue;
                }

                labels.Add( labeledStatement.Identifier.ValueText );
            }

            return labels;
        }

        private static ITypeSymbol GetReturnType( ISymbol symbol )
        {
            switch ( symbol.Kind )
            {
                case SymbolKind.Method when symbol is IMethodSymbol method:
                    return method.ReturnType;

                case SymbolKind.Property when symbol is IPropertySymbol property:
                    return property.Type;

                case SymbolKind.Event when symbol is IEventSymbol @event:
                    return @event.Type;

                default:
                    throw new AssertionFailedException( $"Unsupported: {symbol}" );
            }
        }
    }
}