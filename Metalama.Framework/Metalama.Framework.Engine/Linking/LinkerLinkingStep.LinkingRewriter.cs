// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerLinkingStep
{
    /// <summary>
    /// Rewriter which rewrites classes and methods producing the linked and inlined syntax tree.
    /// </summary>
    private sealed class LinkingRewriter : SafeSyntaxRewriter
    {
        private readonly CompilationContext _compilationContext;
        private readonly SemanticModelProvider _semanticModelProvider;
        private readonly LinkerRewritingDriver _rewritingDriver;

        public LinkingRewriter(
            CompilationContext intermediateCompilationContext,
            LinkerRewritingDriver rewritingDriver )
        {
            this._compilationContext = intermediateCompilationContext;
            this._semanticModelProvider = intermediateCompilationContext.SemanticModelProvider;
            this._rewritingDriver = rewritingDriver;
        }

        public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node )
        {
            var transformedMembers = this.GetMembersForTypeDeclaration( node );

            var semanticModel = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree );

            var symbol = semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

            node = this._rewritingDriver.RewriteStruct( node, symbol, transformedMembers );

            return node;
        }

        public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node )
        {
            var transformedMembers = this.GetMembersForTypeDeclaration( node );

            var semanticModel = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree );

            var symbol = semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

            node = this._rewritingDriver.RewriteClass( node, symbol, transformedMembers );

            return node;
        }

        public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node )
            => node.WithMembers( List( this.GetMembersForTypeDeclaration( node ) ) );

        public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node )
        {
            var transformedMembers = this.GetMembersForTypeDeclaration( node ).AssertNotNull();

            var semanticModel = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree );

            var symbol = semanticModel.GetDeclaredSymbol( node ).AssertNotNull();

            node = this._rewritingDriver.RewriteRecord( node, symbol, transformedMembers );

            return node;
        }

        private IReadOnlyList<MemberDeclarationSyntax> GetMembersForTypeDeclaration( TypeDeclarationSyntax node )
        {
            var newMembers = new List<MemberDeclarationSyntax>();

            foreach ( var member in node.Members )
            {
                // Go through all members of the type.
                // For members that represent overrides:
                //  * If the member can be inlined, skip it.
                //  * If the member cannot be inlined (or is the root of inlining), add the transformed member with all possible inlining instances.
                // For members that represent override targets (i.e. overridden members):
                //  * If the last (transformation order) override is inlineable, replace the member with it's transformed body.
                //  * Otherwise create a stub that calls the last override.

                var semanticModel = this._semanticModelProvider.GetSemanticModel( node.SyntaxTree );

                var symbols =
                    member switch
                    {
                        ConstructorDeclarationSyntax ctorDecl => [semanticModel.GetDeclaredSymbol( ctorDecl )],
                        OperatorDeclarationSyntax operatorDecl => [semanticModel.GetDeclaredSymbol( operatorDecl )],
                        ConversionOperatorDeclarationSyntax destructorDecl => [semanticModel.GetDeclaredSymbol( destructorDecl )],
                        DestructorDeclarationSyntax destructorDecl => [semanticModel.GetDeclaredSymbol( destructorDecl )],
                        MethodDeclarationSyntax methodDecl => [semanticModel.GetDeclaredSymbol( methodDecl )],
                        BasePropertyDeclarationSyntax basePropertyDecl => [semanticModel.GetDeclaredSymbol( basePropertyDecl )],
                        FieldDeclarationSyntax fieldDecl =>
                            fieldDecl.Declaration.Variables.SelectAsArray( v => semanticModel.GetDeclaredSymbol( v ) ),
                        EventFieldDeclarationSyntax eventFieldDecl =>
                            eventFieldDecl.Declaration.Variables.SelectAsArray( v => semanticModel.GetDeclaredSymbol( v ) ),
                        _ => []
                    };

                if ( symbols.Length == 0 || symbols is [null] )
                {
                    // TODO: Comment when this happens.
                    newMembers.Add( (MemberDeclarationSyntax) this.Visit( member )! );

                    continue;
                }

                SyntaxGenerationContext? generationContext = null;

                SyntaxGenerationContext GetSyntaxGenerationContext()
                {
                    return generationContext ??= this._compilationContext.GetSyntaxGenerationContext(
                        this._rewritingDriver.SyntaxGenerationOptions,
                        node.SyntaxTree,
                        member.SpanStart );
                }

                if ( symbols.Length == 1 )
                {
                    // Simple case where the declaration declares a single symbol.
                    if ( this._rewritingDriver.IsRewriteTarget( symbols[0].AssertNotNull() ) )
                    {
                        // Add rewritten member and it's induced members (or nothing if the member is discarded).
                        newMembers.AddRange( this._rewritingDriver.RewriteMember( member, symbols[0].AssertNotNull(), GetSyntaxGenerationContext() ) );
                    }
                    else
                    {
                        // Normal member without any transformations.
                        newMembers.Add( (MemberDeclarationSyntax) this.Visit( member )! );
                    }
                }
                else
                {
                    var remainingSymbols = new HashSet<ISymbol>( this._compilationContext.SymbolComparer );

                    foreach ( var symbol in symbols )
                    {
                        if ( this._rewritingDriver.IsRewriteTarget( symbol.AssertNotNull() ) )
                        {
                            newMembers.AddRange( this._rewritingDriver.RewriteMember( member, symbol.AssertNotNull(), GetSyntaxGenerationContext() ) );
                        }
                        else
                        {
                            remainingSymbols.Add( symbol.AssertNotNull() );
                        }
                    }

                    if ( remainingSymbols.Count == symbols.Length )
                    {
                        // No change.
                        newMembers.Add( member );
                    }
                    else if ( remainingSymbols.Count > 0 )
                    {
                        // Remove declarators that were rewritten.
                        switch ( member )
                        {
                            case EventFieldDeclarationSyntax eventFieldDecl:
                                newMembers.Add(
                                    eventFieldDecl.WithDeclaration(
                                        eventFieldDecl.Declaration.WithVariables(
                                            SeparatedList(
                                                eventFieldDecl.Declaration.Variables
                                                    .Where(
                                                        v =>
                                                            remainingSymbols.Contains( semanticModel.GetDeclaredSymbol( v ).AssertNotNull() ) ) ) ) ) );

                                break;

                            default:
                                throw new AssertionFailedException( $"Unexpected member: {member}" );
                        }
                    }
                }
            }

            return newMembers;
        }
    }
}