// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.AdviceImpl.Introduction;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Templating;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Metalama.Framework.Code.MethodKind;

namespace Metalama.Framework.Engine.Linking;

internal sealed partial class LinkerInjectionStep
{
    private sealed class Rewriter : SafeSyntaxRewriter
    {
        private readonly CompilationModel _compilation;
        private readonly SemanticModelProvider _semanticModelProvider;

        private readonly LinkerInjectionStep _parent;
        private readonly TransformationCollection _transformationCollection;
        private readonly SyntaxTree _syntaxTreeForGlobalAttributes;

        public Rewriter(
            LinkerInjectionStep parent,
            TransformationCollection syntaxTransformationCollection,
            CompilationModel compilation,
            SyntaxTree syntaxTreeForGlobalAttributes )
        {
            this._parent = parent;
            this._compilation = compilation;
            this._transformationCollection = syntaxTransformationCollection;
            this._semanticModelProvider = compilation.RoslynCompilation.GetSemanticModelProvider();
            this._syntaxTreeForGlobalAttributes = syntaxTreeForGlobalAttributes;
        }

        private RefFactory RefFactory => this._compilation.RefFactory;

        private CompilationContext CompilationContext => this._parent._compilationContext;

        private SyntaxGenerationOptions SyntaxGenerationOptions => this._parent._syntaxGenerationOptions;

        private SyntaxGenerationContext GetSyntaxGenerationContext( SyntaxNode node )
            => this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, node );

        public override bool VisitIntoStructuredTrivia => true;

        private (SyntaxList<AttributeListSyntax> Attributes, List<SyntaxTrivia> Trivia)? RewriteDeclarationAttributeLists(
            SyntaxNode originalDeclaringNode,
            SyntaxList<AttributeListSyntax> attributeLists,
            SyntaxNode? originalNodeForTrivia = null )
        {
            if ( !this._transformationCollection.IsNodeWithModifiedAttributes( originalDeclaringNode ) )
            {
                return null;
            }

            // Resolve the symbol.
            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalDeclaringNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalDeclaringNode );

            if ( symbol == null )
            {
                return null;
            }

            // Find trivia that's directly on the declaration (and not on its attributes).
            originalNodeForTrivia ??= originalDeclaringNode;

            if ( attributeLists.Any() )
            {
                originalNodeForTrivia = originalNodeForTrivia.RemoveNodes( attributeLists, SyntaxRemoveOptions.KeepNoTrivia )!;
            }

            var originalDeclarationTrivia = originalNodeForTrivia.GetLeadingTrivia();

            var outputLists = new List<AttributeListSyntax>();
            var outputTrivias = new List<SyntaxTrivia>( originalDeclarationTrivia );
            SyntaxGenerationContext? syntaxGenerationContext = null;

            this.RewriteAttributeLists(
                this.RefFactory.FromDeclarationSymbol( symbol ),
                SyntaxKind.None,
                originalDeclaringNode,
                attributeLists,
                ( a, n ) => a.ContainingDeclaration.GetPrimaryDeclarationSyntax() == n,
                outputLists,
                outputTrivias,
                ref syntaxGenerationContext );

            switch ( symbol )
            {
                case IMethodSymbol method:
                    this.RewriteAttributeLists(
                        this.RefFactory.FromReturnParameter( method ),
                        SyntaxKind.ReturnKeyword,
                        originalDeclaringNode,
                        attributeLists,
                        ( a, n ) => a.ContainingDeclaration.GetPrimaryDeclarationSyntax() == n,
                        outputLists,
                        outputTrivias,
                        ref syntaxGenerationContext );

                    break;
            }

            if ( outputLists.Count == 0 )
            {
                return (default, outputTrivias);
            }
            else
            {
                return (List( outputLists ), outputTrivias);
            }
        }

        private void RewriteAttributeLists(
            IFullRef<IDeclaration> target,
            SyntaxKind targetKind,
            SyntaxNode originalDeclaringNode,
            SyntaxList<AttributeListSyntax> inputAttributeLists,
            Func<AttributeBuilderData, SyntaxNode, bool> isPrimaryNode,
            List<AttributeListSyntax> outputAttributeLists,
            List<SyntaxTrivia> outputTrivia,
            ref SyntaxGenerationContext? syntaxGenerationContext )
        {
            // Get the final list of attributes.
            var finalModelAttributes = this._compilation.GetAttributeCollection( target );

            List<SyntaxTrivia>? firstListLeadingTrivia = null;
            var isFirstList = true;

            // Remove attributes from the list.
            foreach ( var list in inputAttributeLists )
            {
                var wasFirstList = isFirstList;
                isFirstList = false;

                // Skip the different target kinds.
                if ( list.Target == null )
                {
                    if ( targetKind != SyntaxKind.None )
                    {
                        continue;
                    }
                }
                else
                {
                    if ( !list.Target.Identifier.IsKind( targetKind ) )
                    {
                        continue;
                    }
                }

                var modifiedList = list;

                foreach ( var attribute in list.Attributes )
                {
                    if ( !finalModelAttributes.Any( a => a.IsSyntax( attribute ) ) )
                    {
                        modifiedList = modifiedList.RemoveNode( attribute, SyntaxRemoveOptions.KeepDirectives )!;
                    }
                }

                if ( modifiedList.Attributes.Count > 0 )
                {
                    outputAttributeLists.Add( modifiedList );
                }
                else
                {
                    // If we are removing a custom attribute, keep its trivia.
                    foreach ( var trivia in list.GetLeadingTrivia() )
                    {
                        if ( trivia.Kind() is SyntaxKind.SingleLineCommentTrivia
                            or SyntaxKind.MultiLineCommentTrivia
                            or SyntaxKind.SingleLineDocumentationCommentTrivia
                            or SyntaxKind.MultiLineDocumentationCommentTrivia )
                        {
                            List<SyntaxTrivia> targetList;

                            if ( wasFirstList )
                            {
                                // Trivia preceding the first attribute list needs to before the first final attribute list.
                                targetList = firstListLeadingTrivia ??= [];
                            }
                            else
                            {
                                targetList = outputTrivia;
                            }

                            targetList.Add( trivia );

                            if ( trivia.Kind() is SyntaxKind.SingleLineCommentTrivia or SyntaxKind.SingleLineDocumentationCommentTrivia )
                            {
                                syntaxGenerationContext ??= this.GetSyntaxGenerationContext( originalDeclaringNode );
                                targetList.Add( syntaxGenerationContext.ElasticEndOfLineTrivia );
                            }

                            break;
                        }
                    }
                }
            }

            // Add new attributes.
            foreach ( var attribute in finalModelAttributes )
            {
#pragma warning disable CS0618 // Type or member is obsolete
                if ( attribute is IntroducedAttributeRef builderAttributeRef && isPrimaryNode( builderAttributeRef.BuilderData, originalDeclaringNode ) )
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    syntaxGenerationContext ??= this.GetSyntaxGenerationContext( originalDeclaringNode );

                    var attributeModel = builderAttributeRef.GetTarget( this._compilation );

                    var newAttribute = syntaxGenerationContext.SyntaxGenerator.Attribute( attributeModel )
                        .AssertNotNull();

                    var newList = AttributeList( SingletonSeparatedList( newAttribute ) )
                        .WithOptionalTrailingLineFeed( syntaxGenerationContext )
                        .WithAdditionalAnnotations(
                            builderAttributeRef.BuilderData.ParentAdvice?.AspectInstance.AspectClass.GeneratedCodeAnnotation
                            ?? FormattingAnnotations.SystemGeneratedCodeAnnotation );

                    if ( targetKind != SyntaxKind.None )
                    {
                        newList = newList.WithTarget( AttributeTargetSpecifier( Token( targetKind ) ) );
                    }

                    if ( outputTrivia.Any() && !outputAttributeLists.Any() )
                    {
                        newList = newList.WithRequiredLeadingTrivia( newList.GetLeadingTrivia().InsertRange( 0, outputTrivia ) );

                        outputTrivia.Clear();
                    }

                    outputAttributeLists.Add( newList );
                }
            }

            if ( firstListLeadingTrivia != null )
            {
                if ( outputAttributeLists.Count > 0 )
                {
                    syntaxGenerationContext ??= this.GetSyntaxGenerationContext( originalDeclaringNode );

                    outputAttributeLists[0] =
                        outputAttributeLists[0]
                            .WithRequiredLeadingTrivia( outputAttributeLists[0].GetLeadingTrivia().AddRange( firstListLeadingTrivia ) );
                }
                else
                {
                    outputTrivia.InsertRange( 0, firstListLeadingTrivia );
                }
            }
        }

        private T ReplaceAttributes<T>( T node, (SyntaxList<AttributeListSyntax> Attributes, List<SyntaxTrivia> Trivia)? attributesTuple )
            where T : MemberDeclarationSyntax
        {
            if ( attributesTuple is var (attributes, trivia) )
            {
                if ( trivia.ShouldBePreserved( this.SyntaxGenerationOptions ) || (node.HasLeadingTrivia && trivia.Count == 0) )
                {
                    return (T) node.WithAttributeLists( default ).WithRequiredLeadingTrivia( trivia ).WithAttributeLists( attributes );
                }
                else
                {
                    return (T) node.WithAttributeLists( attributes );
                }
            }
            else
            {
                return node;
            }
        }

        public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override SyntaxNode VisitRecordDeclaration( RecordDeclarationSyntax node ) => this.VisitTypeDeclaration( node );

        public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node )
        {
            var originalNode = node;
            var members = new List<EnumMemberDeclarationSyntax>( node.Members.Count );

            // Process the type members.
            foreach ( var member in node.Members )
            {
                var visitedMember = this.VisitEnumMemberDeclarationCore( member );

                members.Add( visitedMember );
            }

            node = node.WithMembers( SeparatedList( members ) );

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node )
        {
            var originalNode = node;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private SyntaxNode VisitTypeDeclaration<T>( T node )
            where T : TypeDeclarationSyntax
        {
            var originalNode = node;
            var members = new List<MemberDeclarationSyntax>( node.Members.Count );
            var additionalBaseList = this._transformationCollection.GetIntroducedInterfacesForTypeDeclaration( node );
            var syntaxGenerationContext = this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, node );

            var baseList = node.BaseList;
            var parameterList = node.GetParameterList();

            this.ApplyMemberLevelTransformationsToPrimaryConstructor(
                node,
                syntaxGenerationContext,
                ref baseList,
                ref parameterList );

            // Process the type members.
            foreach ( var member in node.Members )
            {
                foreach ( var visitedMember in this.VisitMember( member ) )
                {
                    members.Add( visitedMember );
                }

                // We have to call AddIntroductionsOnPosition outside of the previous suppression scope, otherwise we don't get new suppressions.
                this.AddInjectionsOnPosition(
                    new InsertPosition( InsertPositionRelation.After, member ),
                    originalNode.SyntaxTree,
                    members,
                    syntaxGenerationContext );
            }

            this.AddInjectionsOnPosition(
                new InsertPosition( InsertPositionRelation.Within, node ),
                originalNode.SyntaxTree,
                members,
                syntaxGenerationContext );

            // If the type has no braces, add them.
            if ( node.OpenBraceToken.IsKind( SyntaxKind.None ) && members.Count > 0 )
            {
                // TODO: trivias.
                node = (T) node
                    .WithOpenBraceToken( Token( SyntaxKind.OpenBraceToken ).AddColoringAnnotation( TextSpanClassification.GeneratedCode ) )
                    .WithCloseBraceToken( Token( SyntaxKind.CloseBraceToken ).AddColoringAnnotation( TextSpanClassification.GeneratedCode ) )
                    .WithSemicolonToken( default );
            }

            node = (T) node.WithMembers( List( members ) );

            // Process the type bases.
            if ( additionalBaseList.Any() )
            {
                if ( baseList == null )
                {
                    node = (T) node
                        .WithIdentifier(
                            node.Identifier.WithOptionalTrailingTrivia(
                                default,
                                syntaxGenerationContext.Options.TriviaMatters || node.Identifier.ContainsDirectives ) )
                        .WithBaseList(
                            BaseList( SeparatedList( additionalBaseList.SelectAsReadOnlyList( i => i.Syntax ) ) )
                                .WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) )
                        .WithOptionalTrailingTrivia( node.Identifier.TrailingTrivia, syntaxGenerationContext.Options );
                }
                else
                {
                    node = (T) node.WithBaseList(
                        BaseList(
                            baseList.Types.AddRange(
                                additionalBaseList.SelectAsReadOnlyList(
                                    i => i.Syntax.WithGeneratedCodeAnnotation( FormattingAnnotations.SystemGeneratedCodeAnnotation ) ) ) ) );
                }
            }
            else if ( baseList != null )
            {
                node = (T) node.WithBaseList( baseList );
            }

            node = (T) node.WithParameterList( parameterList );

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private void AddInjectionsOnPosition<T>(
            InsertPosition position,
            SyntaxTree originalSyntaxTree,
            List<T> targetList,
            SyntaxGenerationContext syntaxGenerationContext )
            where T : MemberDeclarationSyntax
        {
            var injectedMembersAtPosition = this._transformationCollection.GetInjectedMembersOnPosition( position );

            foreach ( var injectedMember in injectedMembersAtPosition )
            {
                // We should inject into a correct syntax tree.
                Invariant.Assert( injectedMember.GetTargetSyntaxTree() == originalSyntaxTree );

                // Allow for tracking of the node inserted.
                // IMPORTANT: This need to be here and cannot be in injectedMember.Syntax, result of TrackNodes is not trackable!
                var injectedNode = injectedMember.Syntax.TrackNodes( injectedMember.Syntax );

                switch ( injectedMember.Declaration )
                {
                    case IFullRef<IMethodBase> methodBase:
                        // TODO: AssertNotNull is needed due to some weird bug in Roslyn.
                        var entryStatements = this._transformationCollection.GetInjectedEntryStatements( injectedMember );
                        var exitStatements = this._transformationCollection.GetInjectedExitStatements( injectedMember );

                        injectedNode = InjectStatementsIntoMemberDeclaration(
                            methodBase,
                            entryStatements,
                            exitStatements,
                            injectedNode );

                        break;

                    case IFullRef<IPropertyOrIndexer> propertyOrIndexerRef:
                        var propertyOrIndexer = propertyOrIndexerRef.Definition;

                        if ( propertyOrIndexer.GetMethod != null )
                        {
                            var getEntryStatements = this._transformationCollection.GetInjectedEntryStatements(
                                propertyOrIndexer.GetMethod.ToRef(),
                                propertyOrIndexerRef,
                                injectedMember );

                            var getExitStatements = this._transformationCollection.GetInjectedExitStatements(
                                propertyOrIndexer.GetMethod.ToRef(),
                                propertyOrIndexerRef,
                                injectedMember );

                            injectedNode = InjectStatementsIntoMemberDeclaration(
                                propertyOrIndexer.GetMethod.ToFullRef(),
                                getEntryStatements,
                                getExitStatements,
                                injectedNode );
                        }

                        if ( propertyOrIndexer.SetMethod != null )
                        {
                            var setEntryStatements = this._transformationCollection.GetInjectedEntryStatements(
                                propertyOrIndexer.SetMethod.ToRef(),
                                propertyOrIndexerRef,
                                injectedMember );

                            var setExitStatements = this._transformationCollection.GetInjectedExitStatements(
                                propertyOrIndexer.SetMethod.ToRef(),
                                propertyOrIndexerRef,
                                injectedMember );

                            injectedNode = InjectStatementsIntoMemberDeclaration(
                                propertyOrIndexer.SetMethod.ToFullRef(),
                                setEntryStatements,
                                setExitStatements,
                                injectedNode );
                        }

                        break;
                }

                injectedNode = injectedNode
                    .WithOptionalLeadingTrivia( syntaxGenerationContext.TwoElasticEndOfLinesTriviaList, syntaxGenerationContext.Options )
                    .WithGeneratedCodeAnnotation(
                        injectedMember.Transformation?.AspectInstance.AspectClass.GeneratedCodeAnnotation
                        ?? FormattingAnnotations.SystemGeneratedCodeAnnotation );

                switch ( injectedNode )
                {
                    case ConstructorDeclarationSyntax constructorDeclaration:
                        {
                            if ( injectedMember.BuilderData != null &&
                                 this._transformationCollection.TryGetMemberLevelTransformations(
                                     injectedMember.BuilderData.AssertNotNull(),
                                     out var memberLevelTransformations ) )
                            {
                                injectedNode = this.ApplyMemberLevelTransformations(
                                    constructorDeclaration,
                                    memberLevelTransformations,
                                    syntaxGenerationContext );
                            }

                            break;
                        }

                    case PropertyDeclarationSyntax propertyDeclaration:
                        if ( injectedMember.BuilderData is PropertyBuilderData propertyBuilder
                             && this._transformationCollection.IsAutoPropertyWithSynthesizedSetter( propertyBuilder ) )
                        {
                            switch ( injectedMember )
                            {
                                // ReSharper disable once MissingIndent
                                case
                                {
                                    Semantic: InjectedMemberSemantic.Introduction, Kind: DeclarationKind.Property,
                                    Syntax: PropertyDeclarationSyntax
                                }:
                                    injectedNode = propertyDeclaration.WithSynthesizedSetter( syntaxGenerationContext );

                                    break;

                                case { Semantic: InjectedMemberSemantic.InitializerMethod }:
                                    break;

                                default:
                                    throw new AssertionFailedException( $"Unexpected semantic for '{propertyBuilder}'." );
                            }
                        }

                        break;

                    case TypeDeclarationSyntax typeDeclaration:

                        var typeBuilder = (NamedTypeBuilderData) injectedMember.BuilderData.AssertNotNull();
                        var injectedTypeMembers = new List<MemberDeclarationSyntax>();

                        this.AddInjectionsOnPosition(
                            new InsertPosition( InsertPositionRelation.Within, typeBuilder ),
                            originalSyntaxTree,
                            injectedTypeMembers,
                            syntaxGenerationContext );

                        typeDeclaration = typeDeclaration.WithMembers( typeDeclaration.Members.AddRange( injectedTypeMembers ) );
                        injectedNode = AddInjectedInterfaces( typeBuilder, typeDeclaration );

                        break;

                    case NamespaceDeclarationSyntax namespaceDeclaration:
                        // This handles named types injected into a namespace.

                        var namespaceTypeBuilder = (NamedTypeBuilderData) injectedMember.BuilderData.AssertNotNull();
                        var injectedNamedTypeMembers = new List<MemberDeclarationSyntax>();

                        this.AddInjectionsOnPosition(
                            new InsertPosition( InsertPositionRelation.Within, namespaceTypeBuilder ),
                            originalSyntaxTree,
                            injectedNamedTypeMembers,
                            syntaxGenerationContext );

                        var namespaceTypeDeclaration = (TypeDeclarationSyntax) namespaceDeclaration.Members.Single();

                        namespaceTypeDeclaration =
                            namespaceTypeDeclaration.WithMembers( namespaceTypeDeclaration.Members.AddRange( injectedNamedTypeMembers ) );

                        namespaceTypeDeclaration = AddInjectedInterfaces( namespaceTypeBuilder, namespaceTypeDeclaration );

                        injectedNode = namespaceDeclaration.WithMembers( SingletonList<MemberDeclarationSyntax>( namespaceTypeDeclaration ) );

                        break;
                }

                targetList.Add( (T) injectedNode );

                TypeDeclarationSyntax AddInjectedInterfaces( NamedTypeBuilderData typeBuilder, TypeDeclarationSyntax typeDeclaration )
                {
                    var injectedInterfaces = this._transformationCollection.GetIntroducedInterfacesForTypeBuilder( typeBuilder );

                    if ( injectedInterfaces.Count > 0 )
                    {
                        return (TypeDeclarationSyntax) typeDeclaration.AddBaseListTypes( injectedInterfaces.SelectAsArray( i => i.Syntax ) );
                    }
                    else
                    {
                        return typeDeclaration;
                    }
                }
            }
        }

        private static MemberDeclarationSyntax InjectStatementsIntoMemberDeclaration(
            IFullRef<IMember> contextDeclaration,
            IReadOnlyList<StatementSyntax> entryStatements,
            IReadOnlyList<StatementSyntax> exitStatements,
            MemberDeclarationSyntax currentNode )
        {
            if ( entryStatements.Count == 0 && exitStatements.Count == 0 )
            {
                return currentNode;
            }

            switch ( currentNode )
            {
                case ConstructorDeclarationSyntax { Body: { } body } constructor:
                    return constructor.WithBody( ReplaceBlock( contextDeclaration, entryStatements, exitStatements, body ) );

                case ConstructorDeclarationSyntax { ExpressionBody: { } expressionBody } constructor:
                    return
                        constructor.PartialUpdate(
                            expressionBody: null,
                            semicolonToken: default(SyntaxToken),
                            body: ReplaceExpression( entryStatements, exitStatements, expressionBody.Expression, true ) );

                // Static constructor overrides also go here.
                case MethodDeclarationSyntax { Body: { } body } method:
                    return method.WithBody( ReplaceBlock( contextDeclaration, entryStatements, exitStatements, body ) );

                case MethodDeclarationSyntax { ExpressionBody: { } expressionBody } method:
                    var returnsVoid =
                        contextDeclaration switch
                        {
                            IFullRef<IConstructor> => true,
                            IFullRef<IMethod> methodRef => methodRef.DoReturnStatementsRequireArgument(),
                            _ => throw new InvalidOperationException( $"Not supported: {contextDeclaration}" )
                        };

                    return
                        method.PartialUpdate(
                            expressionBody: null,
                            semicolonToken: default(SyntaxToken),
                            body: ReplaceExpression( entryStatements, exitStatements, expressionBody.Expression, returnsVoid ) );

                case MethodDeclarationSyntax { Body: null, ExpressionBody: null } method:
                    Invariant.Assert( method.Modifiers.All( m => !m.IsKind( SyntaxKind.AbstractKeyword ) && !m.IsKind( SyntaxKind.ExternKeyword ) ) );

                    return method.PartialUpdate(
                        body: Block( entryStatements.Concat( exitStatements ) ),
                        semicolonToken: default(SyntaxToken) );

                case OperatorDeclarationSyntax { Body: { } body } @operator:
                    return @operator.WithBody( ReplaceBlock( contextDeclaration, entryStatements, exitStatements, body ) );

                case OperatorDeclarationSyntax { ExpressionBody: { } expressionBody } @operator:
                    return
                        @operator.PartialUpdate(
                            expressionBody: null,
                            semicolonToken: default(SyntaxToken),
                            body: ReplaceExpression( entryStatements, exitStatements, expressionBody.Expression, false ) );

                case PropertyDeclarationSyntax { ExpressionBody: { } expressionBody } property:
                    {
                        Invariant.Assert( contextDeclaration is IFullRef<IMethod> { Definition.MethodKind: MethodKind.PropertyGet } );

                        return
                            property.PartialUpdate(
                                expressionBody: null,
                                semicolonToken: default(SyntaxToken),
                                accessorList: AccessorList(
                                    SingletonList(
                                        AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            ReplaceExpression( entryStatements, exitStatements, expressionBody.Expression, false ) ) ) ) );
                    }

                case IndexerDeclarationSyntax { ExpressionBody: { } expressionBody } indexer:
                    {
                        Invariant.Assert( contextDeclaration is IFullRef<IMethod> { Definition.MethodKind: MethodKind.PropertyGet } );

                        return
                            indexer.PartialUpdate(
                                expressionBody: null,
                                semicolonToken: default(SyntaxToken),
                                accessorList: AccessorList(
                                    SingletonList(
                                        AccessorDeclaration(
                                            SyntaxKind.GetAccessorDeclaration,
                                            ReplaceExpression( entryStatements, exitStatements, expressionBody.Expression, false ) ) ) ) );
                    }

                case BasePropertyDeclarationSyntax { AccessorList: { } accessorList } propertyOrIndexer:
                    {
                        var methodRef = contextDeclaration.As<IMethod>();
                        var methodKind = methodRef.Definition.MethodKind;
                        Invariant.Assert( methodKind is MethodKind.PropertyGet or MethodKind.PropertySet );

                        return
                            propertyOrIndexer.WithAccessorList(
                                accessorList.WithAccessors(
                                    List(
                                        accessorList.Accessors.SelectAsArray(
                                            a =>
                                                IsMatchingAccessor( a, methodKind )
                                                    ? a switch
                                                    {
                                                        { Body: { } body } => a.WithBody(
                                                            ReplaceBlock( contextDeclaration, entryStatements, exitStatements, body ) ),
                                                        { ExpressionBody: { } expressionBody } =>
                                                            a.PartialUpdate(
                                                                expressionBody: null,
                                                                semicolonToken: default(SyntaxToken),
                                                                body: ReplaceExpression(
                                                                    entryStatements,
                                                                    exitStatements,
                                                                    expressionBody.Expression,
                                                                    a.Kind() is not SyntaxKind.GetAccessorDeclaration ) ),
                                                        _ => throw new AssertionFailedException( $"Not supported: {a}" )
                                                    }
                                                    : a ) ) ) );
                    }

                    static bool IsMatchingAccessor( AccessorDeclarationSyntax accessorDeclaration, MethodKind methodKind )
                    {
                        return (accessorDeclaration.Kind(), methodKind) switch
                        {
                            (SyntaxKind.GetAccessorDeclaration, MethodKind.PropertyGet) => true,
                            (SyntaxKind.SetAccessorDeclaration, MethodKind.PropertySet) => true,
                            (SyntaxKind.InitAccessorDeclaration, MethodKind.PropertySet) => true,
                            _ => false
                        };
                    }

                default:
                    throw new AssertionFailedException( $"Not supported: {currentNode}" );
            }

            static BlockSyntax ReplaceBlock(
                IRef<IDeclaration> declaration,
                IReadOnlyList<StatementSyntax> entryStatements,
                IReadOnlyList<StatementSyntax> exitStatements,
                BlockSyntax targetBlock )
            {
                if ( exitStatements.Count > 0 )
                {
                    // Patterns recognizing bodies generated by AuxiliaryMemberFactory.
                    switch ( targetBlock )
                    {
                        case { Statements: [ExpressionStatementSyntax expressionStatement] }:
                            return
                                Block(
                                    Block( List( entryStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    expressionStatement,
                                    Block( List( exitStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ) );

                        case { Statements: [LocalDeclarationStatementSyntax localDeclarationStatement, ReturnStatementSyntax returnStatement] }:
                            return
                                Block(
                                    Block( List( entryStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    localDeclarationStatement,
                                    Block( List( exitStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    returnStatement );

                        case { Statements: [LocalDeclarationStatementSyntax localDeclarationStatement, ForEachStatementSyntax foreachStatement] }:
                            return
                                Block(
                                    Block( List( entryStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    localDeclarationStatement,
                                    Block( List( exitStatements ) ).WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    foreachStatement );

                        case
                        {
                            Statements:
                            [
                                LocalDeclarationStatementSyntax bufferedEnumerableLocal, LocalDeclarationStatementSyntax returnValueLocal,
                                WhileStatementSyntax whileStatement
                            ]
                        }:
                            return
                                Block(
                                    Block( List( entryStatements ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    bufferedEnumerableLocal,
                                    returnValueLocal,
                                    Block(
                                            List(
                                                exitStatements.Select(
                                                    ( s, i ) =>
                                                    {
                                                        if ( i == 0 )
                                                        {
                                                            return s;
                                                        }
                                                        else
                                                        {
                                                            var declarator = returnValueLocal.Declaration.Variables.Single();

                                                            return
                                                                Block(
                                                                        ExpressionStatement(
                                                                            AssignmentExpression(
                                                                                SyntaxKind.SimpleAssignmentExpression,
                                                                                IdentifierName( declarator.Identifier.ValueText ),
                                                                                declarator.Initializer.AssertNotNull().Value ) ),
                                                                        s )
                                                                    .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                                                        }
                                                    } ) ) )
                                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                                    whileStatement );

                        default:
                            throw new AssertionFailedException( $"Unsupported form of body with exit statements for: {declaration}" );
                    }
                }
                else
                {
                    return
                        Block(
                            Block( List( entryStatements ) )
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                            targetBlock
                                .WithSourceCodeAnnotationIfNotGenerated()
                                .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ) );
                }
            }

            static BlockSyntax ReplaceExpression(
                IReadOnlyList<StatementSyntax> entryStatements,
                IReadOnlyList<StatementSyntax> exitStatements,
                ExpressionSyntax targetExpression,
                bool returnsVoid )
            {
                // Auxiliary bodies that may receive exit statements are never expression bodies.
                Invariant.Assert( exitStatements.Count == 0 );

                StatementSyntax statement =
                    targetExpression switch
                    {
                        ThrowExpressionSyntax throwExpression =>
                            ThrowStatement(
                                    throwExpression.ThrowKeyword,
                                    throwExpression.Expression,
                                    Token( SyntaxKind.SemicolonToken ) )
                                .WithSourceCodeAnnotationIfNotGenerated(),
                        _ =>
                            returnsVoid
                                ? ExpressionStatement( targetExpression.WithSourceCodeAnnotationIfNotGenerated() )
                                : ReturnStatement(
                                    Token( default, SyntaxKind.ReturnKeyword, TriviaList( ElasticSpace ) ),
                                    targetExpression.WithSourceCodeAnnotationIfNotGenerated(),
                                    Token( SyntaxKind.SemicolonToken ) )
                    };

                return
                    Block(
                        Block( List( entryStatements ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock ),
                        statement );
            }
        }

        private IReadOnlyList<MemberDeclarationSyntax> VisitMember( MemberDeclarationSyntax member )
        {
            static MemberDeclarationSyntax[] Singleton( MemberDeclarationSyntax m )
            {
                return [m];
            }

            return member switch
            {
                ConstructorDeclarationSyntax constructor => Singleton( this.VisitConstructorDeclarationCore( constructor ) ),
                MethodDeclarationSyntax method => Singleton( this.VisitMethodDeclarationCore( method ) ),
                PropertyDeclarationSyntax property => Singleton( this.VisitPropertyDeclarationCore( property ) ),
                IndexerDeclarationSyntax indexer => Singleton( this.VisitIndexerDeclarationCore( indexer ) ),
                OperatorDeclarationSyntax @operator => Singleton( this.VisitOperatorDeclarationCore( @operator ) ),
                EventDeclarationSyntax @event => Singleton( this.VisitEventDeclarationCore( @event ) ),
                FieldDeclarationSyntax field => this.VisitFieldDeclarationCore( field ),
                EventFieldDeclarationSyntax @eventField => this.VisitEventFieldDeclarationCore( @eventField ),
                _ => Singleton( (MemberDeclarationSyntax) this.Visit( member )! )
            };
        }

        private ConstructorDeclarationSyntax ApplyMemberLevelTransformations(
            ConstructorDeclarationSyntax constructorDeclaration,
            MemberLevelTransformations memberLevelTransformations,
            SyntaxGenerationContext syntaxGenerationContext )
        {
            constructorDeclaration = constructorDeclaration.WithParameterList(
                AppendParameters( constructorDeclaration.ParameterList, memberLevelTransformations.Parameters, syntaxGenerationContext ) );

            constructorDeclaration = constructorDeclaration.WithInitializer(
                this.AppendInitializerArguments( constructorDeclaration.Initializer, memberLevelTransformations.Arguments ) );

            return constructorDeclaration;
        }

        private void ApplyMemberLevelTransformationsToPrimaryConstructor(
            TypeDeclarationSyntax typeDeclaration,
            SyntaxGenerationContext syntaxGenerationContext,
            ref BaseListSyntax? baseList,
            ref ParameterListSyntax? parameterList )
        {
            if ( !this._transformationCollection.TryGetMemberLevelTransformations( typeDeclaration, out var memberLevelTransformations ) )
            {
                return;
            }

            Invariant.AssertNot( typeDeclaration.BaseList == null && memberLevelTransformations.Arguments.Length > 0 );
            Invariant.AssertNot( typeDeclaration.GetParameterList() == null );

            parameterList = AppendParameters( typeDeclaration.GetParameterList()!, memberLevelTransformations.Parameters, syntaxGenerationContext );
            baseList = typeDeclaration.BaseList;

            if ( memberLevelTransformations.Arguments.Length > 0 )
            {
                var baseTypeSyntax = typeDeclaration.BaseList.AssertNotNull().Types[0];

                BaseTypeSyntax newBaseTypeSyntax;

                switch ( baseTypeSyntax )
                {
                    case SimpleBaseTypeSyntax simpleBaseType:
                        newBaseTypeSyntax =
                            PrimaryConstructorBaseType(
                                simpleBaseType.Type,
                                ArgumentList( SeparatedList( memberLevelTransformations.Arguments.Select( x => x.ToSyntax() ) ) ) );

                        break;

                    case PrimaryConstructorBaseTypeSyntax primaryCtorBaseType:
                        newBaseTypeSyntax =
                            primaryCtorBaseType
                                .WithArgumentList(
                                    primaryCtorBaseType.ArgumentList.AddArguments( memberLevelTransformations.Arguments.SelectAsArray( x => x.ToSyntax() ) ) );

                        break;

                    default:
                        throw new AssertionFailedException( $"Unexpected base type: {baseTypeSyntax}" );
                }

                // TODO: This may be slower than replacing specific index.
                baseList = typeDeclaration.BaseList.ReplaceNode( baseTypeSyntax, newBaseTypeSyntax );
            }
        }

        private static ParameterListSyntax AppendParameters(
            ParameterListSyntax existingParameters,
            ImmutableArray<IntroduceParameterTransformation> newParameters,
            SyntaxGenerationContext syntaxGenerationContext )
        {
            if ( newParameters.IsEmpty )
            {
                return existingParameters;
            }
            else
            {
                if ( existingParameters.Parameters.Any( p => p.Modifiers.Any( m => m.IsKind( SyntaxKind.ParamsKeyword ) ) ) )
                {
                    return existingParameters.WithParameters(
                        existingParameters.Parameters.InsertRange(
                            existingParameters.Parameters.Count - 1,
                            newParameters.Select(
                                x => x.ToSyntax( syntaxGenerationContext )
                                    .WithOptionalTrailingTrivia( ElasticSpace, syntaxGenerationContext.Options ) ) ) );
                }
                else
                {
                    return existingParameters.WithParameters(
                        existingParameters.Parameters.AddRange(
                            newParameters.Select(
                                x => x.ToSyntax( syntaxGenerationContext )
                                    .WithOptionalTrailingTrivia( ElasticSpace, syntaxGenerationContext.Options ) ) ) );
                }
            }
        }

        private ConstructorInitializerSyntax? AppendInitializerArguments(
            ConstructorInitializerSyntax? initializerSyntax,
            ImmutableArray<IntroduceConstructorInitializerArgumentTransformation> newArguments )
        {
            if ( newArguments.IsEmpty )
            {
                return initializerSyntax;
            }

            var newArgumentsSyntax = newArguments.Select( a => a.ToSyntax().WithOptionalTrailingTrivia( ElasticSpace, this.SyntaxGenerationOptions ) );

            if ( initializerSyntax == null )
            {
                return ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    ArgumentList( SeparatedList( newArgumentsSyntax ) ) );
            }
            else
            {
                return initializerSyntax.WithArgumentList( initializerSyntax.ArgumentList.AddArguments( newArgumentsSyntax.ToArray() ) );
            }
        }

        private IReadOnlyList<FieldDeclarationSyntax> VisitFieldDeclarationCore( FieldDeclarationSyntax node )
        {
            var originalNode = node;
            var context = this.GetSyntaxGenerationContext( node );

            // Rewrite attributes.
            if ( originalNode.Declaration.Variables.Count > 1
                 && originalNode.Declaration.Variables.Any( this._transformationCollection.IsNodeWithModifiedAttributes ) )
            {
                // TODO: This needs to use rewritten variable declaration or do removal in place.
                var members = new List<FieldDeclarationSyntax>( originalNode.Declaration.Variables.Count );

                // If we have changes in attributes and several members, we have to split them.
                foreach ( var variable in originalNode.Declaration.Variables )
                {
                    if ( this._transformationCollection.IsRemovedSyntax( variable ) )
                    {
                        continue;
                    }

                    var declaration = VariableDeclaration( node.Declaration.Type, SingletonSeparatedList( variable ) );
                    var attributes = this.RewriteDeclarationAttributeLists( variable, originalNode.AttributeLists, originalNode );

                    var fieldDeclaration = FieldDeclaration(
                        default,
                        node.Modifiers,
                        declaration,
                        Token( default, SyntaxKind.SemicolonToken, context.ElasticEndOfLineTriviaList ) );

                    fieldDeclaration = this.ReplaceAttributes( fieldDeclaration, attributes );

                    members.Add( fieldDeclaration );
                }

                return members;
            }
            else
            {
                var rewrittenAttributes = this.RewriteDeclarationAttributeLists(
                    originalNode.Declaration.Variables[0],
                    originalNode.AttributeLists,
                    originalNode );

                node = this.ReplaceAttributes( node, rewrittenAttributes );

                var anyChangeToVariables = false;
                var rewrittenVariables = new List<VariableDeclaratorSyntax>();

                foreach ( var variable in originalNode.Declaration.Variables )
                {
                    if ( this._transformationCollection.IsRemovedSyntax( variable ) )
                    {
                        anyChangeToVariables = true;

                        continue;
                    }

                    rewrittenVariables.Add( variable );
                }

                if ( anyChangeToVariables )
                {
                    if ( rewrittenVariables.Count > 0 )
                    {
                        return [node.WithDeclaration( node.Declaration.WithVariables( SeparatedList( rewrittenVariables ) ) )];
                    }
                    else
                    {
                        return Array.Empty<FieldDeclarationSyntax>();
                    }
                }
                else
                {
                    return [node];
                }
            }
        }

        private EnumMemberDeclarationSyntax VisitEnumMemberDeclarationCore( EnumMemberDeclarationSyntax node )
        {
            var originalNode = node;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private ConstructorDeclarationSyntax VisitConstructorDeclarationCore( ConstructorDeclarationSyntax node )
        {
            var originalNode = node;

            node = (ConstructorDeclarationSyntax) this.VisitConstructorDeclaration( node )!;

            if ( this._transformationCollection.TryGetMemberLevelTransformations( node, out var memberLevelTransformations ) )
            {
                var syntaxGenerationContext = this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, node );
                node = this.ApplyMemberLevelTransformations( node, memberLevelTransformations, syntaxGenerationContext );
            }

            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalNode );

            if ( symbol != null )
            {
                var constructor = this._compilation.RefFactory.FromSymbol<IConstructor>( symbol );
                var entryStatements = this._transformationCollection.GetInjectedEntryStatements( constructor );

                node = (ConstructorDeclarationSyntax) InjectStatementsIntoMemberDeclaration(
                    constructor,
                    entryStatements,
                    Array.Empty<StatementSyntax>(),
                    node );
            }

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private MethodDeclarationSyntax VisitMethodDeclarationCore( MethodDeclarationSyntax node )
        {
            var originalNode = node;

            node = (MethodDeclarationSyntax) this.VisitMethodDeclaration( node )!;

            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalNode );

            if ( symbol is { PartialImplementationPart: null } )
            {
                var method = this._compilation.RefFactory.FromSymbol<IMethod>( symbol );
                var entryStatements = this._transformationCollection.GetInjectedEntryStatements( method );

                node = (MethodDeclarationSyntax) InjectStatementsIntoMemberDeclaration( method, entryStatements, [], node );
            }

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private OperatorDeclarationSyntax VisitOperatorDeclarationCore( OperatorDeclarationSyntax node )
        {
            var originalNode = node;

            node = (OperatorDeclarationSyntax) this.VisitOperatorDeclaration( node )!;

            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalNode );

            if ( symbol != null )
            {
                var method = this._compilation.RefFactory.FromSymbol<IMethod>( symbol );
                var entryStatements = this._transformationCollection.GetInjectedEntryStatements( method );

                node = (OperatorDeclarationSyntax) InjectStatementsIntoMemberDeclaration( method, entryStatements, Array.Empty<StatementSyntax>(), node );
            }

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        public override SyntaxNode VisitParameter( ParameterSyntax node )
        {
            var originalNode = node;
            node = (ParameterSyntax) base.VisitParameter( node )!;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );

            if ( rewrittenAttributes is var (attributes, trivia) )
            {
                if ( trivia.ShouldBePreserved( this.SyntaxGenerationOptions ) )
                {
                    node = node.WithAttributeLists( default ).WithRequiredLeadingTrivia( trivia ).WithAttributeLists( attributes );
                }
                else
                {
                    node = node.WithAttributeLists( attributes );
                }
            }

            return node;
        }

        public override SyntaxNode VisitTypeParameter( TypeParameterSyntax node )
        {
            var originalNode = node;
            node = (TypeParameterSyntax) base.VisitTypeParameter( node )!;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );

            if ( rewrittenAttributes is var (attributes, trivia) )
            {
                if ( trivia.ShouldBePreserved( this.SyntaxGenerationOptions ) )
                {
                    node = node.WithAttributeLists( default ).WithRequiredLeadingTrivia( trivia ).WithAttributeLists( attributes );
                }
                else
                {
                    node = node.WithAttributeLists( attributes );
                }
            }

            return node;
        }

        private PropertyDeclarationSyntax VisitPropertyDeclarationCore( PropertyDeclarationSyntax node )
        {
            var originalNode = node;

            node = (PropertyDeclarationSyntax) this.VisitPropertyDeclaration( node )!;

            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalNode ).AssertSymbolNotNull();
            var property = this._compilation.RefFactory.FromSymbol<IProperty>( symbol );

            if ( symbol is
                {
                    SetMethod: { } setMethodSymbol,
#if ROSLYN_4_12_0_OR_GREATER
                    PartialImplementationPart: null
#endif
                } )
            {
                var setter = this._compilation.RefFactory.FromSymbol<IMethod>( setMethodSymbol );

                var entryStatements = this._transformationCollection.GetInjectedEntryStatements( setter, property );

                node = (PropertyDeclarationSyntax) InjectStatementsIntoMemberDeclaration(
                    setter,
                    entryStatements,
                    [],
                    node );
            }

            if ( this._transformationCollection.IsAutoPropertyWithSynthesizedSetter( originalNode ) )
            {
                node = node.WithSynthesizedSetter( this.GetSyntaxGenerationContext( originalNode ) );
            }

            if ( this._transformationCollection.GetAdditionalDeclarationFlags( originalNode ) is not AspectLinkerDeclarationFlags.None and var flags )
            {
                var existingFlags = node.GetLinkerDeclarationFlags();
                node = node.WithLinkerDeclarationFlags( existingFlags | flags );
            }

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private IndexerDeclarationSyntax VisitIndexerDeclarationCore( IndexerDeclarationSyntax node )
        {
            var originalNode = node;

            node = (IndexerDeclarationSyntax) this.VisitIndexerDeclaration( node )!;

            var semanticModel = this._semanticModelProvider.GetSemanticModel( originalNode.SyntaxTree );
            var symbol = semanticModel.GetDeclaredSymbol( originalNode );

            if ( symbol is
                {
#if ROSLYN_4_12_0_OR_GREATER
                    PartialImplementationPart: null
#endif
                } )
            {
                var indexer = this.RefFactory.FromSymbol<IIndexer>( symbol );

                if ( symbol.GetMethod != null )
                {
                    var getter = this.RefFactory.FromSymbol<IMethod>( symbol.GetMethod );
                    var entryStatements = this._transformationCollection.GetInjectedEntryStatements( getter, indexer );

                    node = (IndexerDeclarationSyntax) InjectStatementsIntoMemberDeclaration(
                        getter,
                        entryStatements,
                        [],
                        node );
                }

                if ( symbol.SetMethod != null )
                {
                    var setter = this._compilation.RefFactory.FromSymbol<IMethod>( symbol.SetMethod );
                    var entryStatements = this._transformationCollection.GetInjectedEntryStatements( setter, indexer );

                    node = (IndexerDeclarationSyntax) InjectStatementsIntoMemberDeclaration(
                        setter,
                        entryStatements,
                        [],
                        node );
                }
            }

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        public override SyntaxNode VisitAccessorDeclaration( AccessorDeclarationSyntax node )
        {
            var originalNode = node;
            node = (AccessorDeclarationSyntax) base.VisitAccessorDeclaration( node )!;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );

            if ( rewrittenAttributes is var (attributes, trivia) )
            {
                if ( trivia.ShouldBePreserved( this.SyntaxGenerationOptions ) )
                {
                    node = node.WithAttributeLists( default ).WithRequiredLeadingTrivia( trivia ).WithAttributeLists( attributes );
                }
                else
                {
                    node = node.WithAttributeLists( attributes );
                }
            }

            return node;
        }

        private EventDeclarationSyntax VisitEventDeclarationCore( EventDeclarationSyntax node )
        {
            var originalNode = node;
            node = (EventDeclarationSyntax) this.VisitEventDeclaration( node )!;

            // Rewrite attributes.
            var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode, originalNode.AttributeLists );
            node = this.ReplaceAttributes( node, rewrittenAttributes );

            return node;
        }

        private IReadOnlyList<MemberDeclarationSyntax> VisitEventFieldDeclarationCore( EventFieldDeclarationSyntax node )
        {
            var context = this.GetSyntaxGenerationContext( node );
            var originalNode = node;

            // Rewrite attributes.
            if ( originalNode.Declaration.Variables.Count > 1
                 && originalNode.Declaration.Variables.Any( this._transformationCollection.IsNodeWithModifiedAttributes ) )
            {
                var members = new List<MemberDeclarationSyntax>( originalNode.Declaration.Variables.Count );

                // If we have changes in attributes and several members, we have to split them.
                foreach ( var variable in originalNode.Declaration.Variables )
                {
                    var declaration = VariableDeclaration( node.Declaration.Type, SingletonSeparatedList( variable ) );

                    var attributes = this.RewriteDeclarationAttributeLists( variable, originalNode.AttributeLists, node );

                    var eventDeclaration = EventFieldDeclaration(
                        default,
                        node.Modifiers,
                        Token( TriviaList(), SyntaxKind.EventKeyword, TriviaList( Space ) ),
                        declaration,
                        Token( default, SyntaxKind.SemicolonToken, context.TwoElasticEndOfLinesTriviaList ) );

                    eventDeclaration = this.ReplaceAttributes( eventDeclaration, attributes );

                    members.Add( eventDeclaration );
                }

                return members;
            }
            else
            {
                var rewrittenAttributes = this.RewriteDeclarationAttributeLists( originalNode.Declaration.Variables[0], originalNode.AttributeLists, node );
                node = this.ReplaceAttributes( node, rewrittenAttributes );

                return [node];
            }
        }

        public override SyntaxNode VisitCompilationUnit( CompilationUnitSyntax node )
        {
            SyntaxGenerationContext? syntaxGenerationContext = null;
            List<AttributeListSyntax> outputLists = [];
            List<SyntaxTrivia> outputTrivias = [];

            this.RewriteAttributeLists(
                this._compilation.ToFullRef(),
                SyntaxKind.AssemblyKeyword,
                node,
                node.AttributeLists,
                ( _, n ) => n.SyntaxTree == this._syntaxTreeForGlobalAttributes,
                outputLists,
                outputTrivias,
                ref syntaxGenerationContext );

            var injections = new List<MemberDeclarationSyntax>();

            this.AddInjectionsOnPosition(
                new InsertPosition( node.SyntaxTree ),
                node.SyntaxTree,
                injections,
                this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, false, false, "\n" ) );

            if ( injections.Count > 0 )
            {
                return
                    ((CompilationUnitSyntax) base.VisitCompilationUnit( node )!)
                    .PartialUpdate(
                        attributeLists: List( outputLists ),
                        members: node.Members.AddRange( injections ) );
            }
            else
            {
                return ((CompilationUnitSyntax) base.VisitCompilationUnit( node )!).WithAttributeLists( List( outputLists ) );
            }
        }

        public override SyntaxNode VisitNamespaceDeclaration( NamespaceDeclarationSyntax node )
        {
            var injections = new List<MemberDeclarationSyntax>();

            this.AddInjectionsOnPosition(
                new InsertPosition( InsertPositionRelation.Within, node ),
                node.SyntaxTree,
                injections,
                this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, false, false, "\n" ) );

            if ( injections.Count > 0 )
            {
                return ((NamespaceDeclarationSyntax) base.VisitNamespaceDeclaration( node )!).WithMembers( node.Members.AddRange( injections ) );
            }
            else
            {
                return (NamespaceDeclarationSyntax) base.VisitNamespaceDeclaration( node )!;
            }
        }

        public override SyntaxNode VisitFileScopedNamespaceDeclaration( FileScopedNamespaceDeclarationSyntax node )
        {
            var injections = new List<MemberDeclarationSyntax>();

            this.AddInjectionsOnPosition(
                new InsertPosition( InsertPositionRelation.Within, node ),
                node.SyntaxTree,
                injections,
                this.CompilationContext.GetSyntaxGenerationContext( this.SyntaxGenerationOptions, false, false, "\n" ) );

            if ( injections.Count > 0 )
            {
                return ((FileScopedNamespaceDeclarationSyntax) base.VisitFileScopedNamespaceDeclaration( node )!).WithMembers(
                    node.Members.AddRange( injections ) );
            }
            else
            {
                return (FileScopedNamespaceDeclarationSyntax) base.VisitFileScopedNamespaceDeclaration( node )!;
            }
        }
    }
}