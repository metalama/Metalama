// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace Metalama.Framework.Engine.ReferenceGraph;

internal sealed class ReferenceIndexWalker : SafeSyntaxWalker
{
    // We don't report MemberAccess as a reference kind to the user, but we use it internally.
    private const ReferenceKinds _memberAccessKind = (ReferenceKinds) 0xf001;

    private readonly CancellationToken _cancellationToken;
    private readonly ISymbolClassificationService? _symbolClassifier;
    private readonly ReferenceIndexBuilder _referenceIndexBuilder;
    private readonly ReferenceIndexerOptions _options;
    private readonly SemanticModelProvider? _semanticModelProvider;
    private readonly IReferenceIndexObserver? _observer;

    private SemanticModel? _semanticModel;
    private SyntaxTree? _syntaxTree;
    private ISymbol? _currentDeclarationSymbol;
    private SyntaxNode? _currentDeclarationNode;
    private ReferenceKinds _currentReferenceKinds = ReferenceKinds.Default;

    public ReferenceIndexWalker(
        ProjectServiceProvider serviceProvider,
        ReferenceIndexBuilder referenceIndexBuilder,
        ReferenceIndexerOptions options,
        CancellationToken cancellationToken )
    {
        // This class cannot run concurrently on many threads.
        this._cancellationToken = cancellationToken;
        this._referenceIndexBuilder = referenceIndexBuilder;
        this._options = options;
        this._symbolClassifier = serviceProvider.GetService<ISymbolClassificationService>(); // May be absent in the introspection scenario.
        this._observer = serviceProvider.GetService<IReferenceIndexObserver>();
    }

    public ReferenceIndexWalker(
        ProjectServiceProvider serviceProvider,
        ReferenceIndexBuilder referenceIndexBuilder,
        ReferenceIndexerOptions options,
        SemanticModelProvider? semanticModelProvider,
        CancellationToken cancellationToken )
    {
        // This class cannot run concurrently on many threads.
        this._cancellationToken = cancellationToken;
        this._referenceIndexBuilder = referenceIndexBuilder;
        this._options = options;
        this._semanticModelProvider = semanticModelProvider;
        this._symbolClassifier = serviceProvider.GetService<ISymbolClassificationService>(); // May be absent in the introspection scenario.
        this._observer = serviceProvider.GetService<IReferenceIndexObserver>();
    }

    public ReferenceIndexWalker(
        ProjectServiceProvider serviceProvider,
        ReferenceIndexBuilder referenceIndexBuilder,
        ReferenceIndexerOptions options,
        SemanticModel semanticModel,
        CancellationToken cancellationToken )
    {
        // This class cannot run concurrently on many threads.
        this._cancellationToken = cancellationToken;
        this._referenceIndexBuilder = referenceIndexBuilder;
        this._options = options;
        this._semanticModel = semanticModel;
        this._syntaxTree = semanticModel.SyntaxTree;
        this._symbolClassifier = serviceProvider.GetService<ISymbolClassificationService>(); // May be absent in the introspection scenario.
        this._observer = serviceProvider.GetService<IReferenceIndexObserver>();
    }

    public void Visit( SemanticModel semanticModel )
    {
        this._semanticModel = semanticModel;
        this._syntaxTree = semanticModel.SyntaxTree;
        this.Visit( this._syntaxTree.GetRoot() );
    }

    public void Visit( SyntaxTree syntaxTree )
    {
        this._syntaxTree = syntaxTree;
        this.Visit( this._syntaxTree.GetRoot() );
    }

    public override void DefaultVisit( SyntaxNode node )
    {
        this._cancellationToken.ThrowIfCancellationRequested();
        base.DefaultVisit( node );
    }

    public override void VisitIdentifierName( IdentifierNameSyntax node ) => this.IndexReference( node, node.Identifier );

    public override void VisitPredefinedType( PredefinedTypeSyntax node )
    {
        this.IndexReference( node, node, default );
    }

    public override void VisitAssignmentExpression( AssignmentExpressionSyntax node )
    {
        this.Visit( node.Right );
        this.VisitWithReferenceKinds( node.Left, ReferenceKinds.Assignment );

        switch ( node.Left )
        {
            case MemberAccessExpressionSyntax memberAccess:
                this.Visit( memberAccess.Expression );

                break;

            case ElementAccessExpressionSyntax elementAccess:
                this.Visit( elementAccess.Expression );
                this.Visit( elementAccess.ArgumentList );

                break;

            case IdentifierNameSyntax:
                // If we just have an identifier, we have nothing to visit.
                break;

            // Other cases are possible but we don't implement them.
            // For instance, we can assign the return value of a ref method.
        }
    }

    public override void VisitInvocationExpression( InvocationExpressionSyntax node )
    {
        if ( node.IsNameOf() )
        {
            this.VisitWithReferenceKinds( node.ArgumentList.Arguments[0].Expression, ReferenceKinds.NameOf );
        }
        else
        {
            this.VisitWithReferenceKinds( node.Expression, ReferenceKinds.Invocation );

            foreach ( var arg in node.ArgumentList.Arguments )
            {
                this.Visit( arg );
            }
        }
    }

    public override void VisitPrimaryConstructorBaseType( PrimaryConstructorBaseTypeSyntax node )
    {
        this.VisitTypeReference( node.Type, ReferenceKinds.BaseType );

        if ( this._options.MustDescendIntoImplementation() )
        {
            this.Visit( node.ArgumentList );
        }
    }

    public override void VisitSimpleBaseType( SimpleBaseTypeSyntax node ) => this.VisitTypeReference( node.Type, ReferenceKinds.BaseType );

    public override void VisitTypeArgumentList( TypeArgumentListSyntax node )
    {
        foreach ( var arg in node.Arguments )
        {
            this.VisitTypeReference( arg, ReferenceKinds.TypeArgument );
        }
    }

    public override void VisitTypeOfExpression( TypeOfExpressionSyntax node ) => this.VisitTypeReference( node.Type, ReferenceKinds.TypeOf );

    public override void VisitParameter( ParameterSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.VisitTypeReference( node.Type, ReferenceKinds.ParameterType );
            this.Visit( node.AttributeLists );
        }
    }

    public override void VisitAttribute( AttributeSyntax node )
    {
        this.VisitWithReferenceKinds( node.Name, ReferenceKinds.AttributeType );

        if ( node.ArgumentList != null && this._options.MustDescendIntoImplementation() )
        {
            foreach ( var arg in node.ArgumentList.Arguments )
            {
                this.Visit( arg );
            }
        }
    }

    public override void VisitTypeConstraint( TypeConstraintSyntax node ) => this.VisitTypeReference( node.Type, ReferenceKinds.TypeConstraint );

    private void VisitMembers( SyntaxList<MemberDeclarationSyntax> members )
    {
        if ( this._options.MustDescendIntoMembers() )
        {
            this.Visit( members );
        }
        else
        {
            // Even if we must not descend into members, we must still visit nested types.
            foreach ( var member in members )
            {
                if ( SyntaxFacts.IsTypeDeclaration( member.Kind() ) )
                {
                    this.Visit( member );

                    break;
                }
            }
        }
    }

    public override void VisitClassDeclaration( ClassDeclarationSyntax node )
    {
        using ( this.EnterTypeDeclarationDefinition( node ) )
        {
            this.Visit( node.AttributeLists );
            this.Visit( node.BaseList );
            this.Visit( node.ConstraintClauses );

            if ( this._options.MustDescendIntoMembers() )
            {
                this.VisitMembers( node.Members );

#if ROSLYN_4_8_0_OR_GREATER
                this.Visit( node.ParameterList );
#endif
            }
        }
    }

    public override void VisitRecordDeclaration( RecordDeclarationSyntax node )
    {
        using ( this.EnterTypeDeclarationDefinition( node ) )
        {
            this.Visit( node.AttributeLists );
            this.Visit( node.BaseList );
            this.Visit( node.ConstraintClauses );

            if ( this._options.MustDescendIntoMembers() )
            {
                this.Visit( node.ParameterList );
            }

            this.VisitMembers( node.Members );
        }
    }

    public override void VisitStructDeclaration( StructDeclarationSyntax node )
    {
        using ( this.EnterTypeDeclarationDefinition( node ) )
        {
            this.Visit( node.AttributeLists );
            this.Visit( node.BaseList );
            this.Visit( node.ConstraintClauses );

            if ( this._options.MustDescendIntoMembers() )
            {
                this.VisitMembers( node.Members );

#if ROSLYN_4_8_0_OR_GREATER
                this.Visit( node.ParameterList );
#endif
            }
        }
    }

    public override void VisitDelegateDeclaration( DelegateDeclarationSyntax node )
    {
        using ( this.EnterTypeDeclarationDefinition( node ) )
        {
            base.VisitDelegateDeclaration( node );
        }
    }

    public override void VisitEnumDeclaration( EnumDeclarationSyntax node )
    {
        using ( this.EnterTypeDeclarationDefinition( node ) )
        {
            this.Visit( node.BaseList );
            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoMembers() )
            {
                foreach ( var member in node.Members )
                {
                    this.Visit( member );
                }
            }
        }
    }

    public override void VisitMethodDeclaration( MethodDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.IndexMember<IMethodSymbol>( node, m => m.OverriddenMethod, m => m.ExplicitInterfaceImplementations );

            this.VisitTypeReference( node.ReturnType, ReferenceKinds.ReturnType );

            foreach ( var parameter in node.ParameterList.Parameters )
            {
                this.Visit( parameter );
            }

            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    public override void VisitPropertyDeclaration( PropertyDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.Visit( node.AttributeLists );

            this.IndexMember<IPropertySymbol>( node, m => m.OverriddenProperty, m => m.ExplicitInterfaceImplementations );
            this.VisitTypeReference( node.Type, ReferenceKinds.MemberType );

            this.Visit( node.AccessorList );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Initializer );
            }
        }
    }

    public override void VisitEventDeclaration( EventDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.Visit( node.AttributeLists );

            this.IndexMember<IEventSymbol>( node, m => m.OverriddenEvent, m => m.ExplicitInterfaceImplementations );

            this.VisitTypeReference( node.Type, ReferenceKinds.MemberType );

            this.Visit( node.AccessorList );
        }
    }

    public override void VisitFieldDeclaration( FieldDeclarationSyntax node )
    {
        for ( var index = 0; index < node.Declaration.Variables.Count; index++ )
        {
            var variable = node.Declaration.Variables[index];

            using ( this.EnterDeclaration( variable ) )
            {
                if ( index == 0 )
                {
                    // Attributes must be visited a single time.
                    this.Visit( node.AttributeLists );
                }

                // The type must be visited every time.
                this.VisitTypeReference( node.Declaration.Type, ReferenceKinds.MemberType );

                if ( variable.Initializer != null && this._options.MustDescendIntoImplementation() )
                {
                    using ( this.EnterDeclaration( variable ) )
                    {
                        this.Visit( variable.Initializer );
                    }
                }
            }
        }
    }

    public override void VisitEventFieldDeclaration( EventFieldDeclarationSyntax node )
    {
        for ( var index = 0; index < node.Declaration.Variables.Count; index++ )
        {
            var variable = node.Declaration.Variables[index];

            using ( this.EnterDeclaration( variable ) )
            {
                if ( index == 0 )
                {
                    // Attributes must be visited a single time.
                    this.Visit( node.AttributeLists );
                }

                // The type must be visited every time.
                this.VisitTypeReference( node.Declaration.Type, ReferenceKinds.MemberType );

                if ( variable.Initializer != null && this._options.MustDescendIntoImplementation() )
                {
                    using ( this.EnterDeclaration( variable ) )
                    {
                        this.Visit( variable.Initializer );
                    }
                }
            }
        }
    }

    public override void VisitLocalDeclarationStatement( LocalDeclarationStatementSyntax node )
    {
        this.VisitTypeReference( node.Declaration.Type, ReferenceKinds.LocalVariableType );

        foreach ( var variable in node.Declaration.Variables )
        {
            if ( variable.Initializer != null )
            {
                this.Visit( variable.Initializer );
            }
        }
    }

    public override void VisitOperatorDeclaration( OperatorDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.VisitTypeReference( node.ReturnType, ReferenceKinds.ReturnType );

            foreach ( var parameter in node.ParameterList.Parameters )
            {
                this.Visit( parameter );
            }

            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    public override void VisitAccessorDeclaration( AccessorDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    private static SyntaxToken GetTypeIdentifier( TypeSyntax? syntax )
        => syntax switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier,
            GenericNameSyntax generic => generic.Identifier,
            QualifiedNameSyntax qualified => GetTypeIdentifier( qualified.Right ),
            _ => default
        };

    public override void VisitConstructorDeclaration( ConstructorDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.Visit( node.AttributeLists );
            var baseClassIdentifier = GetTypeIdentifier( ((TypeDeclarationSyntax) node.Parent!).BaseList?.Types[0].Type );

            // Visit the base constructor.
            if ( node.Initializer != null )
            {
                this.IndexReference( node.Initializer, node.Initializer.ThisOrBaseKeyword, baseClassIdentifier, ReferenceKinds.BaseConstructor );
            }
            else
            {
                // We need to find the base constructor.
                if ( this._options.MustIndexReference( ReferenceKinds.BaseConstructor, baseClassIdentifier ) )
                {
                    var symbol = this.SemanticModel.GetDeclaredSymbol( node );

                    if ( symbol != null )
                    {
                        this._observer?.OnSymbolResolved( symbol );

                        var baseConstructorSymbol = symbol.ContainingType.BaseType?.Constructors.FirstOrDefault( c => c.Parameters.Length == 0 );

                        if ( baseConstructorSymbol != null )
                        {
                            this._referenceIndexBuilder.AddReference(
                                baseConstructorSymbol,
                                this.CurrentDeclarationSymbol.AssertSymbolNotNull(),
                                node.Identifier,
                                ReferenceKinds.BaseConstructor );
                        }
                    }
                }
            }

            // Visit parameters.
            foreach ( var parameter in node.ParameterList.Parameters )
            {
                this.Visit( parameter );
            }

            // Visit the body.
            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    public override void VisitDestructorDeclaration( DestructorDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    public override void VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.VisitTypeReference( node.Type, ReferenceKinds.ReturnType );

            foreach ( var parameter in node.ParameterList.Parameters )
            {
                this.Visit( parameter );
            }

            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
                this.Visit( node.Body );
            }
        }
    }

    public override void VisitIndexerDeclaration( IndexerDeclarationSyntax node )
    {
        using ( this.EnterDeclaration( node ) )
        {
            this.VisitTypeReference( node.Type, ReferenceKinds.ReturnType );

            foreach ( var parameter in node.ParameterList.Parameters )
            {
                this.Visit( parameter );
            }

            this.Visit( node.AttributeLists );

            if ( this._options.MustDescendIntoImplementation() )
            {
                this.Visit( node.ExpressionBody );
            }

            this.Visit( node.AccessorList );
        }
    }

    public override void VisitObjectCreationExpression( ObjectCreationExpressionSyntax node )
    {
        this.IndexReference( node, GetTypeIdentifier( node.Type ), ReferenceKinds.ObjectCreation );

        this.Visit( node.ArgumentList );
        this.Visit( node.Initializer );
    }

    public override void VisitArrayCreationExpression( ArrayCreationExpressionSyntax node )
    {
        this.IndexReference( node, GetTypeIdentifier( node.Type ), ReferenceKinds.ArrayCreation );

        this.Visit( node.Initializer );
    }

#if ROSLYN_4_8_0_OR_GREATER
    public override void VisitCollectionExpression( CollectionExpressionSyntax node )
    {
        if ( this._options.MustIndexReferenceKind( ReferenceKinds.ArrayCreation | ReferenceKinds.ObjectCreation ) && this._currentDeclarationNode != null )
        {
            var expressionType = this.SemanticModel.GetTypeInfo( node ).ConvertedType;

            if ( expressionType == null )
            {
                return;
            }

            this._observer?.OnSymbolResolved( expressionType );

            if ( expressionType is IArrayTypeSymbol arrayType )
            {
                this._referenceIndexBuilder.AddReference( arrayType.ElementType, this.CurrentDeclarationSymbol, node, ReferenceKinds.ArrayCreation );
            }
            else
            {
                this._referenceIndexBuilder.AddReference( expressionType, this.CurrentDeclarationSymbol, node, ReferenceKinds.ObjectCreation );
            }
        }
    }
#endif

    public override void VisitImplicitObjectCreationExpression( ImplicitObjectCreationExpressionSyntax node )
    {
        this.IndexReference( node, node.NewKeyword, default, ReferenceKinds.ObjectCreation );

        this.Visit( node.Initializer );
        this.Visit( node.ArgumentList );
    }

    public override void VisitUsingDirective( UsingDirectiveSyntax node ) => this.VisitWithReferenceKinds( node.Name, ReferenceKinds.UsingNamespace );

    public override void VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
    {
        this.Visit( node.Name );

        // Avoid indexing 'this', 'base' and types on the left side.
        if ( node.Expression.Kind() is not (SyntaxKind.BaseExpression or SyntaxKind.ThisExpression) )
        {
            this.VisitWithReferenceKinds( node.Expression, _memberAccessKind );
        }
    }

    public override void VisitNamespaceDeclaration( NamespaceDeclarationSyntax node ) => this.Visit( node.Members );

    public override void VisitFileScopedNamespaceDeclaration( FileScopedNamespaceDeclarationSyntax node ) => this.Visit( node.Members );

    public override void VisitElementAccessExpression( ElementAccessExpressionSyntax node )
    {
        // We may have an indexer access, which will not be discovered in another node than this one.
        this.IndexReference( node, node.ArgumentList, default );

        // Continue visiting the children.
        base.VisitElementAccessExpression( node );
    }

    public override void VisitCastExpression( CastExpressionSyntax node )
    {
        this.VisitTypeReference( node.Type, ReferenceKinds.CastType );

        this.Visit( node.Expression );
    }

    public override void VisitBinaryExpression( BinaryExpressionSyntax node )
    {
        if ( node.IsKind( SyntaxKind.AsExpression ) )
        {
            this.VisitTypeReference( node.Right, ReferenceKinds.CastType );

            this.Visit( node.Left );
        }
        else if ( node.IsKind( SyntaxKind.IsExpression ) )
        {
            this.VisitTypeReference( node.Right, ReferenceKinds.IsType );

            this.Visit( node.Left );
        }
        else
        {
            base.VisitBinaryExpression( node );
        }
    }

    public override void VisitRecursivePattern( RecursivePatternSyntax node )
    {
        this.VisitTypeReference( node.Type, ReferenceKinds.IsType );
        this.Visit( node.Designation );
        this.Visit( node.PositionalPatternClause );
        this.Visit( node.PropertyPatternClause );
    }

    private void VisitWithReferenceKinds( SyntaxNode? node, ReferenceKinds referenceKind )
    {
#if DEBUG
        if ( referenceKind == ReferenceKinds.None )
        {
            throw new ArgumentOutOfRangeException( nameof(referenceKind) );
        }
#endif

        if ( node == null )
        {
            return;
        }

        var previousReferenceKinds = this._currentReferenceKinds;
        this._currentReferenceKinds = referenceKind;
        this.Visit( node );
        this._currentReferenceKinds = previousReferenceKinds;
    }

    private void IndexReference(
        SyntaxNode node,
        SyntaxToken identifier,
        ReferenceKinds referenceKind = ReferenceKinds.Default )
        => this.IndexReference( node, node, identifier, referenceKind );

    private void IndexReference(
        SyntaxNode nodeForSymbol,
        SyntaxNodeOrToken nodeForReference,
        SyntaxToken identifierForFiltering,
        ReferenceKinds referenceKind = ReferenceKinds.Default )
    {
        if ( this._currentDeclarationNode == null )
        {
            return;
        }

        referenceKind = this.GetEffectiveReferenceKind( referenceKind );

        var isMemberAccessKind = referenceKind == _memberAccessKind;

        if ( isMemberAccessKind )
        {
            referenceKind = ReferenceKinds.Default;
        }

        if ( this._options.MustIndexReference( referenceKind, identifierForFiltering ) )
        {
            var symbol = this.SemanticModel.GetSymbolInfo( nodeForSymbol ).Symbol;

            if ( symbol == null )
            {
                return;
            }

            this._observer?.OnSymbolResolved( symbol );

            if ( !this.CanIndexSymbol( symbol ) )
            {
                return;
            }

            if ( isMemberAccessKind && symbol.Kind == SymbolKind.NamedType )
            {
                // We don't index access of static members on type level.
                return;
            }

            this._referenceIndexBuilder.AddReference( symbol, this.CurrentDeclarationSymbol, nodeForReference, referenceKind );
        }
    }

    private void IndexMember<T>(
        MemberDeclarationSyntax node,
        Func<T, T?> getOverridenMember,
        Func<T, ImmutableArray<T>> getImplementedInterfaceMembers )
        where T : class, ISymbol
    {
        if ( this._currentDeclarationNode == null )
        {
            return;
        }

        if ( this._options.MustIndexReferenceKind( ReferenceKinds.OverrideMember | ReferenceKinds.InterfaceMemberImplementation ) )
        {
            var symbol = (T?) this.CurrentDeclarationSymbol;

            if ( symbol == null )
            {
                return;
            }

            if ( this._options.MustIndexReferenceKind( ReferenceKinds.OverrideMember ) )
            {
                var overridenMember = getOverridenMember( symbol );

                if ( overridenMember != null )
                {
                    this._referenceIndexBuilder.AddReference( overridenMember, this.CurrentDeclarationSymbol, node, ReferenceKinds.OverrideMember );
                }
            }

            if ( this._options.MustIndexReferenceKind( ReferenceKinds.InterfaceMemberImplementation ) )
            {
                var interfaceMembers = getImplementedInterfaceMembers( symbol );

                foreach ( var member in interfaceMembers )
                {
                    this._referenceIndexBuilder.AddReference( member, this.CurrentDeclarationSymbol, node, ReferenceKinds.InterfaceMemberImplementation );
                }
            }
        }
    }

    private ReferenceKinds GetEffectiveReferenceKind( ReferenceKinds referenceKinds )
    {
        if ( referenceKinds is ReferenceKinds.Default or ReferenceKinds.None )
        {
            // This happens for standalone identifiers or for member access.
            referenceKinds = this._currentReferenceKinds;
        }

        return referenceKinds;
    }

    private bool CanIndexSymbol( ISymbol symbol )
    {
        switch ( symbol.Kind )
        {
            case SymbolKind.ArrayType:
            case SymbolKind.Assembly:
            case SymbolKind.DynamicType:
            case SymbolKind.Event:
            case SymbolKind.Field:
            case SymbolKind.Method:
            case SymbolKind.NetModule:
            case SymbolKind.NamedType:
            case SymbolKind.Namespace:
            case SymbolKind.PointerType:
            case SymbolKind.Property:
            case SymbolKind.FunctionPointerType:
                break;

            default:
                return false;
        }

        // Ignore any compile-time type declaration.
        var currentType = this.CurrentDeclarationSymbol?.GetClosestContainingType();

        if ( currentType != null && this._symbolClassifier?.GetExecutionScope( currentType ) is not (null or ExecutionScope.RunTime) )
        {
            return false;
        }

        return true;
    }

    // We are accepting nulls to be more resilient at design time.
    private ISymbol? CurrentDeclarationSymbol
    {
        get
        {
            if ( this._currentDeclarationSymbol == null )
            {
                if ( this._currentDeclarationNode != null )
                {
                    this._currentDeclarationSymbol = this.SemanticModel.GetDeclaredSymbol( this._currentDeclarationNode );

                    if ( this._observer != null && this._currentDeclarationSymbol != null )
                    {
                        this._observer.OnSymbolResolved( this._currentDeclarationSymbol );
                    }
                }
                else
                {
                    this._currentDeclarationSymbol = null;
                }
            }

            return this._currentDeclarationSymbol;
        }
    }

    private SemanticModel SemanticModel
    {
        get
        {
            if ( this._semanticModel == null )
            {
                this._semanticModel = this._semanticModelProvider.AssertNotNull().GetSemanticModel( this._syntaxTree.AssertNotNull(), true );
                this._observer?.OnSemanticModelResolved( this._semanticModel );
            }

            return this._semanticModel;
        }
    }

    private DeclarationContextCookie EnterDeclaration( SyntaxNode node )
    {
        var previousSymbol = this._currentDeclarationSymbol;
        var previousNode = this._currentDeclarationNode;

        this._currentDeclarationSymbol = null;
        this._currentDeclarationNode = node;

        return new DeclarationContextCookie( this, previousSymbol, previousNode );
    }

    private DeclarationContextCookie EnterTypeDeclarationDefinition( SyntaxNode node ) => this.EnterDeclaration( node );

    private void Visit<T>( SyntaxList<T> list )
        where T : SyntaxNode
    {
        foreach ( var node in list )
        {
            this.Visit( node );
        }
    }

    private void VisitTypeReference( SyntaxNode? type, ReferenceKinds kind )
    {
        if ( type == null )
        {
            return;
        }

        switch ( type.Kind() )
        {
            case SyntaxKind.IdentifierName:
            case SyntaxKind.QualifiedName:
            case SyntaxKind.PredefinedType:
                this.VisitWithReferenceKinds( type, kind );

                break;

            case SyntaxKind.NullableType:
                this.VisitWithReferenceKinds( ((NullableTypeSyntax) type).ElementType, kind );

                break;

            case SyntaxKind.ArrayType:
                this.VisitWithReferenceKinds( ((ArrayTypeSyntax) type).ElementType, ReferenceKinds.ArrayElementType );

                break;

            case SyntaxKind.PointerType:
                this.VisitWithReferenceKinds( ((PointerTypeSyntax) type).ElementType, ReferenceKinds.PointerType );

                break;

            case SyntaxKind.RefType:
                this.VisitWithReferenceKinds( ((RefTypeSyntax) type).Type, kind );

                break;

            case SyntaxKind.TupleType:
                foreach ( var item in ((TupleTypeSyntax) type).Elements )
                {
                    this.VisitTypeReference( item.Type, ReferenceKinds.TupleElementType );
                }

                break;

            case SyntaxKind.AliasQualifiedName:
            case SyntaxKind.FunctionPointerType:
                // Not implemented;
                break;

            case SyntaxKind.GenericName:
                {
                    var genericType = (GenericNameSyntax) type;

                    if ( genericType.Identifier.Text == nameof(Nullable<int>) )
                    {
                        // Process nullable types consitently as the ? operator.
                        this.VisitWithReferenceKinds( genericType.TypeArgumentList.Arguments[0], kind );

                        return;
                    }
                    else
                    {
                        this.IndexReference( genericType, genericType.Identifier, kind );
                    }

                    foreach ( var arg in genericType.TypeArgumentList.Arguments )
                    {
                        this.VisitTypeReference( arg, ReferenceKinds.TypeArgument );
                    }
                }

                break;
        }
    }

    private readonly struct DeclarationContextCookie : IDisposable
    {
        private readonly ReferenceIndexWalker? _parent;
        private readonly ISymbol? _previousDeclaration;
        private readonly SyntaxNode? _previousNode;

        public DeclarationContextCookie( ReferenceIndexWalker parent, ISymbol? previousDeclaration, SyntaxNode? previousNode )
        {
            this._parent = parent;
            this._previousDeclaration = previousDeclaration;
            this._previousNode = previousNode;
        }

        public void Dispose()
        {
            if ( this._parent != null )
            {
                this._parent._currentDeclarationSymbol = this._previousDeclaration;
                this._parent._currentDeclarationNode = this._previousNode;
            }
        }
    }
}