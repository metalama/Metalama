// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.Introductions.BuilderData;
using Metalama.Framework.Engine.CodeModel.Introductions.Builders;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SerializableIds;
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
using Metalama.Framework.Code.Comparers;

// ReSharper disable SuspiciousTypeConversion.Global

namespace Metalama.Framework.Tests.LinkerTests.Runner
{
    internal partial class LinkerTestInputBuilder
    {
        private sealed class TestTypeRewriter : SafeSyntaxRewriter
        {
            private readonly TestRewriter _owner;
            private readonly Stack<(TypeDeclarationSyntax Type, List<MemberDeclarationSyntax> Members)> _currentTypeStack;
            private InsertPositionRecord? _currentInsertPosition;
            private readonly Dictionary<IFullRef<IField>, IFullRef<IProperty>> _promotions;

            public TestTypeRewriter( TestRewriter owner )
            {
                this._owner = owner;
                this._currentTypeStack = new Stack<(TypeDeclarationSyntax, List<MemberDeclarationSyntax>)>();
                this._promotions = new Dictionary<IFullRef<IField>, IFullRef<IProperty>>( RefEqualityComparer.Default );
            }
            
            public override SyntaxNode? VisitClassDeclaration( ClassDeclarationSyntax node )
            {
                var rewrittenNode = this.RewriteTypeDeclaration( node, n => base.VisitClassDeclaration( n ), ( n, m ) => n.WithMembers( List( m ) ) );

                if ( this._currentTypeStack.Count > 0 )
                {
                    this._currentTypeStack.Peek().Members.Add( rewrittenNode );

                    return null;
                }
                else
                {
                    return rewrittenNode;
                }
            }

            public override SyntaxNode? VisitRecordDeclaration( RecordDeclarationSyntax node )
            {
                var rewrittenNode = this.RewriteTypeDeclaration( node, n => base.VisitRecordDeclaration( n ), ( n, m ) => n.WithMembers( List( m ) ) );

                if ( this._currentTypeStack.Count > 0 )
                {
                    this._currentTypeStack.Peek().Members.Add( rewrittenNode );

                    return null;
                }
                else
                {
                    return rewrittenNode;
                }
            }

            public override SyntaxNode? VisitStructDeclaration( StructDeclarationSyntax node )
            {
                var rewrittenNode = this.RewriteTypeDeclaration( node, n => base.VisitStructDeclaration( n ), ( n, m ) => n.WithMembers( List( m ) ) );

                if ( this._currentTypeStack.Count > 0 )
                {
                    this._currentTypeStack.Peek().Members.Add( rewrittenNode );

                    return null;
                }
                else
                {
                    return rewrittenNode;
                }
            }

            private T RewriteTypeDeclaration<T>( T node, Action<T> visitAction, Func<T, List<MemberDeclarationSyntax>, T> rewriteFunc )
                where T : TypeDeclarationSyntax
            {
                var nodeId = AllocateNodeId();

                var newMemberList = new List<MemberDeclarationSyntax>();

                this._currentTypeStack.Push( (node, newMemberList) );
                this._currentInsertPosition = new InsertPositionRecord( InsertPositionRelation.Within, nodeId );

                visitAction( node );
                var rewrittenNode = rewriteFunc( node, newMemberList );

                rewrittenNode = AssignNodeId( rewrittenNode, nodeId );

                this._currentTypeStack.Pop();

                this._currentInsertPosition = this._currentTypeStack.Count > 0 ? new InsertPositionRecord( InsertPositionRelation.After, nodeId ) : null;

                return rewrittenNode;
            }

            public override SyntaxNode? VisitMethodDeclaration( MethodDeclarationSyntax node )
            {
                this._currentTypeStack.Peek().Members.AddRange( this.RewriteMemberDeclaration( node ) );

                return null;
            }

            public override SyntaxNode? VisitPropertyDeclaration( PropertyDeclarationSyntax node )
            {
                this._currentTypeStack.Peek().Members.AddRange( this.RewriteMemberDeclaration( node ) );

                return null;
            }

            public override SyntaxNode? VisitEventDeclaration( EventDeclarationSyntax node )
            {
                this._currentTypeStack.Peek().Members.AddRange( this.RewriteMemberDeclaration( node ) );

                return null;
            }

            public override SyntaxNode? VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
            {
                this._currentTypeStack.Peek().Members.AddRange( this.RewriteMemberDeclaration( node ) );

                return null;
            }

            public override SyntaxNode? VisitFieldDeclaration( FieldDeclarationSyntax node )
            {
                this._currentTypeStack.Peek().Members.AddRange( this.RewriteMemberDeclaration( node ) );

                return null;
            }

            private IEnumerable<MemberDeclarationSyntax> RewriteMemberDeclaration<T>( T node )
                where T : MemberDeclarationSyntax
            {
                if ( HasPseudoAttribute( node ) )
                {
                    var semanticModel = this._owner.InputCompilation.SemanticModelProvider.GetSemanticModel( node.SyntaxTree );

                    var newMembers = this.ProcessPseudoAttributeNode( semanticModel, node );
                    var newMemberList = new List<MemberDeclarationSyntax>();

                    foreach ( var (newNode, isPseudoMember) in newMembers )
                    {
                        if ( !isPseudoMember )
                        {
                            var newNodeId = AllocateNodeId();
                            var newNodeWithId = AssignNodeId( newNode.AssertNotNull(), newNodeId );
                            newMemberList.Add( newNodeWithId );
                            this._currentInsertPosition = new InsertPositionRecord( InsertPositionRelation.After, newNodeId );
                        }
                        else
                        {
                            newMemberList.Add( newNode );
                        }
                    }

                    return newMemberList.ToArray();
                }

                // Non-pseudo nodes become the next insert positions.
                var nodeId = AllocateNodeId();
                node = AssignNodeId( node, nodeId );
                this._currentInsertPosition = new InsertPositionRecord( InsertPositionRelation.After, nodeId );

                return [node];
            }

            private static bool HasPseudoAttribute( MemberDeclarationSyntax node )
            {
                return node.AttributeLists.SelectMany( x => x.Attributes ).Any( x => x.Name.ToString().StartsWith( "Pseudo", StringComparison.Ordinal ) );
            }

            private IEnumerable<(MemberDeclarationSyntax Node, bool IsPseudoMember)> ProcessPseudoAttributeNode(
                SemanticModel semanticModel,
                MemberDeclarationSyntax node )
            {
                AttributeSyntax? pseudoIntroductionAttribute = null;
                AttributeSyntax? pseudoOverrideAttribute = null;
                AttributeSyntax? pseudoReplacedAttribute = null;
                AttributeSyntax? pseudoReplacementAttribute = null;

                var notInlineable = false;
                var notDiscardable = false;

                foreach ( var attributeList in node.AttributeLists )
                {
                    // First pass: option attributes
                    foreach ( var attribute in attributeList.Attributes )
                    {
                        var name = attribute.Name.ToString();

                        switch ( name )
                        {
                            case "PseudoNotInlineable":
                                notInlineable = true;

                                break;

                            case "PseudoNotDiscardable":
                                notDiscardable = true;

                                break;
                        }
                    }

                    // Second pass: transformation attributes
                    foreach ( var attribute in attributeList.Attributes )
                    {
                        var name = attribute.Name.ToString();

                        if ( string.Equals( name, "PseudoIntroduction", StringComparison.Ordinal ) )
                        {
                            pseudoIntroductionAttribute = attribute;
                        }
                        else if ( string.Equals( name, "PseudoReplaced", StringComparison.Ordinal ) )
                        {
                            pseudoReplacedAttribute = attribute;
                        }
                        else if ( string.Equals( name, "PseudoReplacement", StringComparison.Ordinal ) )
                        {
                            pseudoReplacementAttribute = attribute;
                        }
                        else if ( string.Equals( name, "PseudoOverride", StringComparison.Ordinal ) )
                        {
                            pseudoOverrideAttribute = attribute;
                        }
                        else if ( name.StartsWith( "Pseudo", StringComparison.Ordinal ) &&
                                  !string.Equals( name, "PseudoNotInlineable", StringComparison.Ordinal ) &&
                                  !string.Equals( name, "PseudoNotDiscardable", StringComparison.Ordinal ) )
                        {
                            throw new NotSupportedException( $"Unsupported pseudo attribute {name}" );
                        }
                    }
                }

                if ( pseudoIntroductionAttribute != null )
                {
                    // Introduction will create a temporary declaration, that will help us to provide values for IMethod member.
                    this.ProcessPseudoIntroduction( semanticModel, node, [], pseudoIntroductionAttribute, notInlineable, notDiscardable, pseudoReplacedAttribute, pseudoReplacementAttribute );

                    return [];
                }
                else if ( pseudoOverrideAttribute != null )
                {
                    Invariant.Assert( pseudoReplacedAttribute == null && pseudoReplacementAttribute == null );

                    this.ProcessPseudoOverride( semanticModel, node, [], pseudoOverrideAttribute, notInlineable, notDiscardable );

                    return [];
                }
                else if ( pseudoReplacedAttribute != null )
                {
                    return [(node.WithAttributeLists( List<AttributeListSyntax>() ), false)];
                }
                else
                {
                    var transformedNode = node.WithAttributeLists( List<AttributeListSyntax>() );

                    if ( notInlineable || notDiscardable )
                    {
                        var flags = AspectLinkerDeclarationFlags.None;

                        if ( notInlineable )
                        {
                            flags |= AspectLinkerDeclarationFlags.NotInlineable;
                        }

                        if ( notDiscardable )
                        {
                            flags |= AspectLinkerDeclarationFlags.NotDiscardable;
                        }

                        transformedNode = transformedNode.WithLinkerDeclarationFlags( flags );
                    }

                    return [(transformedNode, false)];
                }
            }

            private void ProcessPseudoIntroduction(
                SemanticModel semanticModel,
                MemberDeclarationSyntax node,
                IEnumerable<AttributeListSyntax> newAttributeLists,
                AttributeSyntax introductionAttribute,
                bool notInlineable,
                bool notDiscardable,
                AttributeSyntax? replacedAttribute,
                AttributeSyntax? replacementAttribute )
            {
                if ( introductionAttribute.ArgumentList == null || introductionAttribute.ArgumentList.Arguments.Count is < 1 or > 3 )
                {
                    throw new ArgumentException( "PseudoIntroduction should have 1 or 2 arguments - aspect name and optionally layer name." );
                }

                var symbol = node switch
                {
                    FieldDeclarationSyntax { Declaration.Variables: [{ } variable] } => semanticModel.GetDeclaredSymbol( variable ).AssertNotNull(),
                    EventFieldDeclarationSyntax { Declaration.Variables: [{ } variable] } => semanticModel.GetDeclaredSymbol( variable ).AssertNotNull(),
                    _ => semanticModel.GetDeclaredSymbol( node ).AssertNotNull(),
                };

                var aspectName = introductionAttribute.ArgumentList.Arguments[0].ToString().Trim( '\"' );

                string? layerName = null;

                if ( introductionAttribute.ArgumentList.Arguments.Count == 2 )
                {
                    layerName = introductionAttribute.ArgumentList.Arguments[1].ToString().Trim( '\"' );
                }

                var introducedElementName = node switch
                {
                    MethodDeclarationSyntax method => method.Identifier.ValueText,
                    PropertyDeclarationSyntax property => property.Identifier.ValueText,
                    EventDeclarationSyntax @event => @event.Identifier.ValueText,
                    EventFieldDeclarationSyntax eventField => eventField.Declaration.Variables.Single().Identifier.ValueText,
                    FieldDeclarationSyntax field => field.Declaration.Variables.Single().Identifier.ValueText,
                    _ => throw new NotSupportedException()
                };

                var introductionSyntax = node.WithAttributeLists( List( newAttributeLists ) );

                if ( replacementAttribute != null )
                {
                    Invariant.Assert( replacedAttribute == null );
                    Invariant.Assert( replacementAttribute.ArgumentList?.Arguments.Count == 1 );

                    var memberNameOverride = ((InvocationExpressionSyntax) replacementAttribute.ArgumentList.Arguments[0].Expression).ArgumentList.Arguments[0]
                        .ToString();

                    introductionSyntax = GetFinalIntroductionSyntax( introductionSyntax, memberNameOverride );
                }
                else if ( replacedAttribute != null )
                {
                    Invariant.Assert( replacementAttribute == null );
                }

                if ( notInlineable || notDiscardable )
                {
                    var flags = AspectLinkerDeclarationFlags.None;

                    if ( notInlineable )
                    {
                        flags |= AspectLinkerDeclarationFlags.NotInlineable;
                    }

                    if ( notDiscardable )
                    {
                        flags |= AspectLinkerDeclarationFlags.NotDiscardable;
                    }

                    introductionSyntax = introductionSyntax.WithLinkerDeclarationFlags( flags );
                }

                var insertPositionRecord = this._currentInsertPosition.AssertNotNull();
                var aspectLayer = this._owner.GetOrAddAspectLayer( aspectName.AssertNotNull(), layerName );

                TestTransformationBase CreateTransformation( CompilationModel compilationModel )
                {
                    var declaringTypeRef = DurableRefFactory.FromSymbolId<INamedType>( SymbolId.Create( symbol.ContainingType ) );
                    var declaringType = declaringTypeRef.GetTarget( compilationModel );
                    var aspectLayerInstance = CreateTestAspectLayerInstance( declaringType, aspectLayer );

                    MemberBuilder builder;
                    IField? replacedField = null;

                    switch ( introductionSyntax )
                    {
                        case MethodDeclarationSyntax methodDeclaration:
                            var methodSymbol = (IMethodSymbol) symbol;
                            var methodBuilder = new MethodBuilder( aspectLayerInstance, declaringType, methodDeclaration.Identifier.ValueText );
                            builder = methodBuilder;

                            methodBuilder.IsAsync = methodSymbol.IsAsync;
                            methodBuilder.IsReadOnly = methodSymbol.IsReadOnly;
                            methodBuilder.SetIsIteratorMethod( methodSymbol.IsIteratorMethod() );

                            methodBuilder.ReturnType = compilationModel.Factory.GetIType( methodSymbol.ReturnType );

                            foreach ( var typeParam in methodSymbol.TypeParameters )
                            {
                                methodBuilder.AddTypeParameter( typeParam.Name );
                            }

                            foreach ( var param in methodSymbol.Parameters )
                            {
                                methodBuilder.AddParameter( param.Name, compilationModel.Factory.GetIType( param.Type ), param.RefKind.ToOurRefKind() );
                            }

                            break;

                        case PropertyDeclarationSyntax propertyDeclaration:
                            var propertySymbol = (IPropertySymbol) symbol;

                            if ( replacementAttribute != null )
                            {
                                var argument = (InvocationExpressionSyntax) replacementAttribute.ArgumentList.Arguments[0].Expression;
                                var nameofArgument = argument.ArgumentList.Arguments[0];
                                var nameofArgumentInfo = semanticModel.GetSymbolInfo( nameofArgument.Expression );
                                var replacedFieldSymbol = nameofArgumentInfo.Symbol.AssertNotNull();
                                replacedField = (IField) this._owner.Builder.TranslateOriginalSymbol( replacedFieldSymbol ).GetTarget( compilationModel );
                            }

                            var propertyBuilder =
                                new PropertyBuilder(
                                    aspectLayerInstance,
                                    declaringType,
                                    propertyDeclaration.Identifier.ValueText,
                                    propertySymbol.GetMethod != null,
                                    propertySymbol.SetMethod != null,
                                    propertySymbol.IsAutoProperty()!.Value,
                                    propertySymbol.SetMethod is { IsInitOnly: true },
                                    propertySymbol.GetMethod is { IsImplicitlyDeclared: true },
                                    propertySymbol.SetMethod is { IsImplicitlyDeclared: true } )
                                {
                                    OriginalField = replacedField
                                };

                            builder = propertyBuilder;

                            propertyBuilder.Type = compilationModel.Factory.GetIType( propertySymbol.Type );

                            break;

                        case EventDeclarationSyntax eventDeclaration:
                            var eventSymbol = (IEventSymbol) symbol;
                            var eventBuilder = new EventBuilder( aspectLayerInstance, declaringType, eventDeclaration.Identifier.ValueText, false );
                            builder = eventBuilder;

                            eventBuilder.Type = (INamedType) compilationModel.Factory.GetIType( eventSymbol.Type );

                            break;

                        case FieldDeclarationSyntax fieldDeclaration:
                            var fieldSymbol = (IFieldSymbol) symbol;
                            var fieldBuilder = new FieldBuilder( aspectLayerInstance, declaringType, fieldDeclaration.Declaration.Variables.Single().Identifier.ValueText );
                            builder = fieldBuilder;

                            fieldBuilder.Type = compilationModel.Factory.GetIType( fieldSymbol.Type );

                            break;

                        case EventFieldDeclarationSyntax eventFieldDeclaration:
                            var eventFieldSymbol = (IEventSymbol) symbol;
                            var eventFieldBuilder = new EventBuilder( aspectLayerInstance, declaringType, eventFieldDeclaration.Declaration.Variables.Single().Identifier.ValueText, true );
                            builder = eventFieldBuilder;

                            eventFieldBuilder.Type = (INamedType) compilationModel.Factory.GetIType( eventFieldSymbol.Type );

                            break;

                        default:
                            throw new NotSupportedException();
                    }

                    builder.Name = introducedElementName;
                    builder.Accessibility = symbol.DeclaredAccessibility.ToOurAccessibility();
                    builder.IsStatic = symbol.IsStatic;
                    builder.IsAbstract = symbol.IsAbstract;
                    builder.IsSealed = symbol.IsSealed;
                    builder.IsVirtual = symbol.IsVirtual;
                    builder.IsOverride = symbol.IsOverride;
                    builder.IsNew = node.Modifiers.Any( m => m.IsKind( SyntaxKind.NewKeyword ) );
                    builder.HasNewKeyword = node.Modifiers.Any( m => m.IsKind( SyntaxKind.NewKeyword ) );

                    builder.Freeze();

                    var builderData = builder switch
                    {
                        MethodBuilder => ((IntroducedRef<IMethod>) builder.ToFullRef()).BuilderData,
                        PropertyBuilder => ((IntroducedRef<IProperty>) builder.ToFullRef()).BuilderData,
                        EventBuilder => ((IntroducedRef<IEvent>) builder.ToFullRef()).BuilderData,
                        FieldBuilder => ((IntroducedRef<IField>) builder.ToFullRef()).BuilderData,
                        _ => throw new NotSupportedException()
                    };

                    if ( replacedField != null )
                    {
                        this._promotions[replacedField.ToFullRef()] = builderData.ToFullRef().As<IProperty>();
                    }

                    this._owner.Builder.RegisterBuilderDataForSymbol( symbol, builderData );

                    var insertPosition = this._owner.Builder.TranslateInsertPosition( insertPositionRecord );

                    if ( replacementAttribute != null )
                    {
                        return
                            new TestPromoteFieldTransformation(
                                aspectLayerInstance,
                                insertPosition,
                                ((PropertyBuilderData) builderData).OriginalField,
                                (PropertyBuilderData) builderData,
                                introductionSyntax );
                    }
                    else
                    {
                        return
                            new TestIntroduceDeclarationTransformation(
                                aspectLayerInstance,
                                insertPosition,
                                builderData,
                                introductionSyntax );
                    }
                }

                this._owner.Builder.AddTransformationFactory( CreateTransformation );
            }

            private static MemberDeclarationSyntax GetFinalIntroductionSyntax( MemberDeclarationSyntax introductionSyntax, string? memberNameOverride )
            {
                if ( memberNameOverride == null )
                {
                    return introductionSyntax;
                }

                switch ( introductionSyntax )
                {
                    case PropertyDeclarationSyntax propertyDecl:
                        return propertyDecl.WithIdentifier( Identifier( memberNameOverride ) );

                    case FieldDeclarationSyntax fieldDecl:
                        return
                            fieldDecl.WithDeclaration(
                                fieldDecl.Declaration.WithVariables(
                                    SingletonSeparatedList( fieldDecl.Declaration.Variables.Single().WithIdentifier( Identifier( memberNameOverride ) ) ) ) );

                    default:
                        throw new AssertionFailedException( $"Unexpected syntax kind {introductionSyntax.GetLocation()}." );
                }
            }

            private void ProcessPseudoOverride(
                SemanticModel semanticModel,
                MemberDeclarationSyntax node,
                IEnumerable<AttributeListSyntax> newAttributeLists,
                AttributeSyntax overrideAttribute,
                bool notInlineable,
                bool notDiscardable )
            {
                if ( overrideAttribute.ArgumentList == null || overrideAttribute.ArgumentList.Arguments.Count is < 2 or > 3 )
                {
                    throw new ArgumentException(
                        "PseudoOverride should have 2 or 3 arguments - overridden declaration name, aspect name and optionally layer name." );
                }

                var argument = (InvocationExpressionSyntax) overrideAttribute.ArgumentList.Arguments[0].Expression;
                var nameofArgument = argument.ArgumentList.Arguments[0];
                var nameofArgumentInfo = semanticModel.GetSymbolInfo( nameofArgument.Expression );

                var overriddenDeclarationSymbol =
                    nameofArgumentInfo switch
                    {
                        {
                                CandidateReason: CandidateReason.MemberGroup,
                                CandidateSymbols: [{ ContainingType.TypeKind: Microsoft.CodeAnalysis.TypeKind.Interface } interfaceMemberSymbol]
                            } =>
                            semanticModel.GetDeclaredSymbol( node )
                                .ContainingType.FindImplementationForInterfaceMember( interfaceMemberSymbol )
                                .AssertNotNull(),
                        {
                                CandidateReason: CandidateReason.MemberGroup,
                                CandidateSymbols: { Length: > 1 } symbols
                            } when
                            symbols.All( s => s is IMethodSymbol { ContainingType.TypeKind: Microsoft.CodeAnalysis.TypeKind.Interface } )
                            && node is MethodDeclarationSyntax { ParameterList.Parameters: { } parameters }
                            && symbols.Count( s => ((IMethodSymbol) s).Parameters.Length == parameters.Count ) == 1 =>
                            semanticModel.GetDeclaredSymbol( node )
                                .ContainingType.FindImplementationForInterfaceMember(
                                    symbols.Single( s => ((IMethodSymbol) s).Parameters.Length == parameters.Count ) )
                                .AssertNotNull(),
                        { Symbol: { ContainingType.TypeKind: Microsoft.CodeAnalysis.TypeKind.Interface } interfaceMemberSymbol } =>
                            semanticModel.GetDeclaredSymbol( node )
                                .ContainingType.FindImplementationForInterfaceMember( interfaceMemberSymbol )
                                .AssertNotNull(),
                        { CandidateReason: CandidateReason.MemberGroup, CandidateSymbols: [{ } symbol] } => symbol,
                        { CandidateReason: CandidateReason.MemberGroup, CandidateSymbols: { Length: > 1 } symbols }
                            when
                            symbols.All( s => s is IMethodSymbol )
                            && node is MethodDeclarationSyntax { ParameterList.Parameters: { } parameters }
                            && symbols.Count( s => ((IMethodSymbol) s).Parameters.Length == parameters.Count ) == 1 =>
                            symbols.Single( s => ((IMethodSymbol) s).Parameters.Length == parameters.Count ),
                        { Symbol: { } symbol } => symbol,
                        _ => throw new AssertionFailedException( "Unsupported" ),
                    };

                var aspectName = overrideAttribute.ArgumentList.Arguments[1].ToString().Trim( '\"' );

                string? layerName = null;

                if ( overrideAttribute.ArgumentList.Arguments.Count == 3 )
                {
                    layerName = overrideAttribute.ArgumentList.Arguments[2].ToString().Trim( '\"' );
                }

                // Rewrite the body of the thing we will insert
                var methodBodyRewriter = new TestMethodBodyRewriter( aspectName, layerName );
                MemberDeclarationSyntax overrideSyntax;

                switch ( node )
                {
                    case MethodDeclarationSyntax { Body: not null } method:
                        var rewrittenMethodBody = methodBodyRewriter.VisitBlock( method.Body.AssertNotNull() );

                        overrideSyntax =
                            method
                                .WithAttributeLists( List( newAttributeLists ) )
                                .WithBody( (BlockSyntax) rewrittenMethodBody.AssertNotNull() );

                        break;

                    case MethodDeclarationSyntax { ExpressionBody: not null } method:
                        var rewrittenMethodExpressionBody = methodBodyRewriter.VisitArrowExpressionClause( method.ExpressionBody.AssertNotNull() );

                        overrideSyntax =
                            method
                                .WithAttributeLists( List( newAttributeLists ) )
                                .WithExpressionBody( (ArrowExpressionClauseSyntax) rewrittenMethodExpressionBody.AssertNotNull() );

                        break;

                    case PropertyDeclarationSyntax { AccessorList: not null } property when property.AccessorList!.Accessors.All( a => a.Body != null ):
                        overrideSyntax =
                            property
                                .WithAttributeLists( List( newAttributeLists ) )
                                .WithAccessorList(
                                    AccessorList(
                                        List(
                                            property.AccessorList!.Accessors.SelectAsImmutableArray(
                                                a => a.WithBody( (BlockSyntax) methodBodyRewriter.VisitBlock( a.Body! ).AssertNotNull() ) ) ) ) );

                        break;

                    case EventDeclarationSyntax @event:
                        overrideSyntax =
                            @event
                                .WithAttributeLists( List( newAttributeLists ) )
                                .WithAccessorList(
                                    AccessorList(
                                        List(
                                            @event.AccessorList!.Accessors.SelectAsImmutableArray(
                                                a => a.WithBody( (BlockSyntax) methodBodyRewriter.VisitBlock( a.Body! ).AssertNotNull() ) ) ) ) );

                        break;

                    case EventFieldDeclarationSyntax eventField:
                        overrideSyntax =
                            eventField
                                .WithAttributeLists( List( newAttributeLists ) );

                        break;

                    default:
                        throw new NotSupportedException();
                }

                if ( notInlineable || notDiscardable )
                {
                    var flags = AspectLinkerDeclarationFlags.None;

                    if ( notInlineable )
                    {
                        flags |= AspectLinkerDeclarationFlags.NotInlineable;
                    }

                    if ( notDiscardable )
                    {
                        flags |= AspectLinkerDeclarationFlags.NotDiscardable;
                    }

                    overrideSyntax = overrideSyntax.WithLinkerDeclarationFlags( flags );
                }

                var insertPositionRecord = this._currentInsertPosition.AssertNotNull();
                var aspectLayer = this._owner.GetOrAddAspectLayer( aspectName.AssertNotNull(), layerName );

                TestTransformationBase CreateTransformation( CompilationModel compilationModel )
                {
                    var overriddenDeclaration = this._owner.Builder.TranslateOriginalSymbol( overriddenDeclarationSymbol ).GetTarget( compilationModel );

                    if ( overriddenDeclaration is IField overriddenField && this._promotions.TryGetValue( overriddenField.ToFullRef(), out var promotedField ) )
                    {
                        overriddenDeclaration = promotedField.GetTarget( compilationModel );
                    }

                    var aspectLayerInstance = CreateTestAspectLayerInstance( overriddenDeclaration, aspectLayer );
                    var insertPosition = this._owner.Builder.TranslateInsertPosition( insertPositionRecord );

                    return new TestOverrideDeclarationTransformation(
                        aspectLayerInstance,
                        insertPosition,
                        overriddenDeclaration.ToFullRef(),
                        overrideSyntax );
                }

                this._owner.Builder.AddTransformationFactory( CreateTransformation );
            }

            private static AspectLayerInstance CreateTestAspectLayerInstance( IDeclaration targetDeclaration, AspectLayerId aspectLayer )
            {
                var fakeAspectInstance = 
                    new AspectInstance( 
                        new TestAspect(), 
                        targetDeclaration, 
                        new TestAspectClass( aspectLayer.AspectName ), 
                        [], 
                        ImmutableArray<AspectPredecessor>.Empty );

                var aspectLayerInstance = new AspectLayerInstance( fakeAspectInstance, aspectLayer.LayerName, null! /* TODO */ );

                return aspectLayerInstance;
            }
        }
    }
}