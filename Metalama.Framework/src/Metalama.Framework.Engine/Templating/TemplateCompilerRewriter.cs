// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

// ReSharper disable RedundantUsingDirective

using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CompileTime;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.SerializableIds;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;
using SpecialType = Microsoft.CodeAnalysis.SpecialType;
using TypeKind = Microsoft.CodeAnalysis.TypeKind;

namespace Metalama.Framework.Engine.Templating;

/// <summary>
/// Compiles the source code of a template, annotated with <see cref="TemplateAnnotator"/>,
/// to an executable template.
/// </summary>
internal sealed partial class TemplateCompilerRewriter : MetaSyntaxRewriter, IDiagnosticAdder
{
    private const string _rewrittenTypeOfAnnotation = "Metalama.RewrittenTypeOf";
    private static readonly SyntaxAnnotation _userExpressionAnnotation = new( "Metalama.UserExpression" );
    private static readonly Regex _endOfLineRegex = new( "[\r\n\\s]+", RegexOptions.Multiline );

    private readonly TemplateCompilerSemantics _syntaxKind;
    private readonly Compilation _runTimeCompilation;
    private readonly string _templateName;
    private readonly SyntaxTreeAnnotationMap _syntaxTreeAnnotationMap;
    private readonly IDiagnosticAdder _diagnosticAdder;
    private readonly CancellationToken _cancellationToken;
    private readonly SerializableTypes _serializableTypes;
    private readonly TemplateMemberClassifier _templateMemberClassifier;
    private readonly CompileTimeOnlyRewriter _compileTimeOnlyRewriter;
    private readonly TypeOfRewriter _typeOfRewriter;
    private readonly TypeSyntax _templateTypeArgumentType;
    private readonly HashSet<string> _templateCompileTimeTypeParameterNames = [];
    private readonly TypeSyntax _templateSyntaxFactoryType;
    private readonly TypeSyntax _dictionaryOfITypeType;
    private readonly TypeSyntax _dictionaryOfTypeSyntaxType;
    private readonly ITypeSymbol _iExpressionSymbol;
    private readonly TypeSyntax _unsafeType;

    private TemplateMetaSyntaxFactoryImpl _templateMetaSyntaxFactory;
    private MetaContext? _currentMetaContext;
    private int _nextStatementListId;
    private int _nextLocalFunctionFactoryId;
    private int _nextLabelId = 1;
    private ISymbol? _rootTemplateSymbol;

    public TemplateCompilerRewriter(
        string templateName,
        TemplateCompilerSemantics syntaxKind,
        ClassifyingCompilationContext runTimeCompilationContext,
        SyntaxTreeAnnotationMap syntaxTreeAnnotationMap,
        IDiagnosticAdder diagnosticAdder,
        CompilationContext compileTimeCompilationContext,
        SerializableTypes serializableTypes,
        RoslynApiVersion targetApiVersion,
        CancellationToken cancellationToken ) : base( compileTimeCompilationContext, targetApiVersion )
    {
        this._templateName = templateName;
        this._syntaxKind = syntaxKind;
        this._runTimeCompilation = runTimeCompilationContext.SourceCompilation;
        this._syntaxTreeAnnotationMap = syntaxTreeAnnotationMap;
        this._diagnosticAdder = diagnosticAdder;
        this._cancellationToken = cancellationToken;
        this._serializableTypes = serializableTypes;
        this._templateMetaSyntaxFactory = new TemplateMetaSyntaxFactoryImpl( _templateSyntaxFactoryParameterName );

        this._templateMemberClassifier = new TemplateMemberClassifier(
            runTimeCompilationContext,
            syntaxTreeAnnotationMap );

        this._compileTimeOnlyRewriter = new CompileTimeOnlyRewriter( this );

        var syntaxGenerationContext = compileTimeCompilationContext.GetSyntaxGenerationContext( SyntaxGenerationOptions.Formatted );
        this._typeOfRewriter = new TypeOfRewriter( syntaxGenerationContext );

        this._templateTypeArgumentType =
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.MetaSyntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(TemplateTypeArgument) ) );

        this._templateSyntaxFactoryType =
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.MetaSyntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(ITemplateSyntaxFactory) ) );

        this._dictionaryOfTypeSyntaxType =
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax(
                this.MetaSyntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(Dictionary<string, TypeSyntax>) ) );

        this._dictionaryOfITypeType =
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.MetaSyntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(Dictionary<string, IType>) ) );

        this._iExpressionSymbol = this._runTimeCompilation.GetTypeByMetadataName( typeof(IExpression).FullName! ).AssertSymbolNotNull();

        this._unsafeType =
            syntaxGenerationContext.SyntaxGenerator.TypeSyntax( this.MetaSyntaxFactory.ReflectionMapper.GetTypeSymbol( typeof(Unsafe) ) );
    }

    public bool Success { get; private set; } = true;

    public void Report( Diagnostic diagnostic )
    {
        this._diagnosticAdder.Report( diagnostic );

        if ( diagnostic.Severity == DiagnosticSeverity.Error )
        {
            this.Success = false;
        }
    }

    public override bool VisitIntoStructuredTrivia => false;

    /// <summary>
    /// Sets the current <see cref="MetaContext"/> for the current execution context. To be used in a <c>using</c> statement.
    /// </summary>
    /// <param name="newMetaContext"></param>
    /// <returns></returns>
    private MetaContextCookie WithMetaContext( MetaContext newMetaContext )
    {
        var cookie = new MetaContextCookie( this, this._currentMetaContext );

        this._currentMetaContext = newMetaContext;

        return cookie;
    }

    /// <summary>
    /// Generates the code to generate a run-time symbol name (i.e. a call to <see cref="ITemplateSyntaxFactory.GetUniqueIdentifier"/>),
    /// adds this code to the list of statements of the current <see cref="MetaContext"/>, and returns the identifier of
    /// the compiled template that contains the run-time symbol name.
    /// </summary>
    /// <param name="symbol">The symbol in the source template.</param>
    /// <returns>The identifier of the compiled template that contains the run-time symbol name.</returns>
    private IdentifierNameSyntax ReserveRunTimeSymbolName( ISymbol symbol )
    {
        var metaVariableIdentifier = this._currentMetaContext!.GetTemplateVariableName( symbol );

        this.DeclareMetaVariable( symbol.Name, metaVariableIdentifier );

        return SyntaxFactoryEx.WellKnownIdentifierName( metaVariableIdentifier );
    }

    private IdentifierNameSyntax ReserveRunTimeVariableName( string name )
    {
        var metaVariableIdentifier = this._currentMetaContext!.GetTemplateVariableName( name );

        this.DeclareMetaVariable( name, metaVariableIdentifier );

#pragma warning disable LAMA0850 // Intentional use: metaVariableIdentifier is a well-known generated identifier
        return IdentifierName( metaVariableIdentifier.AddMetaVariableAnnotation() );
#pragma warning restore LAMA0850
    }

    private void DeclareMetaVariable( string hint, SyntaxToken metaVariableIdentifier )
    {
        var callGetUniqueIdentifier = this._templateMetaSyntaxFactory.GetUniqueIdentifier( hint );

        var localDeclaration =
            LocalDeclarationStatement(
                    VariableDeclaration( this.MetaSyntaxFactory.Type( typeof(SyntaxToken) ) )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator( metaVariableIdentifier )
                                    .WithInitializer( EqualsValueClause( callGetUniqueIdentifier ) ) ) ) )
                .NormalizeWhitespace();

        this._currentMetaContext!.AddStatement( localDeclaration );
    }

    /// <summary>
    /// Determines how a <see cref="SyntaxNode"/> should be transformed:
    /// <see cref="MetaSyntaxRewriter.TransformationKind.None"/> for compile-time code
    /// or <see cref="MetaSyntaxRewriter.TransformationKind.Transform"/> for run-time code.
    /// </summary>
    protected override TransformationKind GetTransformationKind( SyntaxNode node )
        => IsCompileTimeCode( node ) ? TransformationKind.None : TransformationKind.Transform;

    internal static bool IsCompileTimeCode( SyntaxNode node )
    {
        var targetScope = node.GetTargetScopeFromAnnotation();

        if ( targetScope == TemplatingScope.MustFollowParent )
        {
            // This flag is a hack and means that the scoped inferred from the depth-first analysis must be ignored,
            // and the scope must be determined from the parent only.
            return GetFromParent();
        }

        var scope = node.GetScopeFromAnnotation().GetValueOrDefault( TemplatingScope.RunTimeOrCompileTime );

        // Take a decision from the node if we can.
        if ( scope.IsUndetermined() )
        {
            switch ( targetScope )
            {
                case TemplatingScope.RunTimeOnly:
                    return false;

                case TemplatingScope.CompileTimeOnly:
                    return true;

                case TemplatingScope.RunTimeOrCompileTime:
                    return GetFromParent();

                default:
                    throw new AssertionFailedException();
            }
        }
        else
        {
            // If we have a scope annotation, follow it.
            return !scope.MustBeTransformed();
        }

        bool GetFromParent()
        {
            // Look for annotation on the parent, but stop at 'if', 'foreach', and similar statements,
            // which have special interpretation.
            var parent = node.Parent;

            switch ( parent?.Kind() )
            {
                case null:
                    // This situation seems to happen only when Transform is called from a newly created syntax node,
                    // which has not been added to the syntax tree yet. Transform then calls Visit and, which then calls GetTransformationKind
                    // so we need to return Transform here. This is not nice and would need to be refactored.

                    return false;

                case SyntaxKind.IfStatement:
                case SyntaxKind.ElseClause:
                case SyntaxKind.SwitchSection:
                case SyntaxKind.ForEachStatement:
                case SyntaxKind.ForEachVariableStatement:
                case SyntaxKind.WhileStatement:
                case SyntaxKind.DoStatement:
                    throw new AssertionFailedException( $"The node '{node}' must be annotated." );

                default:
                    return IsCompileTimeCode( parent );
            }
        }
    }

    private T TransformCompileTimeCode<T>( T node )
        where T : SyntaxNode
        => (T) this._compileTimeOnlyRewriter.Visit( node )!;

    protected override SyntaxNode? VisitCore( SyntaxNode? node )
    {
        if ( node == null )
        {
            return null;
        }

        this._cancellationToken.ThrowIfCancellationRequested();

        // Captures the root symbol.
        if ( this._rootTemplateSymbol == null )
        {
            if ( node == null )
            {
                throw new ArgumentNullException( nameof(node) );
            }

            this._rootTemplateSymbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( node );

            if ( this._rootTemplateSymbol == null )
            {
                throw new AssertionFailedException( "Didn't find a symbol for a template method node." );
            }
        }

        if ( node.GetTargetScopeFromAnnotation() == TemplatingScope.RunTimeOnly &&
             node.GetScopeFromAnnotation().GetValueOrDefault().GetExpressionExecutionScope() == TemplatingScope.CompileTimeOnly )
        {
            // The node itself does not need to be transformed because it is compile time, but it needs to be converted
            // into a run-time value. However, calls to variants of Proceed must be transformed into calls to the standard Proceed.
            var transformedNode = base.VisitCore( node )!;

            return this.CreateRunTimeExpression( (ExpressionSyntax) transformedNode );
        }
        else
        {
            return base.VisitCore( node );
        }
    }

#if ROSLYN_5_0_0_OR_GREATER
    public override SyntaxNode? VisitFieldExpression( FieldExpressionSyntax node )
    {
        // Handle the C# 14 'field' keyword in property accessors.
        // Transform to a call to ITemplateSyntaxFactory.GetPropertyBackingField().
        return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetPropertyBackingField) ) )
            .WithAdditionalAnnotations( _userExpressionAnnotation );
    }
#endif

    public override SyntaxNode? VisitTupleExpression( TupleExpressionSyntax node )
    {
        var qualifiedTuple = this.AddTupleNames( node );

        return base.VisitTupleExpression( qualifiedTuple );
    }

    private TupleExpressionSyntax AddTupleNames( TupleExpressionSyntax node )
    {
        // Tuples can be initialized from variables and then items take names from variable name
        // but variable name is not safe and could be renamed because of target variables
        // in this case we initialize tuple with explicit names.
        var tupleType = (INamedTypeSymbol?) this._syntaxTreeAnnotationMap.GetExpressionType( node );

#pragma warning disable IDE0270 // Use coalesce expression
        if ( tupleType == null )
        {
            // We may fail to get the tuple type if it has an element with the `default` keyword, i.e. `(default, "")`.
            throw new AssertionFailedException( $"Cannot get the type of tuple '{node}'." );
        }
#pragma warning restore IDE0270 // Use coalesce expression

        var transformedArguments = new ArgumentSyntax[node.Arguments.Count];

        for ( var i = 0; i < tupleType.TupleElements.Length; i++ )
        {
            var tupleElement = tupleType.TupleElements[i];
            ArgumentSyntax arg;

            // Skip adding NameColon for DeclarationExpression (e.g., var first) because the name is already in the declaration.
            // Tuple element names are not permitted on the left side of a deconstruction.
            if ( node.Arguments[i].Expression.IsKind( SyntaxKind.DeclarationExpression ) )
            {
                arg = node.Arguments[i];
            }

            // If the tuple element has a name (i.e. it's not just ItemX), set it explicitly.
            else if ( !tupleElement.Name.Equals( tupleElement.CorrespondingTupleField!.Name, StringComparison.Ordinal ) )
            {
                arg = node.Arguments[i].WithNameColon( NameColon( tupleElement.Name ) );
            }
            else
            {
                arg = node.Arguments[i];
            }

            transformedArguments[i] = arg;
        }

        return node.WithArguments( SeparatedList( transformedArguments ) );
    }

    protected override ExpressionSyntax TransformAnonymousObjectCreationExpression( AnonymousObjectCreationExpressionSyntax node )
    {
        var qualifiedAnonymousObject = this.AddAnonymousObjectNames( node );

        return base.TransformAnonymousObjectCreationExpression( qualifiedAnonymousObject );
    }

    private AnonymousObjectCreationExpressionSyntax AddAnonymousObjectNames( AnonymousObjectCreationExpressionSyntax node )
    {
        var anonymousType = (INamedTypeSymbol?) this._syntaxTreeAnnotationMap.GetExpressionType( node )
                            ?? throw new AssertionFailedException( $"Cannot get the type of anonymous type '{node}'." );

        var transformedInitializers = new AnonymousObjectMemberDeclaratorSyntax[node.Initializers.Count];

        var properties = anonymousType.GetMembers().OfType<IPropertySymbol>().ToArray();

        for ( var i = 0; i < properties.Length; i++ )
        {
            transformedInitializers[i] = node.Initializers[i].WithNameEquals( NameEquals( properties[i].Name ) );
        }

        return node.WithInitializers( SeparatedList( transformedInitializers ) );
    }

    protected override ExpressionSyntax Transform( SyntaxToken token )
    {
        if ( token.IsKind( SyntaxKind.IdentifierToken ) && token.Parent != null )
        {
            // Transforms identifier declarations (local variables and local functions). Local identifiers must have
            // a unique name in the target code, which is unknown when the template is compiled, therefore local identifiers
            // get their name dynamically at expansion time. The ReserveRunTimeSymbolName method generates code that
            // reserves the name at expansion time. The result is stored in a local variable of the expanded template.
            // Then, each template reference uses this local variable.

            var identifierSymbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( token.Parent! );

            if ( IsLocalSymbol( identifierSymbol ) )
            {
                if ( identifierSymbol?.Kind == SymbolKind.Parameter && identifierSymbol is IParameterSymbol { Name: "_" } )
                {
                    // If we have a discard parameter (or a pseudo-discard one, just by naming conventions).
                    // Formally, it may be a usable parameter and we may need to map it,
                    // but it's better in general not to do so and to let the user cope with the consequences of conflicts.

                    return this.MetaSyntaxFactory.Identifier(
                        SyntaxFactoryEx.Default,
                        this.MetaSyntaxFactory.Kind( SyntaxKind.UnderscoreToken ),
                        SyntaxFactoryEx.LiteralExpression( "_" ),
                        SyntaxFactoryEx.LiteralExpression( "_" ),
                        SyntaxFactoryEx.Default );
                }

                if ( !this._currentMetaContext!.TryGetRunTimeSymbolLocal( identifierSymbol!, out var declaredSymbolNameLocal ) )
                {
                    // It is the first time we are seeing this local symbol, so we reserve a name for it.

                    declaredSymbolNameLocal = this.ReserveRunTimeSymbolName( identifierSymbol! ).Identifier;

                    this._currentMetaContext.AddRunTimeSymbolLocal( identifierSymbol!, declaredSymbolNameLocal );
                }

#pragma warning disable LAMA0850 // Intentional use: declaredSymbolNameLocal is a well-known generated identifier
                return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.EscapeIdentifier) ) )
                    .AddArgumentListArguments( Argument( IdentifierName( declaredSymbolNameLocal.Text ) ) );
#pragma warning restore LAMA0850
            }
            else if ( token.HasMetaVariableAnnotation() )
            {
#pragma warning disable LAMA0850 // Intentional use: passing SyntaxToken
                return IdentifierName( token );
#pragma warning restore LAMA0850
            }
            else
            {
                // This is not a symbol declaration but a symbol reference.
            }
        }

        var transformedToken = base.Transform( token );

        var tokenKind = transformedToken.SyntaxKind.ToString();

        if ( tokenKind.EndsWith( "Keyword", StringComparison.Ordinal ) )
        {
            transformedToken = transformedToken.WithTrailingTrivia( ElasticSpace );
        }

        return transformedToken;
    }

    protected override ExpressionSyntax TransformVariableDeclaration( VariableDeclarationSyntax node )
    {
        switch ( node )
        {
            case { Type: NullableTypeSyntax { ElementType: IdentifierNameSyntax { Identifier.Text: "dynamic" } } }:
                // Variable of dynamic? type needs to become var type (without the ?).
                return base.TransformVariableDeclaration(
                    VariableDeclaration(
                        SyntaxFactoryEx.VarIdentifier(),
                        node.Variables ) );

            default:
                return base.TransformVariableDeclaration( node );
        }
    }

    protected override ExpressionSyntax TransformIdentifierName( IdentifierNameSyntax node )
    {
        switch ( node.Identifier.Kind() )
        {
            case SyntaxKind.GlobalKeyword:
            case SyntaxKind.VarKeyword:
                return base.TransformIdentifierName( node );

            case SyntaxKind.IdentifierToken:
                return this.TransformIdentifierToken( node );

            default:
                throw new AssertionFailedException( $"Unexpected identifier kind: {node.Identifier.Kind()}." );
        }
    }

    /// <summary>
    /// Determines is a symbol is local to the current template.
    /// </summary>
    private static bool IsLocalSymbol( ISymbol? symbol )
        => symbol switch
        {
            IMethodSymbol { MethodKind: MethodKind.LocalFunction or MethodKind.AnonymousFunction } or ILocalSymbol => true,
            IParameterSymbol or ITypeParameterSymbol => IsLocalSymbol( symbol.ContainingSymbol ),
            _ => false
        };

    protected override ExpressionSyntax TransformNullableType( NullableTypeSyntax node )
    {
        if ( node.ElementType.IsKind( SyntaxKind.IdentifierName ) && node.ElementType is IdentifierNameSyntax identifier )
        {
            if ( string.Equals( identifier.Identifier.Text, "dynamic", StringComparison.Ordinal ) )
            {
                // Avoid transforming "dynamic?" into "var?".
                return base.TransformIdentifierName( SyntaxFactoryEx.VarIdentifier() );
            }
            else if ( this._templateCompileTimeTypeParameterNames.Contains( identifier.Identifier.ValueText ) )
            {
                // Avoid transforming "T?" into e.g. "string??" or "int??".

                // Note that this implementation means that templates behave differently than regular C#.
                // In C# with unconstrained T substituted with a value type turns T? into e.g. int.
                // In templates, T? turns into e.g. int?.

                // T.Type.IsNullable == true
#pragma warning disable LAMA0850 // Intentional use: identifier.Identifier is a SyntaxToken, nameof() is well-known
                var isNullableType = BinaryExpression(
                    SyntaxKind.EqualsExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName( identifier.Identifier ),
                            IdentifierName( nameof(TemplateTypeArgument.Type) ) ),
                        IdentifierName( nameof(IType.IsNullable) ) ),
                    SyntaxFactoryEx.LiteralExpression( true ) );
#pragma warning restore LAMA0850

                return ConditionalExpression( isNullableType, this.Transform( node.ElementType ), base.TransformNullableType( node ) );
            }
        }

        return base.TransformNullableType( node );
    }

    private ExpressionSyntax TransformIdentifierToken( IdentifierNameSyntax node )
    {
        if ( string.Equals( node.Identifier.Text, "dynamic", StringComparison.Ordinal ) )
        {
            // We change all dynamic into var in the template.
            return base.TransformIdentifierName( SyntaxFactoryEx.VarIdentifier() );
        }

        // If the identifier is declared within the template, the expanded name is given by the TemplateExpansionContext and
        // stored in a template variable named __foo, where foo is the variable name in the template. This variable is defined
        // and initialized in the VisitVariableDeclarator.
        // For identifiers declared outside of the template we just call the regular Roslyn SyntaxFactory.IdentifierName().
        var identifierSymbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

        if ( IsLocalSymbol( identifierSymbol ) )
        {
            if ( this._currentMetaContext!.TryGetRunTimeSymbolLocal( identifierSymbol!, out var declaredSymbolNameLocal ) )
            {
                // Some identifier names must be escaped when used in a different context than the template.
                return this.MetaSyntaxFactory.IdentifierName(
                    InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.EscapeIdentifier) ) )
                        .AddArgumentListArguments( Argument( SyntaxFactoryEx.WellKnownIdentifierName( declaredSymbolNameLocal ) ) ) );
            }
            else if ( identifierSymbol?.Kind == SymbolKind.Parameter && identifierSymbol is IParameterSymbol parameterSymbol
                                                                     && SymbolEqualityComparer.Default.Equals(
                                                                         parameterSymbol.ContainingSymbol,
                                                                         this._rootTemplateSymbol ) )
            {
                // We have a reference to a template parameter. Currently, only introductions can have template parameters, and these don't need
                // to be renamed.
            }
            else
            {
                // That should not happen in a correct compilation because IdentifierName is used only for an identifier reference, not an identifier definition.
                // Identifier definitions should be processed by Transform(SyntaxToken).

                // However, this can happen in an incorrect/incomplete compilation. In this case, returning anything is fine.
                this.Report(
                    TemplatingDiagnosticDescriptors.UndeclaredRunTimeIdentifier.CreateRoslynDiagnostic(
                        this._syntaxTreeAnnotationMap.GetLocation( node ),
                        node.Identifier.Text ) );

                this.Success = false;
            }
        }
        else if ( node.Identifier.HasMetaVariableAnnotation() )
        {
            return this.MetaSyntaxFactory.IdentifierName( node );
        }

        return base.TransformIdentifierName( node );
    }

    protected override ExpressionSyntax TransformArgument( ArgumentSyntax node )
    {
        // The base implementation is very verbose, so we use this one:
        if ( node.RefKindKeyword.IsKind( SyntaxKind.None ) )
        {
            var transformedExpression = this.Transform( node.Expression );
            var transformedArgument = this.MetaSyntaxFactory.Argument( SyntaxFactoryEx.Null, SyntaxFactoryEx.Default, transformedExpression );

            if ( node.NameColon != null )
            {
                var transformedNameColon = this.TransformNameColon( node.NameColon );

                transformedArgument =
                    InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                transformedArgument,
                                SyntaxFactoryEx.WellKnownIdentifierName( "WithNameColon" ) ) )
                        .WithArgumentList( ArgumentList( SingletonSeparatedList( Argument( transformedNameColon ) ) ) );
            }

            return transformedArgument.WithTemplateAnnotationsFrom( node );
        }
        else
        {
            return base.TransformArgument( node );
        }
    }

    protected override ExpressionSyntax TransformStatement( StatementSyntax statement )
        =>

            // We can get here when the parent node is a run-time `if` or `foreach` and the current node a compile-time statement
            // that is not a block. The easiest approach is to wrap the statement into a block.
            (ExpressionSyntax) this.BuildRunTimeBlock( Block( statement ), true );

    protected override ExpressionSyntax TransformExpression( ExpressionSyntax expression ) => this.CreateRunTimeExpression( expression );

    /// <summary>
    /// Transforms an <see cref="ExpressionSyntax"/> to an <see cref="ExpressionSyntax"/> that represents the input.
    /// </summary>
    private ExpressionSyntax CreateRunTimeExpression( ExpressionSyntax expression )
    {
        if ( expression.HasAnnotation( _userExpressionAnnotation ) )
        {
            // The expression is already a compile-time user expression.
            return expression;
        }

        switch ( expression.Kind() )
        {
            // TODO: We need to transform null and default values though. How to do this right then?
            case SyntaxKind.NullLiteralExpression:
            case SyntaxKind.DefaultLiteralExpression:
                return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                    .AddArgumentListArguments( Argument( this.MetaSyntaxFactory.LiteralExpression( this.Transform( expression.Kind() ) ) ) );

            case SyntaxKind.DefaultExpression:
                return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                    .AddArgumentListArguments(
                        Argument( this.MetaSyntaxFactory.DefaultExpression( (ExpressionSyntax) this.Visit( ((DefaultExpressionSyntax) expression).Type )! ) ) );

            case SyntaxKind.IdentifierName:
                {
                    var identifierName = (IdentifierNameSyntax) expression;

                    if ( identifierName.IsVar )
                    {
                        return this.TransformIdentifierName( (IdentifierNameSyntax) expression );
                    }

                    break;
                }

            case SyntaxKind.InvocationExpression:
                {
                    var typeOfAnnotation = expression.GetAnnotations( _rewrittenTypeOfAnnotation ).FirstOrDefault();

                    if ( typeOfAnnotation != null )
                    {
                        return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.TypeOf) ) )
                            .AddArgumentListArguments(
                                Argument( SyntaxFactoryEx.LiteralExpression( typeOfAnnotation.Data! ) ),
                                Argument(
                                    this.CreateTypeParameterSubstitutionDictionary(
                                        nameof(TemplateTypeArgument.SyntaxWithoutNullabilityAnnotations),
                                        this._dictionaryOfTypeSyntaxType ) ) );
                    }

                    break;
                }

            case SyntaxKind.SimpleLambdaExpression:
                break;

            case SyntaxKind.ThisExpression:
                // Cannot use 'this' in a context that expects a run-time expression.
                var location = this._syntaxTreeAnnotationMap.GetLocation( expression );

                // Find a meaningful parent exception.
                var parentExpression = expression
                                           .Ancestors()
                                           .FirstOrDefault(
                                               n => n.Kind() is SyntaxKind.InvocationExpression or SyntaxKind.AddExpression or SyntaxKind.SubtractExpression
                                                   or SyntaxKind.MultiplyExpression or SyntaxKind.DivideExpression or SyntaxKind.ModuloExpression
                                                   or SyntaxKind.LeftShiftExpression or SyntaxKind.RightShiftExpression
                                                   or SyntaxKind.UnsignedRightShiftExpression or SyntaxKind.LogicalOrExpression
                                                   or SyntaxKind.LogicalAndExpression or SyntaxKind.BitwiseOrExpression or SyntaxKind.BitwiseAndExpression
                                                   or SyntaxKind.ExclusiveOrExpression or SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression
                                                   or SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression or SyntaxKind.GreaterThanExpression
                                                   or SyntaxKind.GreaterThanOrEqualExpression or SyntaxKind.IsExpression or SyntaxKind.AsExpression
                                                   or SyntaxKind.CoalesceExpression )
                                       ?? expression;

                this.Report( TemplatingDiagnosticDescriptors.CannotUseThisInRunTimeContext.CreateRoslynDiagnostic( location, parentExpression.ToString() ) );

                return expression;

            case SyntaxKind.TypeOfExpression:
                {
                    var type = (ITypeSymbol) this._syntaxTreeAnnotationMap.GetSymbol( ((TypeOfExpressionSyntax) expression).Type ).AssertSymbolNotNull();
                    var typeOfString = this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOfExpression( type ).ToString();

                    return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.TypeOf) ) )
                        .AddArgumentListArguments(
                            Argument( SyntaxFactoryEx.LiteralExpression( typeOfString ) ),
                            Argument(
                                this.CreateTypeParameterSubstitutionDictionary(
                                    nameof(TemplateTypeArgument.SyntaxWithoutNullabilityAnnotations),
                                    this._dictionaryOfTypeSyntaxType ) ) );
                }
        }

        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( expression );

        // Get the expression type. Sometime it fails: this seems to happen with lambda expressions in a method that cannot
        // be resolved.
        var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( expression )
                             ?? this._runTimeCompilation.GetSpecialType( SpecialType.System_Object );

        if ( symbol?.Kind == SymbolKind.Parameter && symbol is IParameterSymbol parameter
                                                  && this._templateMemberClassifier.IsRunTimeTemplateParameter( parameter ) )
        {
            // Run-time template parameters are always bound to a run-time meta-expression.
            return expression;
        }
        else if ( symbol?.Kind == SymbolKind.TypeParameter && symbol is ITypeParameterSymbol typeParameter
                                                           && this._templateMemberClassifier.IsCompileTimeTemplateTypeParameter( typeParameter ) )
        {
            return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                expression,
                SyntaxFactoryEx.WellKnownIdentifierName( nameof(TemplateTypeArgument.Syntax) ) );
        }

        // A local function that wraps the input `expression` into a LiteralExpression.
        ExpressionSyntax CreateRunTimeExpressionForLiteralCreateExpressionFactory( SyntaxKind syntaxKind )
        {
            InvocationExpressionSyntax literalExpression;

            if ( syntaxKind == SyntaxKind.StringLiteralExpression )
            {
                literalExpression = InvocationExpression(
                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.StringLiteralExpression) ) )
                    .AddArgumentListArguments( Argument( expression ) );
            }
            else
            {
                // Try to preserve the original literal text (e.g., "0xff" instead of "255").
                var originalLiteralText = this.TryGetOriginalLiteralText( expression );

                var literalToken = originalLiteralText != null
                    ? this.MetaSyntaxFactory.LiteralWithText( originalLiteralText, expression )
                    : this.MetaSyntaxFactory.Literal( expression );

                literalExpression = this.MetaSyntaxFactory.LiteralExpression(
                    this.Transform( syntaxKind ),
                    literalToken );
            }

            return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                .AddArgumentListArguments(
                    Argument( literalExpression ),
                    Argument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal( expressionType.GetSerializableTypeId().Id ) ) ) );
        }

        if ( expressionType.Kind == SymbolKind.ErrorType && expressionType is IErrorTypeSymbol )
        {
            // There is a compile-time error. Return default.
            return LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) );
        }

        bool ExpressionTypeIsGenericDynamic()
        {
            return expressionType.Kind == SymbolKind.NamedType && expressionType is INamedTypeSymbol { TypeArguments: [IDynamicTypeSymbol] };
        }

        // ReSharper disable once ConstantConditionalAccessQualifier
        switch ( expressionType.Name )
        {
            case "dynamic":
            case "Task"
                when ExpressionTypeIsGenericDynamic() &&
                     expressionType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks":
            case "ConfiguredTaskAwaitable"
                when ExpressionTypeIsGenericDynamic() &&
                     expressionType.ContainingNamespace.ToDisplayString() == "System.Runtime.CompilerServices":
            case "IEnumerable" or "IEnumerator" or "IAsyncEnumerable" or "IAsyncEnumerator"
                when ExpressionTypeIsGenericDynamic() &&
                     expressionType.ContainingNamespace.ToDisplayString() == "System.Collections.Generic":

                return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetDynamicSyntax) ) )
                    .AddArgumentListArguments( Argument( expression.WithoutTrivia() ) );

            case "String":
                return CreateRunTimeExpressionForLiteralCreateExpressionFactory( SyntaxKind.StringLiteralExpression );

            case "Int32":
            case "Int16":
            case "Int64":
            case "UInt32":
            case "UInt16":
            case "UInt64":
            case "Byte":
            case "SByte":
            case nameof(Single):
            case nameof(Double):
            case nameof(Decimal):
                return CreateRunTimeExpressionForLiteralCreateExpressionFactory( SyntaxKind.NumericLiteralExpression );

            case nameof(Char):
                return CreateRunTimeExpressionForLiteralCreateExpressionFactory( SyntaxKind.CharacterLiteralExpression );

            case nameof(Boolean):
                return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                    .AddArgumentListArguments(
                        Argument(
                            InvocationExpression( this.MetaSyntaxFactory.SyntaxFactoryMethod( nameof(LiteralExpression) ) )
                                .AddArgumentListArguments(
                                    Argument(
                                        InvocationExpression(
                                                this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.Boolean) ) )
                                            .AddArgumentListArguments( Argument( expression ) ) ) ) ),
                        Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( "typeof(bool)" ) ) ) );

            case null:
                throw new AssertionFailedException( $"Cannot convert {expression.Kind()} '{expression}' to a run-time value." );

            default:
                // If it's an IExpression, it can be returned as a TypedExpressionSyntax.
                if ( this._runTimeCompilation.HasImplicitConversion( expressionType, this._iExpressionSymbol ) )
                {
                    return InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetTypedExpression) ) )
                        .AddArgumentListArguments( Argument( expression ) );
                }

                // Try to find a serializer for this type. If the object type is simply 'object', we will resolve it at expansion time.
                if ( expressionType.SpecialType == SpecialType.System_Object || this._serializableTypes.IsSerializable(
                        expressionType,
                        this._syntaxTreeAnnotationMap.GetLocation( expression ),
                        this ) )
                {
                    return InvocationExpression(
                        this._templateMetaSyntaxFactory.GenericTemplateSyntaxFactoryMember(
                            nameof(ITemplateSyntaxFactory.Serialize),
                            this.MetaSyntaxFactory.Type( expressionType ) ),
                        ArgumentList( SingletonSeparatedList( Argument( expression ) ) ) );
                }
                else
                {
                    // We don't have a valid tree, but let the compilation continue. The call to IsSerializable wrote a diagnostic.
                    return LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) );
                }
        }
    }

    /// <summary>
    /// Tries to find the original literal text for a compile-time expression. This allows preserving the source form
    /// of numeric literals (e.g., <c>0xff</c>, <c>0b1010</c>, <c>42L</c>) when they are serialized back to run-time code.
    /// </summary>
    private string? TryGetOriginalLiteralText( ExpressionSyntax expression )
    {
        return TryGetLiteralTextFromExpression( expression )
               ?? TryGetLiteralTextFromDeclaration( expression );

        string? TryGetLiteralTextFromExpression( ExpressionSyntax expr )
        {
            // Direct literal expression.
            if ( expr.IsKind( SyntaxKind.NumericLiteralExpression )
                 && expr is LiteralExpressionSyntax { Token.RawKind: (int) SyntaxKind.NumericLiteralToken } literal )
            {
                return literal.Token.Text;
            }

            return null;
        }

        string? TryGetLiteralTextFromDeclaration( ExpressionSyntax expr )
        {
            // For an identifier, look up the declaring variable and check its initializer.
            if ( expr is not IdentifierNameSyntax identifier )
            {
                return null;
            }

            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( expr );

            if ( symbol is not ILocalSymbol localSymbol )
            {
                return null;
            }

            foreach ( var syntaxRef in localSymbol.DeclaringSyntaxReferences )
            {
                if ( syntaxRef.GetSyntax() is not VariableDeclaratorSyntax declarator )
                {
                    continue;
                }

                var initValue = declarator.Initializer?.Value;

                if ( initValue == null )
                {
                    continue;
                }

                // Safety check: don't preserve literal text if the variable might be modified after declaration
                // (e.g., passed as ref/out, assigned to, incremented, etc.).
                if ( IsVariablePotentiallyMutated( identifier.Identifier.ValueText, declarator ) )
                {
                    return null;
                }

                // Direct literal initializer: var x = 0xff;
                var literalText = TryGetLiteralTextFromExpression( initValue );

                if ( literalText != null )
                {
                    return literalText;
                }

                // Unwrap meta.CompileTime(literal) or similar single-argument invocations.
                if ( initValue.IsKind( SyntaxKind.InvocationExpression )
                     && initValue is InvocationExpressionSyntax { ArgumentList.Arguments: [var singleArg] } )
                {
                    literalText = TryGetLiteralTextFromExpression( singleArg.Expression );

                    if ( literalText != null )
                    {
                        return literalText;
                    }
                }

                // Unwrap cast expressions: (int)0xff
                if ( initValue.IsKind( SyntaxKind.CastExpression ) && initValue is CastExpressionSyntax castExpr )
                {
                    literalText = TryGetLiteralTextFromExpression( castExpr.Expression );

                    if ( literalText != null )
                    {
                        return literalText;
                    }
                }
            }

            return null;
        }

        // Check if a variable could be modified after its declaration within the enclosing method body.
        static bool IsVariablePotentiallyMutated( string variableName, VariableDeclaratorSyntax declarator )
        {
            // Find the enclosing block or method body.
            var enclosingBlock = declarator.FirstAncestorOrSelf<BlockSyntax>();

            if ( enclosingBlock == null )
            {
                return true; // Assume mutable if we can't determine.
            }

            var declaratorSpanEnd = declarator.Span.End;

            foreach ( var descendant in enclosingBlock.DescendantNodes() )
            {
                // Only check nodes after the declaration.
                if ( descendant.SpanStart <= declaratorSpanEnd )
                {
                    continue;
                }

                if ( descendant is not IdentifierNameSyntax id || id.Identifier.ValueText != variableName )
                {
                    continue;
                }

                var parent = id.Parent;

                // Check for assignment: variable = ..., variable += ..., etc.
                if ( parent is AssignmentExpressionSyntax assignment && assignment.Left == id )
                {
                    return true;
                }

                // Check for ref/out argument: Method(ref variable), Method(out variable).
                if ( parent is ArgumentSyntax { RefOrOutKeyword.RawKind: not (int) SyntaxKind.None } )
                {
                    return true;
                }

                // Check for increment/decrement: variable++, ++variable, variable--, --variable.
                if ( parent is PostfixUnaryExpressionSyntax or PrefixUnaryExpressionSyntax )
                {
                    var parentKind = parent.Kind();

                    if ( parentKind is SyntaxKind.PostIncrementExpression or SyntaxKind.PostDecrementExpression
                         or SyntaxKind.PreIncrementExpression or SyntaxKind.PreDecrementExpression )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    private ExpressionSyntax CreateTypeParameterSubstitutionDictionary( string propertyName, TypeSyntax dictionaryType )
    {
        if ( this._templateCompileTimeTypeParameterNames.Count == 0 )
        {
            return SyntaxFactoryEx.Null;
        }
        else
        {
            return ObjectCreationExpression( dictionaryType )
                .WithInitializer(
                    InitializerExpression(
                        SyntaxKind.ObjectInitializerExpression,
                        SeparatedList<ExpressionSyntax>(
                            this._templateCompileTimeTypeParameterNames.SelectAsReadOnlyCollection(
                                name =>
                                    AssignmentExpression(
                                        SyntaxKind.SimpleAssignmentExpression,
                                        ImplicitElementAccess()
                                            .WithArgumentList(
                                                BracketedArgumentList(
                                                    SingletonSeparatedList(
                                                        Argument(
                                                            LiteralExpression(
                                                                SyntaxKind
                                                                    .StringLiteralExpression,
                                                                Literal( name ) ) ) ) ) ),
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            SyntaxFactoryEx.SafeIdentifierName( name ),
                                            SyntaxFactoryEx.WellKnownIdentifierName( propertyName ) ) ) ) ) ) )
                .NormalizeWhitespace();
        }
    }

    public override SyntaxNode? VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
    {
        if ( this.TryVisitNamespaceOrTypeName( node, out var transformedNode ) )
        {
            return transformedNode;
        }

        if ( this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression )?.Kind == SymbolKind.DynamicType
             && this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression ) is IDynamicTypeSymbol
             && !this._templateMemberClassifier.IsTemplateParameter( node.Expression ) )
        {
            // We have a member access of a dynamic expression.
            return InvocationExpression(
                this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DynamicMemberAccessExpression) ),
                ArgumentList(
                    SeparatedList(
                    [
                        Argument( this.ConvertToUserExpression( (ExpressionSyntax) this.Visit( node.Expression )! ) ),
                        Argument( SyntaxFactoryEx.LiteralExpression( node.Name.Identifier.ValueText ) )
                    ] ) ) );
        }

        var transformationKind = this.GetTransformationKind( node.Expression );

        if ( transformationKind == TransformationKind.Transform )
        {
            if ( this._syntaxTreeAnnotationMap.GetSymbol( node )?.Kind == SymbolKind.Method
                 && this._syntaxTreeAnnotationMap.GetSymbol( node ) is IMethodSymbol { ReducedFrom: not null } method )
            {
                this.Report(
                    TemplatingDiagnosticDescriptors.ExtensionMethodMethodGroupConversion.CreateRoslynDiagnostic( node.GetDiagnosticLocation(), method ) );
            }
        }

        return base.VisitMemberAccessExpression( node );
    }

    protected override ExpressionSyntax TransformConditionalAccessExpression( ConditionalAccessExpressionSyntax node )
    {
        var transformationKind = this.GetTransformationKind( node.Expression );

        if ( transformationKind != TransformationKind.Transform
             && this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression )?.Kind == SymbolKind.DynamicType
             && this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression ) is IDynamicTypeSymbol )
        {
            return InvocationExpression(
                this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ConditionalAccessExpression) ),
                ArgumentList( SeparatedList( [Argument( this.Transform( node.Expression ) ), Argument( this.Transform( node.WhenNotNull ) )] ) ) );
        }

        // Expand extension methods.
        if ( this.ProcessConditionalAccessExtensionMethod( node ) is { } expressions )
        {
            // Turns e.g. `a?.Foo()` into `a is {} x ? x.Foo() : null`.
            var result = ConditionalExpression( expressions.Condition, expressions.Invocation, SyntaxFactoryEx.Null );

            return this.TransformConditionalExpression( result );
        }

        // Detect unsupported pattern: compile-time expression ?. compile-time-returning-run-time member.
        // For example, meta.Target.Parameters.FirstOrDefault(...)?.Value where the expression is compile-time
        // and .Value is a compile-time member returning a run-time value. When the compile-time expression is null,
        // its type is unknown, so type-preserving run-time code cannot be generated.
        if ( transformationKind != TransformationKind.Transform )
        {
            var firstMemberBinding = node.WhenNotNull.Kind() switch
            {
                SyntaxKind.MemberBindingExpression => (MemberBindingExpressionSyntax) node.WhenNotNull,
                SyntaxKind.ConditionalAccessExpression
                    when ((ConditionalAccessExpressionSyntax) node.WhenNotNull).Expression.Kind() == SyntaxKind.MemberBindingExpression
                    => (MemberBindingExpressionSyntax) ((ConditionalAccessExpressionSyntax) node.WhenNotNull).Expression,
                _ => null
            };

            if ( firstMemberBinding != null
                 && firstMemberBinding.GetScopeFromAnnotation().GetValueOrDefault().IsCompileTimeMemberReturningRunTimeValue() )
            {
                this.Report(
                    TemplatingDiagnosticDescriptors.CannotUseConditionalAccessWithCompileTimeToRunTimeMember.CreateRoslynDiagnostic(
                        this._syntaxTreeAnnotationMap.GetLocation( node ),
                        (node.ToString(), node.Expression.ToString(), firstMemberBinding.Name.ToString()) ) );

                return node;
            }
        }

        return base.TransformConditionalAccessExpression( node );
    }

    private (ExpressionSyntax Condition, ExpressionSyntax Invocation)? ProcessConditionalAccessExtensionMethod(
        ConditionalAccessExpressionSyntax conditionalAccessExpression )
    {
        var memberBinding = conditionalAccessExpression.DescendantNodes().OfType<MemberBindingExpressionSyntax>().FirstOrDefault();
        var symbol = memberBinding == null ? null : this._syntaxTreeAnnotationMap.GetSymbol( memberBinding );

        if ( symbol is not IMethodSymbol { IsExtensionMethod: true } )
        {
            return null;
        }

        var name = this._syntaxTreeAnnotationMap.GetExpressionType( conditionalAccessExpression.Expression )?.Name;

        if ( string.IsNullOrEmpty( name ) )
        {
            name = conditionalAccessExpression.Expression.ToString();
        }

        var variable = this.ReserveRunTimeVariableName( name.ToIdentifier().ToCamelCase() );

        var whenNotNull = (ExpressionSyntax) new RemoveConditionalAccessRewriter( variable ).Visit( conditionalAccessExpression.WhenNotNull ).AssertNotNull();

        // For e.g. `a?.Foo()` returns `a is {} x` and `x.Foo()`.
        return (IsPatternExpression(
                        conditionalAccessExpression.Expression,
                        RecursivePattern()
                            .WithPropertyPatternClause( PropertyPatternClause() )
                            .WithDesignation( SingleVariableDesignation( variable.Identifier ) ) )
                    .AddScopeAnnotation( TemplatingScope.RunTimeOnly ),
                whenNotNull);
    }

    public override SyntaxNode? VisitExpressionStatement( ExpressionStatementSyntax node )
    {
        // The default implementation has to be overridden because VisitInvocationExpression can
        // return null in case of pragma. In this case, the ExpressionStatement must return null too.
        // In the default implementation, such case would result in an exception.

        bool IsSubtemplateCall()
        {
            return this._syntaxTreeAnnotationMap.GetInvocableSymbol( node.Expression ) is { } symbol
                   && this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol ).CanBeReferencedAsSubtemplate;
        }

        if ( this.GetTransformationKind( node ) == TransformationKind.Transform
             || (this._templateMemberClassifier.IsNodeOfDynamicType( node.Expression ) && !IsSubtemplateCall()) )
        {
            return this.TransformExpressionStatement( node );
        }
        else
        {
            var transformedExpression = this.Visit( node.Expression );

            if ( transformedExpression == null )
            {
                return null;
            }
            else
            {
                return node.Update(
                    this.VisitList( node.AttributeLists ),
                    (ExpressionSyntax) transformedExpression,
                    this.VisitToken( node.SemicolonToken ) );
            }
        }
    }

    protected override ExpressionSyntax TransformExpressionStatement( ExpressionStatementSyntax node )
    {
        if ( node.Expression.Kind() is SyntaxKind.SimpleAssignmentExpression or SyntaxKind.AddAssignmentExpression or SyntaxKind.SubtractAssignmentExpression
                 or SyntaxKind.MultiplyAssignmentExpression or SyntaxKind.DivideAssignmentExpression or SyntaxKind.ModuloAssignmentExpression
                 or SyntaxKind.AndAssignmentExpression or SyntaxKind.ExclusiveOrAssignmentExpression or SyntaxKind.OrAssignmentExpression
                 or SyntaxKind.LeftShiftAssignmentExpression or SyntaxKind.RightShiftAssignmentExpression or SyntaxKind.UnsignedRightShiftAssignmentExpression
                 or SyntaxKind.CoalesceAssignmentExpression
             && node.Expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifier } assignment )
        {
            var identifierSymbol = this._syntaxTreeAnnotationMap.GetSymbol( identifier );

            if ( IsLocalSymbol( identifierSymbol ) || (identifierSymbol?.Kind == SymbolKind.Discard && identifierSymbol is IDiscardSymbol) )
            {
                if ( this.IsCompileTimeDynamic( assignment.Right ) )
                {
                    // Process the statement "<local_or_discard> = meta.XXX()", where "meta.XXX()" is a call to a compile-time dynamic method. 

                    var invocationExpression = InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DynamicLocalAssignment) ) )
                        .AddArgumentListArguments(
                            Argument( this.Transform( identifier ) ),
                            Argument( this.MetaSyntaxFactory.Kind( assignment.Kind() ) ),
                            Argument( this.ConvertToUserExpression( (ExpressionSyntax) this.Visit( assignment.Right ).AssertNotNull() ) ),
                            Argument( LiteralExpression( SyntaxKind.FalseLiteralExpression ) ) );

                    return this.WithCallToAddSimplifierAnnotation( invocationExpression );
                }
                else if ( assignment.Right.IsKind( SyntaxKind.AwaitExpression ) && assignment.Right is AwaitExpressionSyntax awaitExpression
                                                                                && this.IsCompileTimeDynamic( awaitExpression.Expression ) )
                {
                    // Process the statement "<local_or_discard> = await meta.XXX()", where "meta.XXX()" is a call to a compile-time dynamic method. 

                    var invocationExpression = InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DynamicLocalAssignment) ) )
                        .AddArgumentListArguments(
                            Argument( this.Transform( identifier ) ),
                            Argument( this.MetaSyntaxFactory.Kind( assignment.Kind() ) ),
                            Argument( this.ConvertToUserExpression( (ExpressionSyntax) this.Visit( awaitExpression.Expression ).AssertNotNull() ) ),
                            Argument( LiteralExpression( SyntaxKind.TrueLiteralExpression ) ) );

                    return this.WithCallToAddSimplifierAnnotation( invocationExpression );
                }
            }
        }

        // Expand conditional access extension methods.
        if ( node.Expression.IsKind( SyntaxKind.ConditionalAccessExpression )
             && node.Expression is ConditionalAccessExpressionSyntax conditionalAccessExpression
             && this.ProcessConditionalAccessExtensionMethod( conditionalAccessExpression ) is { } expressions )
        {
            // Turns e.g. `a?.Foo();` into `if (a is {} x) x.Foo();`. 
            var result = IfStatement( expressions.Condition, ExpressionStatement( expressions.Invocation ).AddScopeAnnotation( TemplatingScope.RunTimeOnly ) );

            return this.TransformIfStatement( result );
        }

        var expression = this.Transform( node.Expression );

        var toStatementExpression = InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ToStatement) ),
            ArgumentList( SingletonSeparatedList( Argument( expression ) ) ) );

        return toStatementExpression;
    }

    public override SyntaxNode? VisitInvocationExpression( InvocationExpressionSyntax node )
    {
        var transformationKind = this.GetTransformationKind( node );

        if ( node.IsNameOf() )
        {
            // nameof is always transformed into a literal except when it is a template parameter.

            var expression = node.ArgumentList.Arguments[0].Expression;
            var argumentSymbol = this._syntaxTreeAnnotationMap.GetSymbol( expression );

            if ( argumentSymbol?.Kind == SymbolKind.Parameter && argumentSymbol is IParameterSymbol parameter
                                                              && this._templateMemberClassifier.IsRunTimeTemplateParameter( parameter ) )
            {
                if ( transformationKind == TransformationKind.Transform )
                {
                    return this.MetaSyntaxFactory.InvocationExpression(
                        this.MetaSyntaxFactory.IdentifierName(
                            this.MetaSyntaxFactory.Identifier(
                                SyntaxFactoryEx.Default,
                                this.MetaSyntaxFactory.Kind( SyntaxKind.NameOfKeyword ),
                                SyntaxFactoryEx.LiteralExpression( "nameof" ),
                                SyntaxFactoryEx.LiteralExpression( "nameof" ),
                                SyntaxFactoryEx.Default ) ),
                        this.MetaSyntaxFactory.ArgumentList(
                            this.MetaSyntaxFactory.SingletonSeparatedList<ArgumentSyntax>(
                                this.MetaSyntaxFactory.Argument( SyntaxFactoryEx.Default, SyntaxFactoryEx.Default, expression ) ) ) );
                }
                else
                {
                    // since expression references a parameter, we can just call ToString() on it
                    return InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            expression,
                            SyntaxFactoryEx.WellKnownIdentifierName( nameof(this.ToString) ) ) );
                }
            }

            var symbolName = argumentSymbol?.Name ?? "<error>";

            if ( transformationKind == TransformationKind.Transform )
            {
                return this.MetaSyntaxFactory.LiteralExpression(
                    this.MetaSyntaxFactory.Kind( SyntaxKind.StringLiteralExpression ),
                    this.MetaSyntaxFactory.Literal( symbolName ) );
            }
            else
            {
                return SyntaxFactoryEx.LiteralExpression( symbolName );
            }
        }
        else if ( this._compileTimeOnlyRewriter.TryRewriteProceedInvocation( node, out var proceedNode ) )
        {
            return proceedNode;
        }

        // Process special methods.
        switch ( this._templateMemberClassifier.GetMetaMemberKind( node.Expression ) )
        {
            case MetaMemberKind.InsertComment:
                {
                    var transformedArgumentList = this.VisitList( node.ArgumentList.Arguments );

                    // TemplateSyntaxFactory.AddComments( __s, comments );
                    this.AddTemplateSyntaxFactoryStatement( node, nameof(ITemplateSyntaxFactory.AddComments), transformedArgumentList.ToArray() );

                    return null;
                }

            case MetaMemberKind.InsertStatement:
                // TemplateSyntaxFactory.AddStatement( __s, statement );
                this.AddAddStatementStatement( node, node.ArgumentList.Arguments.Single().Expression );

                return null;

            case MetaMemberKind.InvokeTemplate:
                {
                    var transformedArgumentList = this.VisitList( node.ArgumentList.Arguments );

                    // TemplateSyntaxFactory.AddStatement( __s, TemplateSyntaxFactory.InvokeTemplate( ... ) );
                    this.AddAddStatementStatement(
                        node,
                        InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.InvokeTemplate) ) )
                            .AddArgumentListArguments( transformedArgumentList.ToArray() ) );

                    return null;
                }

            case MetaMemberKind.Return:
                {
                    var returnStatement = ReturnStatement( node.ArgumentList.Arguments.SingleOrDefault()?.Expression );

                    var transformedReturnStatement = (ExpressionSyntax) this.VisitReturnStatement( returnStatement );

                    this.AddAddStatementStatement( node, transformedReturnStatement );

                    return null;
                }

            case MetaMemberKind.DefineLocalVariable:
                {
                    var arguments = new List<ArgumentSyntax>
                    {
                        Argument( SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext.AssertNotNull().StatementListVariableName ) )
                    };

                    arguments.AddRange( node.ArgumentList.Arguments.SelectAsReadOnlyList( a => Argument( (ExpressionSyntax) this.Visit( a.Expression )! ) ) );

                    var invocationExpression = InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DefineLocalVariable) ) )
                        .AddArgumentListArguments( arguments.ToArray() );

                    return invocationExpression;
                }

            case MetaMemberKind.RunTime:
                return this.Transform( node.ArgumentList.Arguments[0].Expression );
        }

        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node.Expression );

        if ( symbol != null )
        {
            var templateInfo = this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol );

            if ( templateInfo.CanBeReferencedAsSubtemplate )
            {
                // We are calling a subtemplate.
                var compiledTemplateName = TemplateNameHelper.GetCompiledTemplateName( symbol );

                var transformedArguments = new List<ArgumentSyntax>( node.ArgumentList.Arguments.Count );
                var transformedOptionalArguments = new List<ArgumentSyntax>();

                foreach ( var argument in node.ArgumentList.Arguments )
                {
                    var modifiedArgument = argument;
                    var parameter = this._syntaxTreeAnnotationMap.GetParameterSymbol( argument ).AssertSymbolNotNull();

                    if ( argument.Expression is not LiteralExpressionSyntax && argument.Expression.GetScopeFromAnnotation() == TemplatingScope.RunTimeOnly )
                    {
                        // Run-time parameters are passed to subtemplates as expressions.
                        // If the subtemplate accessed that parameter multiple times, it would cause re-evaluation of the expression.
                        // Avoid that by storing the value in a run-time local variable.
                        var variableIdentifier = this.ReserveRunTimeSymbolName( parameter );

                        // T arg = expr;
                        var variableDeclaration = this.MetaSyntaxFactory.LocalDeclarationStatement(
                            SyntaxFactoryEx.Default,
                            SyntaxFactoryEx.Default,
                            this.MetaSyntaxFactory.VariableDeclaration(
                                this.Transform( this.MetaSyntaxFactory.Type( parameter.Type ) ),
                                this.MetaSyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(
                                    this.MetaSyntaxFactory.VariableDeclarator(
                                        variableIdentifier,
                                        SyntaxFactoryEx.Default,
                                        this.MetaSyntaxFactory.EqualsValueClause( this.Transform( argument.Expression ) ) ) ) ) );

                        this.AddAddStatementStatement( node, variableDeclaration );

                        modifiedArgument = argument.WithExpression( this.MetaSyntaxFactory.IdentifierName( variableIdentifier ) );
                    }

                    // Run standard transformations on the argument.
                    var transformedArgument = (ArgumentSyntax) this.VisitArgument( modifiedArgument );

                    // Run-time template parameters must be of type ExpressionSyntax, while these arguments of non-template methods would be of type IExpression.
                    if ( this._templateMemberClassifier.SymbolClassifier.GetTemplatingScope( parameter ).GetExpressionExecutionScope()
                         != TemplatingScope.CompileTimeOnly )
                    {
                        transformedArgument = transformedArgument.WithExpression(
                            InvocationExpression(
                                    this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ConvertToExpressionSyntax) ) )
                                .AddArgumentListArguments( Argument( transformedArgument.Expression ) ) );
                    }

                    if ( !parameter.IsOptional )
                    {
                        transformedArguments.Add( transformedArgument );
                    }
                    else
                    {
                        transformedOptionalArguments.Add( transformedArgument );
                    }
                }

                var (receiver, name) = node.Expression.Kind() switch
                {
                    { IsSimpleName: true } when node.Expression is SimpleNameSyntax simpleName => (null, simpleName),
                    SyntaxKind.SimpleMemberAccessExpression when node.Expression is MemberAccessExpressionSyntax memberAccess => (
                        this.Visit( memberAccess.Expression ).AssertCast<ExpressionSyntax>().AssertNotNull(), memberAccess.Name),
                    _ => throw new AssertionFailedException( $"Expression '{node.Expression}' has unexpected expression type {node.Expression.GetType()}." )
                };

                if ( !symbol.IsStatic && receiver is (not null) and (not ThisExpressionSyntax) )
                {
                    // Handle receiver side-effects by saving it into a variable.
                    var variableIdentifier = this._currentMetaContext!.GetVariable( symbol.Name );

                    var variableDeclaration = LocalDeclarationStatement(
                        VariableDeclaration( SyntaxFactoryEx.VarIdentifier() )
                            .AddVariables( VariableDeclarator( variableIdentifier, default, EqualsValueClause( receiver ) ) ) );

                    this._currentMetaContext.AddStatement( variableDeclaration );

                    receiver = SyntaxFactoryEx.WellKnownIdentifierName( variableIdentifier );
                }

                if ( name.IsKind( SyntaxKind.GenericName ) && name is GenericNameSyntax genericName )
                {
                    var i = 0;

                    var typeParameters = symbol.AssertCast<IMethodSymbol>().TypeParameters;

                    foreach ( var typeArgument in genericName.TypeArgumentList.Arguments )
                    {
                        var typeParameter = typeParameters[i];

                        // templateSyntaxFactory.TemplateTypeArgument("name", typeof(T))
                        var templateTypeArgumentExpression =
                            InvocationExpression(
                                    this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.TemplateTypeArgument) ) )
                                .AddArgumentListArguments(
                                    Argument( SyntaxFactoryEx.LiteralNonNullExpression( typeParameter.Name ) ),
                                    Argument( this.TransformCompileTimeCode<ExpressionSyntax>( TypeOfExpression( typeArgument ) ) ) );

                        transformedArguments.Add( Argument( templateTypeArgumentExpression ) );

                        i++;
                    }
                }

                foreach ( var transformedOptionalArgument in transformedOptionalArguments )
                {
                    transformedArguments.Add( transformedOptionalArgument );
                }

                ExpressionSyntax compiledTemplateExpression =
                    receiver == null
                        ? SyntaxFactoryEx.WellKnownIdentifierName( compiledTemplateName )
                        : MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            receiver,
                            SyntaxFactoryEx.WellKnownIdentifierName( compiledTemplateName ) );

                var templateProviderExpression = symbol.IsStatic switch
                {
                    // Called template is static and from the same type as current template, so preserve templateProvider.
                    true when symbol.ContainingType.Equals( this._rootTemplateSymbol?.ContainingType ) => SyntaxFactoryEx.Null,
                    true => TypeOfExpression( this.MetaSyntaxFactory.Type( symbol.ContainingType ) ),
                    false => receiver ?? ThisExpression()
                };

                // templateSyntaxFactory.ForTemplate("templateName", templateProvider)
                var templateSyntaxFactoryExpression = InvocationExpression(
                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ForTemplate) ) )
                    .AddArgumentListArguments( Argument( SyntaxFactoryEx.LiteralNonNullExpression( symbol.Name ) ), Argument( templateProviderExpression ) );

                var templateInvocationExpression = InvocationExpression( compiledTemplateExpression )
                    .AddArgumentListArguments( Argument( templateSyntaxFactoryExpression ) )
                    .AddArgumentListArguments( transformedArguments.ToArray() );

                this.AddAddStatementStatement( node, CastExpression( this.MetaSyntaxFactory.Type( typeof(StatementSyntax) ), templateInvocationExpression ) );

                return null;
            }
        }
/*
        if ( transformationKind != TransformationKind.Transform &&
             node.ArgumentList.Arguments.Any( this._templateMemberClassifier.IsDynamicParameter ) )
        {
            // We are transforming a call to a compile-time method that accepts dynamic arguments.

            ArgumentSyntax LocalTransformArgument( ArgumentSyntax a )
            {
                if ( this._templateMemberClassifier.IsDynamicParameter( a ) )
                {
                    var expressionScope = a.Expression.GetScopeFromAnnotation().GetValueOrDefault();
                    var transformedExpression = (ExpressionSyntax) this.Visit( a.Expression )!;

                    switch ( expressionScope )
                    {
                        case TemplatingScope.Dynamic:
                        case TemplatingScope.RunTimeOnly:
                            var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( a.Expression );

                            if ( expressionType != null )
                            {
                                var typedExpression = InvocationExpression(
                                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                                    .AddArgumentListArguments(
                                        Argument( transformedExpression ),
                                        Argument(
                                            LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( expressionType.GetSerializableTypeId().Id ) ) ) );

                                transformedExpression = typedExpression;
                            }

                            break;

                        default:
                            transformedExpression = this.CreateRunTimeExpression( transformedExpression );

                            break;
                    }

                    return a.WithExpression( transformedExpression );
                }
                else
                {
                    return this.VisitArgument( a ).AssertCast<ArgumentSyntax>();
                }
            }

            var transformedArguments = node.ArgumentList.Arguments.SelectAsImmutableArray( LocalTransformArgument );

            return node.Update(
                (ExpressionSyntax) this.Visit( node.Expression )!,
                ArgumentList( SeparatedList( transformedArguments ) ) );
        }*/
        else if ( this._templateMemberClassifier.IsNodeOfDynamicType( node.Expression ) )
        {
            // We are in an invocation like: `meta.This.Foo(...)`.
        }
        else if ( this._templateMemberClassifier.IsRunTimeMethod( node.Expression ) )
        {
            // Replace `meta.RunTime(x)` to `x`.
            var expression = node.ArgumentList.Arguments[0].Expression;

            if ( this.GetTransformationKind( expression ) == TransformationKind.None )
            {
                return this.CreateRunTimeExpression( expression );
            }
            else
            {
                return this.Visit( expression );
            }
        }

        // Expand extension methods.
        if ( transformationKind == TransformationKind.Transform )
        {
            if ( symbol?.Kind == SymbolKind.Method && symbol is IMethodSymbol { ReducedFrom: not null } method )
            {
                if ( node.Expression.IsKind( SyntaxKind.SimpleMemberAccessExpression )
                     && node.Expression is MemberAccessExpressionSyntax memberAccessExpression )
                {
                    var receiver = memberAccessExpression.Expression;

                    List<ArgumentSyntax> arguments =
                        new( node.ArgumentList.Arguments.Count + 1 ) { Argument( receiver ).WithTemplateAnnotationsFrom( receiver ) };

                    arguments.AddRange( node.ArgumentList.Arguments );

                    var replacementNode = InvocationExpression(
                            MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                this.MetaSyntaxFactory.Type( method.ContainingType ),
                                memberAccessExpression.Name ),
                            ArgumentList( SeparatedList( arguments ) ) )
                        .WithSymbolAnnotationsFrom( node )
                        .WithTemplateAnnotationsFrom( node );

                    return this.VisitInvocationExpression( replacementNode );
                }
                else
                {
                    throw new AssertionFailedException( $"Unexpected expression type {node.Expression.GetType()} when processing extension methods." );
                }
            }
        }

        return base.VisitInvocationExpression( node );
    }

    public override SyntaxNode VisitArgument( ArgumentSyntax node )
    {
        var transformedNode = base.VisitArgument( node ).AssertNotNull();

        if ( this.GetTransformationKind( node ) == TransformationKind.None && this.GetTransformationKind( node.Expression ) == TransformationKind.Transform &&
             node.TargetRequiresUserExpression() )
        {
            var transformedArgument = (ArgumentSyntax) transformedNode;

            var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression );

            if ( expressionType is null or { TypeKind: TypeKind.Dynamic } )
            {
                expressionType = this._runTimeCompilation.GetSpecialType( SpecialType.System_Object );
            }

            // Wrap the ExceptionSyntax into a TypedExpressionSyntax.
            var runTimeExpression =
                InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                    .AddArgumentListArguments(
                        Argument( transformedArgument.Expression ),
                        Argument( SyntaxFactoryEx.LiteralExpression( expressionType.GetSerializableTypeId().Id ) ) );

            return transformedArgument.WithExpression( runTimeExpression );
        }
        else
        {
            return transformedNode;
        }
    }

    private void AddTemplateSyntaxFactoryStatement( SyntaxNode node, string templateSyntaxFactoryMemberName, params ArgumentSyntax[] arguments )
    {
        var addStatementStatement = ExpressionStatement(
            InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( templateSyntaxFactoryMemberName ) )
                .AddArgumentListArguments( Argument( SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext!.StatementListVariableName ) ) )
                .AddArgumentListArguments( arguments ) );

        addStatementStatement = this.DeepIndent(
            addStatementStatement.WithLeadingTrivia(
                this.GetCommentFromNode( node.Parent! )
                    .AddRange( addStatementStatement.GetLeadingTrivia() ) ) );

        this._currentMetaContext.AddStatement( addStatementStatement );
    }

    private void AddAddStatementStatement( SyntaxNode node, ExpressionSyntax statementExpression )
        => this.AddTemplateSyntaxFactoryStatement( node, nameof(ITemplateSyntaxFactory.AddStatement), Argument( statementExpression ) );

    private SyntaxTriviaList GetCommentFromNode( SyntaxNode node )
    {
        var text = _endOfLineRegex.Replace( node.ToString(), " " );

        if ( text.Length > 120 )
        {
            text = text.Substring( 0, 117 ) + "...";
        }

        return TriviaList( Comment( "// " + text ), this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia );
    }

    private ParameterSyntax CreateTemplateSyntaxFactoryParameter()
        => Parameter( default, default, this._templateSyntaxFactoryType, SyntaxFactoryEx.WellKnownIdentifier( _templateSyntaxFactoryParameterName ), null );

    public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node )
    {
        this.Indent( 3 );

        // Build the template parameter list.
        var templateParameters =
            new List<ParameterSyntax>( 1 + node.ParameterList.Parameters.Count + (node.TypeParameterList?.Parameters.Count ?? 0) )
            {
                this.CreateTemplateSyntaxFactoryParameter()
            };

        var templateOptionalParameters = new List<ParameterSyntax>();
        var templateParameterDefaultStatements = new List<StatementSyntax>();

        // Add non-optional parameters.
        foreach ( var parameter in node.ParameterList.Parameters )
        {
            var templateParameter = parameter;
            var parameterSymbol = (IParameterSymbol) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( parameter ).AssertSymbolNotNull();
            var isCompileTime = this._templateMemberClassifier.IsCompileTimeParameter( parameterSymbol );

            if ( !isCompileTime )
            {
                templateParameter =
                    templateParameter
                        .WithType( SyntaxFactoryEx.ExpressionSyntaxType )
                        .WithModifiers( TokenList() )
                        .WithAttributeLists( default );

                if ( !parameterSymbol.IsOptional )
                {
                    templateParameters.Add( templateParameter );
                }
                else
                {
                    // Optional parameters are added to the end of the signature.
                    templateParameter =
                        templateParameter.WithDefault( EqualsValueClause( SyntaxFactoryEx.Null ) );

                    // param ??= default-syntax;
                    templateParameterDefaultStatements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.CoalesceAssignmentExpression,
                                SyntaxFactoryEx.WellKnownIdentifierName( parameter.Identifier ),
                                this.TransformExpression( parameter.Default.AssertNotNull().Value ) ) ) );

                    templateOptionalParameters.Add( templateParameter );
                }
            }
            else
            {
                if ( !parameterSymbol.IsOptional )
                {
                    templateParameters.Add( templateParameter );
                }
                else
                {
                    templateOptionalParameters.Add( templateParameter );
                }
            }
        }

        // Add type parameters between non-optional and optional parameters.
        if ( node.TypeParameterList != null )
        {
            foreach ( var parameter in node.TypeParameterList.Parameters )
            {
                var parameterSymbol = (ITypeParameterSymbol) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( parameter ).AssertSymbolNotNull();
                var isCompileTime = this._templateMemberClassifier.IsCompileTimeParameter( parameterSymbol );

                if ( isCompileTime )
                {
                    this._templateCompileTimeTypeParameterNames.Add( parameter.Identifier.ValueText );

                    templateParameters.Add( Parameter( default, default, this._templateTypeArgumentType, parameter.Identifier, null ) );
                }
            }
        }

        // Add optional parameters last.
        foreach ( var templateOptionalParameter in templateOptionalParameters )
        {
            templateParameters.Add( templateOptionalParameter );
        }

        // Build the template body.
        BlockSyntax? body;

        if ( node.Body != null )
        {
            body = (BlockSyntax) this.BuildRunTimeBlock( node.Body, false );
        }
        else if ( node.ExpressionBody != null )
        {
            var isVoid = node.ReturnType.IsKind( SyntaxKind.PredefinedType ) && node.ReturnType is PredefinedTypeSyntax predefinedType
                                                                             && predefinedType.Keyword.IsKind( SyntaxKind.VoidKeyword );

            body = (BlockSyntax) this.BuildRunTimeBlock(
                node.ExpressionBody.AssertNotNull().Expression,
                false,
                isVoid );
        }
        else
        {
            body = null;
        }

        if ( templateParameterDefaultStatements.Any() )
        {
            body = body?.WithStatements( body.Statements.InsertRange( 0, templateParameterDefaultStatements ) );
        }

        var result = this.CreateTemplateMethod(
            node,
            body,
            templateParameters.ToArray(),
            node.Modifiers.Where( modifier => modifier.IsAccessModifierKeyword() ).ToArray() );

        this.Unindent( 3 );

        this._templateCompileTimeTypeParameterNames.Clear();

        return result;
    }

    public override SyntaxNode VisitAccessorDeclaration( AccessorDeclarationSyntax node )
    {
        if ( node.Body == null && node.ExpressionBody == null )
        {
            // Not supported or incomplete syntax.
            return node;
        }

        this.Indent( 3 );

        // Create the body.
        BlockSyntax body;

        if ( node.Body != null )
        {
            body = (BlockSyntax) this.BuildRunTimeBlock( node.Body, false );
        }
        else
        {
            var isVoid = !node.Keyword.IsKind( SyntaxKind.GetKeyword );

            body = (BlockSyntax) this.BuildRunTimeBlock(
                node.ExpressionBody.AssertNotNull().Expression,
                false,
                isVoid );
        }

        // Create the parameter list.
        var parameters = node.Keyword.IsKind( SyntaxKind.GetKeyword )
            ? [this.CreateTemplateSyntaxFactoryParameter()]
            : new[]
            {
                this.CreateTemplateSyntaxFactoryParameter(),
                Parameter( default, default, SyntaxFactoryEx.ExpressionSyntaxType, SyntaxFactoryEx.WellKnownIdentifier( "value" ), null )
            };

        // Create the method.
        var result = this.CreateTemplateMethod( node, body, parameters );

        this.Unindent( 3 );

        return result;
    }

    public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node )
    {
        if ( node.ExpressionBody is { Expression: var bodyExpression } )
        {
            this.Indent( 3 );

            var body = (BlockSyntax) this.BuildRunTimeBlock( bodyExpression, false, false );

            var result = this.CreateTemplateMethod( node, body );

            this.Unindent( 3 );

            return result;
        }
        else if ( node.Initializer is { Value: var initializerExpression } )
        {
            this.Indent( 3 );

            var body = (BlockSyntax) this.BuildRunTimeBlock( initializerExpression, false, false );

            var result = this.CreateTemplateMethod( node, body );

            this.Unindent( 3 );

            return result;
        }
        else
        {
            throw new AssertionFailedException( $"The property has no expression body and no initializer at '{node.GetLocation()}'." );
        }
    }

    public override SyntaxNode? VisitVariableDeclarator( VariableDeclaratorSyntax node )
    {
        if ( this._syntaxKind == TemplateCompilerSemantics.Initializer && node.Parent == null )
        {
            this.Indent( 3 );

            // This is template for field initializer.
            var body = (BlockSyntax) this.BuildRunTimeBlock( node.Initializer.AssertNotNull().Value, false, false );

            var result = this.CreateTemplateMethod( node, body );

            this.Unindent( 3 );

            return result;
        }
        else
        {
            return base.VisitVariableDeclarator( node );
        }
    }

    private MethodDeclarationSyntax CreateTemplateMethod(
        SyntaxNode node,
        BlockSyntax? body,
        ParameterSyntax[]? parameters = null,
        SyntaxToken[]? accessibilityModifiers = null )
        => MethodDeclaration(
                this.MetaSyntaxFactory.Type( typeof(SyntaxNode) ).WithTrailingTrivia( Space ),
                SyntaxFactoryEx.WellKnownIdentifier( this._templateName ) )
            .AddParameterListParameters( parameters ?? [this.CreateTemplateSyntaxFactoryParameter()] )
            .WithModifiers( this.DetermineModifiers( accessibilityModifiers ) )
            .NormalizeWhitespace()
            .WithBody( body )
            .WithSemicolonToken( Token( body == null ? SyntaxKind.SemicolonToken : SyntaxKind.None ) )
            .WithLeadingTrivia( node.GetLeadingTrivia() )
            .WithTrailingTrivia( LineFeed, LineFeed );

    private SyntaxTokenList DetermineModifiers( SyntaxToken[]? accessibilityModifiers )
    {
        var modifiers = TokenList( accessibilityModifiers ?? [Token( SyntaxKind.PublicKeyword ).WithTrailingTrivia( Space )] );

        var templateSymbol = this._rootTemplateSymbol.AssertSymbolNotNull();

        void AddModifier( SyntaxKind kind )
        {
            modifiers = modifiers.Add( Token( kind ).WithTrailingTrivia( Space ) );
        }

        if ( templateSymbol.IsStatic )
        {
            AddModifier( SyntaxKind.StaticKeyword );
        }

        if ( templateSymbol.Kind == SymbolKind.Method && templateSymbol is IMethodSymbol { AssociatedSymbol: null } )
        {
            // Only regular methods (not accessors) can be used as subtemplates, so only they get virtual-related modifiers.

            if ( templateSymbol.IsVirtual )
            {
                AddModifier( SyntaxKind.VirtualKeyword );
            }

            if ( templateSymbol.IsAbstract )
            {
                AddModifier( SyntaxKind.AbstractKeyword );
            }

            if ( templateSymbol.IsOverride )
            {
                // If the base template is from an assembly that was compiled with Metalama version older than 2023.3,
                // the base compiled template won't be abstract or virtual, so the derived compiled template can't be override.
                var overriddenTemplate = templateSymbol.GetOverriddenMember();

                if ( overriddenTemplate != null && !Equals( overriddenTemplate.ContainingAssembly, templateSymbol.ContainingAssembly ) )
                {
                    var compileTimeBaseType = this.MetaSyntaxFactory.ReflectionMapper.GetNamedTypeSymbolByMetadataName(
                        overriddenTemplate.ContainingType.GetReflectionFullName(),
                        new AssemblyName( overriddenTemplate.ContainingAssembly.Name ) );

                    var baseCompiledTemplate = compileTimeBaseType.GetMembers( this._templateName ).SingleOrDefault();

                    if ( baseCompiledTemplate != null && (baseCompiledTemplate.IsVirtual || baseCompiledTemplate.IsAbstract) )
                    {
                        if ( templateSymbol.IsSealed )
                        {
                            AddModifier( SyntaxKind.SealedKeyword );
                        }

                        AddModifier( SyntaxKind.OverrideKeyword );
                    }
                    else if ( !templateSymbol.IsSealed && !templateSymbol.ContainingType.IsSealed )
                    {
                        AddModifier( SyntaxKind.VirtualKeyword );
                    }
                }
                else
                {
                    if ( templateSymbol.IsSealed )
                    {
                        AddModifier( SyntaxKind.SealedKeyword );
                    }

                    AddModifier( SyntaxKind.OverrideKeyword );
                }
            }
        }

        return modifiers;
    }

    public override SyntaxNode VisitBlock( BlockSyntax node )
    {
        var transformationKind = this.GetTransformationKind( node );

        if ( transformationKind == TransformationKind.Transform )
        {
            return this.BuildRunTimeBlock( node, true );
        }
        else
        {
            using ( this.WithMetaContext( MetaContext.CreateForCompileTimeBlock( this._currentMetaContext! ) ) )
            {
                this.ReserveLocalFunctionNames( node.Statements );

                var metaStatements = this.ToMetaStatements( node.Statements );

                this._currentMetaContext!.AddStatements( metaStatements );

                return Block( this._currentMetaContext.GetStatements() );
            }
        }
    }

    /// <summary>
    /// Generates a run-time block from expression.
    /// </summary>
    /// <param name="generateExpression"><c>true</c> if the returned <see cref="SyntaxNode"/> must be an
    /// expression (in this case, a delegate invocation is returned), or <c>false</c> if it can be a statement
    /// (in this case, a return statement is returned).</param>
    private SyntaxNode BuildRunTimeBlock( ExpressionSyntax node, bool generateExpression, bool isVoid )
    {
        StatementSyntax statement;

        if ( node.IsKind( SyntaxKind.ThrowExpression ) && node is ThrowExpressionSyntax throwExpression )
        {
            statement = ThrowStatement( throwExpression.ThrowKeyword, throwExpression.Expression, Token( SyntaxKind.SemicolonToken ) );
        }
        else
        {
            statement = isVoid ? ExpressionStatement( node ) : ReturnStatement( node );
        }

        return this.BuildRunTimeBlock( SingletonList( statement ), generateExpression );
    }

    /// <summary>
    /// Generates a run-time block.
    /// </summary>
    /// <param name="generateExpression"><c>true</c> if the returned <see cref="SyntaxNode"/> must be an
    /// expression (in this case, a delegate invocation is returned), or <c>false</c> if it can be a statement
    /// (in this case, a return statement is returned).</param>
    private SyntaxNode BuildRunTimeBlock( BlockSyntax node, bool generateExpression )
        => this.BuildRunTimeBlock(
            node.Statements,
            generateExpression,
            this.GetFunctionLikeRunTimeBlockInfo( node ) );

    private sealed record FunctionLikeRunTimeBlockInfo( ITypeSymbol ReturnType, bool IsAsync );

    private FunctionLikeRunTimeBlockInfo? GetFunctionLikeRunTimeBlockInfo( SyntaxNode? node )
    {
        switch ( node?.Parent?.Kind() )
        {
            case SyntaxKind.LocalFunctionStatement:
                {
                    var localFunction = (LocalFunctionStatementSyntax) node.Parent!;
                    var localFunctionSymbol = (IMethodSymbol?) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( localFunction );

                    if ( localFunctionSymbol == null )
                    {
                        return null;
                    }

                    var returnType = localFunctionSymbol.ReturnType;

                    return new FunctionLikeRunTimeBlockInfo( returnType, localFunctionSymbol.IsAsync );
                }

            case SyntaxKind.SimpleLambdaExpression or SyntaxKind.ParenthesizedLambdaExpression or SyntaxKind.AnonymousMethodExpression:
                {
                    var anonymousFunction = (AnonymousFunctionExpressionSyntax) node.Parent!;
                    var anonymousFunctionSymbol = (IMethodSymbol?) this._syntaxTreeAnnotationMap.GetSymbol( anonymousFunction );

                    if ( anonymousFunctionSymbol == null )
                    {
                        return null;
                    }

                    return new FunctionLikeRunTimeBlockInfo( anonymousFunctionSymbol.ReturnType, anonymousFunctionSymbol.IsAsync );
                }

            default:
                return null;
        }
    }

    /// <summary>
    /// Generates a run-time block.
    /// </summary>
    /// <param name="statements">The statements to add to the block.</param>
    /// <param name="generateExpression"><c>true</c> if the returned <see cref="SyntaxNode"/> must be an
    /// expression (in this case, a delegate invocation is returned), or <c>false</c> if it can be a statement
    /// (in this case, a return statement is returned).</param>
    private SyntaxNode BuildRunTimeBlock(
        SyntaxList<StatementSyntax> statements,
        bool generateExpression,
        FunctionLikeRunTimeBlockInfo? localFunctionInfo = null,
        bool generateStatementList = false )
    {
        var blockId = ++this._nextStatementListId;

        using ( this.WithMetaContext(
                   MetaContext.CreateForRunTimeBlock( this._currentMetaContext, $"__s{blockId}", new SkipCompileTimeLogicVariable( $"__skip{blockId}" ) ) ) )
        {
            // List<StatementOrTrivia> statements = new List<StatementOrTrivia>();
            var listType = this.MetaSyntaxFactory.Type( typeof(List<StatementOrTrivia>) );

            this._currentMetaContext!.AddStatement(
                LocalDeclarationStatement(
                        VariableDeclaration( listType )
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator( SyntaxFactoryEx.WellKnownIdentifier( this._currentMetaContext.StatementListVariableName ) )
                                        .WithInitializer( EqualsValueClause( ObjectCreationExpression( listType, ArgumentList(), default ) ) ) ) ) )
                    .NormalizeWhitespace()
                    .WithLeadingTrivia( this.GetIndentation() )
                    .WithTrailingTrivia( LineFeed ) );

            // Declare the skip flag for this run-time block. This allows statements after an early
            // return in a compile-time conditional to be skipped while still processing local functions.
            this.DeclareSkipCompileTimeLogicFlag( this._currentMetaContext );

            var previousTemplateMetaSyntaxFactory = this._templateMetaSyntaxFactory;

            // If we are in a local function, use a different TemplateMetaSyntaxFactory.
            // Each local function gets a unique name to support nested local functions.
            if ( localFunctionInfo != null )
            {
                var localFactoryName = _templateSyntaxFactoryLocalName + (++this._nextLocalFunctionFactoryId);
                this._templateMetaSyntaxFactory = new TemplateMetaSyntaxFactoryImpl( localFactoryName );

                // var localSyntaxFactory = syntaxFactory.ForLocalFunction( "typeof(X)", map );
                var map = this.CreateTypeParameterSubstitutionDictionary( nameof(TemplateTypeArgument.Type), this._dictionaryOfITypeType );

                this._currentMetaContext!.AddStatement(
                    LocalDeclarationStatement(
                            VariableDeclaration( this._templateSyntaxFactoryType )
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator( SyntaxFactoryEx.WellKnownIdentifier( localFactoryName ) )
                                            .WithInitializer(
                                                EqualsValueClause(
                                                    InvocationExpression(
                                                            previousTemplateMetaSyntaxFactory.TemplateSyntaxFactoryMember(
                                                                nameof(ITemplateSyntaxFactory.ForLocalFunction) ) )
                                                        .WithArgumentList(
                                                            ArgumentList(
                                                                SeparatedList(
                                                                [
                                                                    Argument(
                                                                        SyntaxFactoryEx.LiteralExpression(
                                                                            localFunctionInfo.ReturnType.GetSerializableTypeId().Id ) ),
                                                                    Argument( map ),
                                                                    Argument( SyntaxFactoryEx.LiteralExpression( localFunctionInfo.IsAsync ) )
                                                                ] ) ) ) ) ) ) ) )
                        .NormalizeWhitespace()
                        .WithLeadingTrivia( this.GetIndentation() ) );
            }

            this.ReserveLocalFunctionNames( statements );

            this._currentMetaContext.AddStatements( this.ToMetaStatements( statements ) );

            this._templateMetaSyntaxFactory = previousTemplateMetaSyntaxFactory;

            // TemplateSyntaxFactory.ToStatementList( __s1 )
            var toArrayStatementExpression = InvocationExpression(
                this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ToStatementList) ),
                ArgumentList(
                    SingletonSeparatedList( Argument( SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext.StatementListVariableName ) ) ) ) );

            if ( generateExpression )
            {
                // return TemplateSyntaxFactory.ToStatementArray( __s1 );

                var returnStatementSyntax = ReturnStatement( toArrayStatementExpression ).WithLeadingTrivia( this.GetIndentation() ).NormalizeWhitespace();
                this._currentMetaContext.AddStatement( returnStatementSyntax );

                // Block( Func<SyntaxList<StatementSyntax>>( delegate { ... } )

                var statementList = InvocationExpression(
                    ObjectCreationExpression( this.MetaSyntaxFactory.Type( typeof(Func<SyntaxList<StatementSyntax>>) ) )
                        .NormalizeWhitespace()
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        AnonymousMethodExpression()
                                            .WithBody(
                                                Block( this._currentMetaContext.GetStatements() )
                                                    .AddNoDeepIndentAnnotation() ) ) ) ) ) );

                if ( generateStatementList )
                {
                    return this.DeepIndent( statementList );
                }
                else
                {
                    return this.DeepIndent( this.MetaSyntaxFactory.Block( SyntaxFactoryEx.Default, statementList ) );
                }
            }
            else
            {
                // return __s;
                this._currentMetaContext.AddStatement(
                    ReturnStatement(
                        this.MetaSyntaxFactory.Block( SyntaxFactoryEx.Default, toArrayStatementExpression ).WithLeadingTrivia( this.GetIndentation() ) ) );

                return Block( this._currentMetaContext.GetStatements() );
            }
        }
    }

    /// <summary>
    /// Reserve names for local functions in the current block. This needs to be done upfront because local functions, contrarily to local variables,
    /// can be used before they are declared.
    /// </summary>
    private void ReserveLocalFunctionNames( SyntaxList<StatementSyntax> statements )
    {
        foreach ( var localFunctionDeclaration in statements.OfType<LocalFunctionStatementSyntax>() )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetDeclaredSymbol( localFunctionDeclaration ).AssertSymbolNotNull();
            var declaredSymbolNameLocal = this.ReserveRunTimeSymbolName( symbol ).Identifier;
            this._currentMetaContext!.AddRunTimeSymbolLocal( symbol, declaredSymbolNameLocal );
        }
    }

    /// <summary>
    /// Transforms a list of <see cref="StatementSyntax"/> of the source template into a list of <see cref="StatementSyntax"/> for the compiled
    /// template.
    /// </summary>
    /// <param name="statements">The statements to transform.</param>
    /// <param name="isConditionalBlock">
    /// <c>true</c> if these statements are inside a compile-time conditional (if/while/for/do/switch case);
    /// <c>false</c> for regular statements.
    /// </param>
    private IEnumerable<StatementSyntax> ToMetaStatements( in SyntaxList<StatementSyntax> statements, bool isConditionalBlock = false )
        => statements.SelectMany( s => this.ToMetaStatements( s, isConditionalBlock ) );

    /// <summary>
    /// Transforms a <see cref="StatementSyntax"/> of the source template into a single <see cref="StatementSyntax"/> for the compiled template.
    /// This method is guaranteed to return a single <see cref="StatementSyntax"/>. If the source statement results in several compiled statements,
    /// they will be wrapped into a block.
    /// </summary>
    /// <param name="statement">A statement of the source template.</param>
    /// <param name="isConditionalBlock">
    /// <c>true</c> if this statement is the body of a compile-time conditional (if/while/for/do);
    /// <c>false</c> for regular statements.
    /// </param>
    private StatementSyntax ToMetaStatement( StatementSyntax statement, bool isConditionalBlock = false )
    {
        var statements = this.ToMetaStatements( statement, isConditionalBlock ).ToList();

        // Declaration statements (for local variable or function) and labeled statements cannot be embedded in e.g. an if statement directly,
        // so enclose them in a block here, in case they're used that way.
        return statements is [not (LocalDeclarationStatementSyntax or LabeledStatementSyntax or LocalFunctionStatementSyntax)]
            ? statements[0]
            : Block( statements );
    }

    /// <summary>
    /// Transforms a <see cref="StatementSyntax"/> of the source template into a list of <see cref="StatementSyntax"/> for the compiled template.
    /// </summary>
    /// <param name="statement">A statement of the source template.</param>
    /// <param name="isConditionalBlock">
    /// <c>true</c> if this statement is the body of a compile-time conditional (if/while/for/do);
    /// <c>false</c> for regular statements.
    /// </param>
    /// <returns>A list of statements for the compiled template.</returns>
    private IEnumerable<StatementSyntax> ToMetaStatements( StatementSyntax statement, bool isConditionalBlock = false )
    {
        MetaContext newContext;

        if ( statement.IsKind( SyntaxKind.Block ) && statement is BlockSyntax block )
        {
            // Push the compile-time template block.
            newContext = MetaContext.CreateForCompileTimeBlock( this._currentMetaContext!, isConditionalBlock );

            using ( this.WithMetaContext( newContext ) )
            {
                this.ReserveLocalFunctionNames( block.Statements );

                // Process all statements in this block.
                foreach ( var childStatement in block.Statements )
                {
                    ProcessStatement( childStatement );
                }
            }
        }
        else
        {
            // Push a new MetaContext so statements get added to a new list of statements.
            // Switch case statements are not wrapped in a block syntactically, but if they're inside
            // a compile-time switch, return/throw should still terminate compile-time flow. We use
            // CreateForCompileTimeBlock to mark the context as a conditional block in this case.
            newContext = isConditionalBlock
                ? MetaContext.CreateForCompileTimeBlock( this._currentMetaContext!, isConditionalBlock: true )
                : MetaContext.CreateStatementGroupContext( this._currentMetaContext! );

            using ( this.WithMetaContext( newContext ) )
            {
                ProcessStatement( statement );
            }
        }

        // Returns the statements collected during this call.
        return newContext.GetStatements();

        void ProcessStatement( StatementSyntax singleStatement )
        {
            var isLocalFunction = singleStatement.IsKind( SyntaxKind.LocalFunctionStatement );
            var skipCompileTimeLogicVariable = this._currentMetaContext!.SkipCompileTimeLogicVariable;
            var skipMightBeSetBeforeProcessing = skipCompileTimeLogicVariable.MightBeTrue;
            var olsIsSkipVariableKnownFalse = skipCompileTimeLogicVariable.IsKnownFalse;

            skipCompileTimeLogicVariable.IsKnownFalse = true;
            var transformedNode = this.Visit( singleStatement );
            skipCompileTimeLogicVariable.IsKnownFalse = olsIsSkipVariableKnownFalse;

            switch ( transformedNode )
            {
                case null:
                    break;

                case StatementSyntax statementSyntax:
                    // The statement is already build-time code so there is nothing to transform.
                    var statementWithTrivia = statementSyntax.WithLeadingTrivia( this.GetIndentation() );

                    // If the skip flag was set and this is not a local function, wrap it in a condition.
                    // Local functions must always be defined so they can be referenced.
                    if ( !isLocalFunction && skipMightBeSetBeforeProcessing )
                    {
                        newContext.AddStatements( this.WrapInSkipCompileTimeLogicCheck( statementWithTrivia, singleStatement ) );
                    }
                    else
                    {
                        newContext.AddStatement( statementWithTrivia );
                    }

                    break;

                case ExpressionSyntax expressionSyntax:
                    {
                        // The statement is run-time code and has been transformed into an expression creating the StatementSyntax.
                        // We need to generate the code adding this code to the list of statements, i.e. `AddStatement( expression )`.

                        var leadingTrivia = TriviaList( this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia )
                            .AddRange( this.GetIndentation() )
                            .AddRange( this.GetCommentFromNode( singleStatement ) )
                            .Add( this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia )
                            .AddRange( this.GetIndentation() );

                        var eol = this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                        var trailingTrivia = TriviaList( eol, eol );

                        // TemplateSyntaxFactory.Add( __s, expression )
                        StatementSyntax add = this.DeepIndent(
                            ExpressionStatement(
                                InvocationExpression(
                                    this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.AddStatement) ),
                                    ArgumentList(
                                        SeparatedList(
                                        [
                                            Argument( SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext!.StatementListVariableName ) ),
                                            Argument( expressionSyntax )
                                        ] ) ) ) ) );

                        add = add.WithLeadingTrivia( leadingTrivia ).WithTrailingTrivia( trailingTrivia );

                        // If the skip flag was set and this is not a local function, wrap it in a condition.
                        // Local functions must always be defined so they can be referenced.
                        if ( !isLocalFunction && skipMightBeSetBeforeProcessing )
                        {
                            newContext.AddStatements( this.WrapInSkipCompileTimeLogicCheck( add, singleStatement ) );
                        }
                        else
                        {
                            newContext.AddStatement( add );
                        }

                        break;
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected node kind {transformedNode.Kind()} at '{singleStatement.GetLocation()};." );
            }
        }
    }

    /// <summary>
    /// Declares the skip compile-time logic flag at the start of a run-time block.
    /// Generates: <c>bool __skipN = false;</c> where N is the block ID.
    /// </summary>
    private void DeclareSkipCompileTimeLogicFlag( MetaContext context )
    {
        var declaration = LocalDeclarationStatement(
                VariableDeclaration( PredefinedType( Token( SyntaxKind.BoolKeyword ).WithTrailingTrivia( Space ) ) )
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator( SyntaxFactoryEx.WellKnownIdentifier( context.SkipCompileTimeLogicVariable.Name ) )
                                .WithInitializer( EqualsValueClause( LiteralExpression( SyntaxKind.FalseLiteralExpression ) ) ) ) ) )
            .WithLeadingTrivia( this.GetIndentation() )
            .WithTrailingTrivia( LineFeed );

        context.AddConditionalStatement( declaration, () => context.SkipCompileTimeLogicVariable is { HasBeenSet: true } );
    }

    /// <summary>
    /// Generates a statement that sets the skip compile-time logic flag to true.
    /// Generates: <c>__skipN = true;</c> where N is the block ID.
    /// </summary>
    private BlockSyntax AddSetSkipCompileTimeLogicFlag( BlockSyntax block )
    {
        return block.WithStatements( block.Statements.Add( this.CreateSetSkipCompileTimeLogicFlag() ) );
    }

    private StatementSyntax CreateSetSkipCompileTimeLogicFlag()
    {
        this._currentMetaContext!.SkipCompileTimeLogicVariable.HasBeenSet = true;

        return ExpressionStatement(
                AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext!.SkipCompileTimeLogicVariable.Name ),
                    LiteralExpression( SyntaxKind.TrueLiteralExpression ) ) )
            .WithLeadingTrivia( this.GetIndentation() )
            .WithTrailingTrivia( LineFeed );
    }

    /// <summary>
    /// Wraps a statement in a condition that checks if compile-time logic should be skipped.
    /// Generates: <c>if (__skipN) goto __nextN; statement; __nextN:; Unsafe.SkipInit(out variables);</c> where N is the block ID.
    /// Compile-time variables declared in the statement are hoisted and fake-initialized with Unsafe.SkipInit after the label.
    /// </summary>
    private IEnumerable<StatementSyntax> WrapInSkipCompileTimeLogicCheck( StatementSyntax statement, StatementSyntax originalStatement )
    {
        var labelName = $"__next{this._nextLabelId++}";

        // if (__skip) goto __next;
        yield return IfStatement(
                SyntaxFactoryEx.WellKnownIdentifierName( this._currentMetaContext!.SkipCompileTimeLogicVariable.Name ),
                GotoStatement( SyntaxKind.GotoStatement, SyntaxFactoryEx.SafeIdentifierName( labelName ) ) )
            .WithLeadingTrivia( this.GetIndentation() )
            .NormalizeWhitespace();

        // statement;
        yield return statement;

        // __next:;
        yield return LabeledStatement( labelName, EmptyStatement() )
            .WithLeadingTrivia( this.GetIndentation() )
            .NormalizeWhitespace();

        // If the statement is `return` with an expression, we must also return.
        if ( statement.IsKind( SyntaxKind.ReturnStatement ) && statement is ReturnStatementSyntax { Expression: not null } )
        {
            yield return ReturnStatement( SyntaxFactoryEx.Default );
        }

        // Find compile-time variables declared in this statement that need Unsafe.SkipInit
        var variableFinder = new StatementCompileTimeVariableFinder( this, originalStatement );
        variableFinder.Visit();

        foreach ( var localSymbol in variableFinder.AssignedVariables )
        {
            // System.Runtime.CompilerServices.Unsafe.SkipInit(out variable);
            yield return ExpressionStatement(
                    InvocationExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            this._unsafeType,
                            SyntaxFactoryEx.WellKnownIdentifierName( "SkipInit" ) ),
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument( SyntaxFactoryEx.SafeIdentifierName( localSymbol.Name ) )
                                    .WithRefOrOutKeyword( Token( SyntaxKind.OutKeyword ) ) ) ) ) )
                .WithLeadingTrivia( this.GetIndentation() )
                .NormalizeWhitespace();
        }
    }

    protected override ExpressionSyntax TransformInterpolatedStringExpression( InterpolatedStringExpressionSyntax node )
    {
        List<ExpressionSyntax> transformedContents = new( node.Contents.Count );

        foreach ( var content in node.Contents )
        {
            switch ( content.Kind() )
            {
                case SyntaxKind.InterpolatedStringText when content is InterpolatedStringTextSyntax text:
                    transformedContents.Add( this.TransformInterpolatedStringText( text ) );

                    break;

                case SyntaxKind.Interpolation when content is InterpolationSyntax interpolation:
                    if ( this.GetTransformationKind( interpolation ) == TransformationKind.None &&
                         !interpolation.Expression.IsKind( SyntaxKind.TypeOfExpression ) )
                    {
                        // We have a compile-time interpolation (e.g. formatting string argument).
                        // We can evaluate it at compile time and add it as a text content.

                        // typeof was skipped because it is always annotated as compile time but actually always transformed.

                        var compileTimeInterpolatedString =
                            InterpolatedStringExpression(
                                Token( SyntaxKind.InterpolatedStringStartToken ),
                                SingletonList<InterpolatedStringContentSyntax>( interpolation ),
                                Token( SyntaxKind.InterpolatedStringEndToken ) );

                        var token = this.MetaSyntaxFactory.Token(
                            LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) ),
                            this.Transform( SyntaxKind.InterpolatedStringTextToken ),
                            compileTimeInterpolatedString,
                            compileTimeInterpolatedString,
                            LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) ) );

                        transformedContents.Add( this.MetaSyntaxFactory.InterpolatedStringText( token ) );
                    }
                    else
                    {
                        var transformedInterpolation = this.TransformInterpolation( interpolation );
                        transformedContents.Add( transformedInterpolation );
                    }

                    break;

                default:
                    throw new AssertionFailedException( $"Unexpected content {content.Kind()} at '{content.GetLocation()}'." );
            }
        }

        this.Indent();

        var createInterpolatedString = InvocationExpression( this.MetaSyntaxFactory.SyntaxFactoryMethod( nameof(InterpolatedStringExpression) ) )
            .WithArgumentList(
                ArgumentList(
                    SeparatedList<ArgumentSyntax>(
                        new SyntaxNodeOrToken[]
                        {
                            Argument( this.Transform( node.StringStartToken ) ).WithLeadingTrivia( this.GetIndentation() ),
                            Token( SyntaxKind.CommaToken ).WithTrailingTrivia( GetLineBreak() ),
                            Argument( this.MetaSyntaxFactory.List<InterpolatedStringContentSyntax>( transformedContents ) )
                                .WithLeadingTrivia( this.GetIndentation() ),
                            Token( SyntaxKind.CommaToken ).WithTrailingTrivia( GetLineBreak() ),
                            Argument( this.Transform( node.StringEndToken ) ).WithLeadingTrivia( this.GetIndentation() )
                        } ) ) );

        var callRender = InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RenderInterpolatedString) ),
            ArgumentList( SingletonSeparatedList( Argument( createInterpolatedString ) ) ) );

        this.Unindent();

        return callRender;
    }

    protected override ExpressionSyntax TransformInterpolation( InterpolationSyntax node )
    {
        var transformedNode = base.TransformInterpolation( node ).AssertNotNull();

        // FixInterpolationSyntax detects dynamic expressions via type annotations from the compile-time object model
        // (set by GetDynamicSyntax during template expansion), not from the template's semantic model.
        // The template semantic model reports many expressions as 'dynamic' (e.g. foreach iteration variables
        // in iterator templates) that have concrete types at runtime.
        var fixedNode = InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.FixInterpolationSyntax) ),
            ArgumentList( SingletonSeparatedList( Argument( transformedNode ) ) ) );

        return fixedNode;
    }

    public override SyntaxNode? VisitInterpolation( InterpolationSyntax node )
    {
        var transformedNode = base.VisitInterpolation( node );

        if ( transformedNode.IsKind( SyntaxKind.Interpolation ) && transformedNode is InterpolationSyntax transformedInterpolation )
        {
            return InterpolationSyntaxHelper.Fix( transformedInterpolation );
        }
        else
        {
            return transformedNode;
        }
    }

    public override SyntaxNode VisitSwitchStatement( SwitchStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time. Just serialize to syntax.
            return this.TransformSwitchStatement( node );
        }
        else
        {
            var transformedSections = new SwitchSectionSyntax[node.Sections.Count];

            for ( var i = 0; i < node.Sections.Count; i++ )
            {
                var section = node.Sections[i];

                // Compile-time switch: pass isConditionalBlock=true so returns/throws inside terminate compile-time flow.
                var transformedStatements = this.ToMetaStatements( section.Statements, isConditionalBlock: true ).ToMutableList();

                // If the last statement does not transfer control elsewhere, add a break statement.
                // This happens when the transfer control statement in a template is run-time (e.g. a throw).
                if ( transformedStatements[^1].Kind() is not
                    (SyntaxKind.BreakStatement
                    or SyntaxKind.ContinueStatement
                    or SyntaxKind.ReturnStatement
                    or SyntaxKind.ThrowStatement
                    or SyntaxKind.GotoCaseStatement
                    or SyntaxKind.GotoDefaultStatement
                    or SyntaxKind.GotoStatement) )
                {
                    transformedStatements.Add( BreakStatement() );
                }

                if ( section.Statements.NeverContinues() )
                {
                    // We insert at the first position to make sure we are before the `break` or `return`.
                    transformedStatements.Insert( 0, this.CreateSetSkipCompileTimeLogicFlag() );
                }

                transformedSections[i] = SwitchSection( section.Labels, List( transformedStatements ) );
            }

            return SwitchStatement(
                node.SwitchKeyword,
                node.OpenParenToken,
                node.Expression,
                node.CloseParenToken,
                node.OpenBraceToken,
                List( transformedSections ),
                node.CloseBraceToken );
        }
    }

    protected override ExpressionSyntax Transform( SyntaxList<StatementSyntax> list )
    {
        return (ExpressionSyntax) this.BuildRunTimeBlock( list, true, generateStatementList: true );
    }

    public override SyntaxNode VisitIfStatement( IfStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time if. Just serialize to syntax.
            return this.TransformIfStatement( node );
        }
        else
        {
            // Compile-time if: pass isConditionalBlock=true so returns inside terminate compile-time flow.
            var transformedStatement = this.ToMetaStatement( node.Statement, isConditionalBlock: true );
            var transformedElseStatement = node.Else != null ? this.ToMetaStatement( node.Else.Statement, isConditionalBlock: true ) : null;

            // The condition may contain constructs like typeof or nameof that need to be transformed.
            var condition = (ExpressionSyntax) this.Visit( node.Condition )!;

            // If the statement is not a block, wrap it in a block, to ensure chains of if-else-if statements are properly nested.
            if ( !transformedStatement.IsKind( SyntaxKind.Block ) )
            {
                transformedStatement = Block( transformedStatement );
            }

            if ( transformedElseStatement is not null && !transformedElseStatement.IsKind( SyntaxKind.Block ) )
            {
                transformedElseStatement = Block( transformedElseStatement );
            }

            // Mark the compile-time control flow for skipping.
            if ( node.Statement.NeverContinues() )
            {
                transformedStatement = this.AddSetSkipCompileTimeLogicFlag( (BlockSyntax) transformedStatement );
            }

            if ( node.Else?.Statement.NeverContinues() == true )
            {
                transformedElseStatement = this.AddSetSkipCompileTimeLogicFlag( (BlockSyntax) transformedElseStatement! );
            }

            return IfStatement(
                node.AttributeLists,
                condition,
                transformedStatement,
                transformedElseStatement != null ? ElseClause( transformedElseStatement ) : null );
        }
    }

    public override SyntaxNode VisitConditionalExpression( ConditionalExpressionSyntax node )
    {
        // condition has to be preserved if one of the expressions is throw
        var runTimeCondition = this.GetTransformationKind( node.Condition ) == TransformationKind.Transform ||
                               node.Condition.GetScopeFromAnnotation().GetValueOrDefault().GetExpressionValueScope() == TemplatingScope.RunTimeOnly ||
                               node.WhenTrue.IsKind( SyntaxKind.ThrowExpression ) ||
                               node.WhenFalse.IsKind( SyntaxKind.ThrowExpression );

        if ( runTimeCondition )
        {
            // Run-time conditional expression. Just serialize to syntax.
            return this.TransformConditionalExpression( node );
        }
        else
        {
            ExpressionSyntax transformedWhenTrue;
            ExpressionSyntax transformedWhenFalse;

            if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
            {
                // Run-time sub-expressions, serialize them to syntax.
                transformedWhenTrue = this.Transform( node.WhenTrue );
                transformedWhenFalse = this.Transform( node.WhenFalse );
            }
            else
            {
                transformedWhenTrue = (ExpressionSyntax) this.Visit( node.WhenTrue )!;
                transformedWhenFalse = (ExpressionSyntax) this.Visit( node.WhenFalse )!;
            }

            // The condition may contain constructs like typeof or nameof that need to be transformed.
            var condition = (ExpressionSyntax) this.Visit( node.Condition )!;

            return ConditionalExpression( condition, transformedWhenTrue, transformedWhenFalse );
        }
    }

    public override SyntaxNode VisitWhileStatement( WhileStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time while. Just serialize to syntax.
            return this.TransformWhileStatement( node );
        }
        else
        {
            // Compile-time while: pass isConditionalBlock=true so returns inside terminate compile-time flow.
            var transformedStatement = this.ToMetaStatement( node.Statement, isConditionalBlock: true );

            return WhileStatement(
                node.AttributeLists,
                node.Condition,
                transformedStatement );
        }
    }

    public override SyntaxNode VisitDoStatement( DoStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time do. Just serialize to syntax.
            return this.TransformDoStatement( node );
        }
        else
        {
            // Compile-time do: pass isConditionalBlock=true so returns inside terminate compile-time flow.
            var transformedStatement = this.ToMetaStatement( node.Statement, isConditionalBlock: true );

            return DoStatement(
                node.AttributeLists,
                transformedStatement,
                node.Condition );
        }
    }

    public override SyntaxNode VisitForEachStatement( ForEachStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time foreach. Just serialize to syntax.
            return this.TransformForEachStatement( node );
        }

        this.Indent();

        // Compile-time foreach: pass isConditionalBlock=true so returns inside terminate compile-time flow.
        var statement = this.ToMetaStatement( node.Statement, isConditionalBlock: true );

        this.Unindent();

        // The expression may contain typeof, nameof, ...
        var expression = (ExpressionSyntax) this.Visit( node.Expression )!;

        // It seems that trivia can be lost upstream, there can be a missing one between the 'in' keyword and the expression. Add them to be sure.
        return ForEachStatement(
            node.Type.WithTrailingTrivia( ElasticSpace ),
            node.Identifier.WithTrailingTrivia( ElasticSpace ),
            expression.WithLeadingTrivia( ElasticSpace ),
            statement );
    }

    public override SyntaxNode? VisitForEachVariableStatement( ForEachVariableStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time foreach with variable deconstruction. Just serialize to syntax.
            return this.TransformForEachVariableStatement( node );
        }

        this.Indent();

        // Compile-time foreach with variable deconstruction: pass isConditionalBlock=true so returns inside terminate compile-time flow.
        var statement = this.ToMetaStatement( node.Statement, isConditionalBlock: true );

        this.Unindent();

        // The expression may contain typeof, nameof, ...
        var expression = (ExpressionSyntax) this.Visit( node.Expression )!;

        return ForEachVariableStatement(
            node.Variable.WithTrailingTrivia( ElasticSpace ),
            expression.WithLeadingTrivia( ElasticSpace ),
            statement );
    }

    /// <summary>
    /// Determines if the expression will be transformed into syntax that instantiates an <see cref="IUserExpression"/>.
    /// </summary>
    private bool IsCompileTimeDynamic( ExpressionSyntax? expression )
    {
        if ( expression == null )
        {
            return false;
        }

        expression = expression.IgnoreSuppressNullWarning();

        return expression.GetScopeFromAnnotation() == TemplatingScope.CompileTimeOnlyReturningRuntimeOnly
               && !this._templateMemberClassifier.IsTemplateParameter( expression )
               && this.GetTransformationKind( expression ) != TransformationKind.Transform
               && ((this._syntaxTreeAnnotationMap.GetExpressionType( expression )?.Kind == SymbolKind.DynamicType
                    && this._syntaxTreeAnnotationMap.GetExpressionType( expression ) is IDynamicTypeSymbol)
                   || (this._syntaxTreeAnnotationMap.GetExpressionType( expression )?.Kind == SymbolKind.NamedType
                       && this._syntaxTreeAnnotationMap.GetExpressionType( expression ) is INamedTypeSymbol
                       {
                           Name: "Task" or "ConfiguredTaskAwaitable" or "IEnumerable" or "IAsyncEnumerator", TypeArguments: [IDynamicTypeSymbol]
                       }));
    }

    public override SyntaxNode VisitReturnStatement( ReturnStatementSyntax node )
    {
        if ( node.GetScopeFromAnnotation() == TemplatingScope.CompileTimeOnly )
        {
            // Compile-time returns can exist in anonymous methods.
            return base.VisitReturnStatement( node )!;
        }

        InvocationExpressionSyntax invocationExpression;

        if ( this.IsCompileTimeDynamic( node.Expression ) )
        {
            // We have a dynamic parameter. We need to call the second overload of ReturnStatement, the one that accepts the IUserExpression
            // itself and not the syntax.
            invocationExpression = CreateInvocationExpression( node.Expression.AssertNotNull(), false );
        }
        else if ( node.Expression.IsKind( SyntaxKind.AwaitExpression ) && node.Expression is AwaitExpressionSyntax awaitExpression
                                                                       && this.IsCompileTimeDynamic( awaitExpression.Expression ) )
        {
            invocationExpression = CreateInvocationExpression( awaitExpression.Expression, true );
        }
        else
        {
            var expression = this.Transform( node.Expression );

            invocationExpression = InvocationExpression(
                    this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ReturnStatement) ) )
                .AddArgumentListArguments( Argument( expression ) );
        }

        InvocationExpressionSyntax CreateInvocationExpression( ExpressionSyntax expression, bool awaitResult )
        {
            return
                InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DynamicReturnStatement) ) )
                    .AddArgumentListArguments(
                        Argument( this.ConvertToUserExpression( (ExpressionSyntax) this.Visit( expression ).AssertNotNull() ) ),
                        Argument( LiteralExpression( awaitResult ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression ) ) );
        }

        var addReturnExpression = this.WithCallToAddSimplifierAnnotation( invocationExpression );

        return addReturnExpression;
    }

    public override SyntaxNode VisitThrowStatement( ThrowStatementSyntax node )
    {
        // Transform the throw expression to create run-time syntax.
        var transformedExpression = node.Expression != null ? this.Transform( node.Expression ) : null;

        // Generate: SyntaxFactory.ThrowStatement( expression )
        InvocationExpressionSyntax invocationExpression;

        if ( transformedExpression != null )
        {
            invocationExpression = InvocationExpression( this.MetaSyntaxFactory.SyntaxFactoryMethod( nameof(ThrowStatement) ) )
                .AddArgumentListArguments( Argument( transformedExpression ) );
        }
        else
        {
            // Rethrow: throw;
            invocationExpression = InvocationExpression( this.MetaSyntaxFactory.SyntaxFactoryMethod( nameof(ThrowStatement) ) );
        }

        var addThrowExpression = this.WithCallToAddSimplifierAnnotation( invocationExpression );

        return addThrowExpression;
    }

    private InvocationExpressionSyntax ConvertToUserExpression( ExpressionSyntax expression )
        => InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetUserExpression) ) )
            .AddArgumentListArguments( Argument( expression ) );

    public override SyntaxNode? VisitLocalDeclarationStatement( LocalDeclarationStatementSyntax node )
    {
        var declaration = node.Declaration;

        if ( declaration.Variables.Count == 1 )
        {
            var declarator = declaration.Variables[0];

            if ( declarator.Initializer != null )
            {
                if ( this.IsCompileTimeDynamic( declarator.Initializer.Value ) )
                {
                    // Assigning dynamic to a variable.
                    return this.WithCallToAddSimplifierAnnotation( CreateInvocationExpression( declarator.Initializer.Value, false ) );
                }

                if ( declarator.Initializer is { Value: AwaitExpressionSyntax awaitExpression } && this.IsCompileTimeDynamic( awaitExpression.Expression ) )
                {
                    // Assigning awaited dynamic to a variable.
                    return this.WithCallToAddSimplifierAnnotation( CreateInvocationExpression( awaitExpression.Expression, true ) );
                }

                InvocationExpressionSyntax CreateInvocationExpression(
                    ExpressionSyntax expression,
                    bool awaitResult )
                {
                    return InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.DynamicLocalDeclaration) ) )
                        .AddArgumentListArguments(
                            Argument( (ExpressionSyntax) this.Visit( declaration.Type )! ),
                            Argument( this.Transform( declarator.Identifier ) ),
                            Argument( this.ConvertToUserExpression( (ExpressionSyntax) this.Visit( expression ).AssertNotNull() ) ),
                            Argument( LiteralExpression( awaitResult ? SyntaxKind.TrueLiteralExpression : SyntaxKind.FalseLiteralExpression ) ) );
                }
            }
        }

        return base.VisitLocalDeclarationStatement( node );
    }

    public override SyntaxNode? VisitIdentifierName( IdentifierNameSyntax node )
    {
        if ( node.Identifier.IsKind( SyntaxKind.IdentifierToken ) && !node.IsVar )
        {
            var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

            if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
            {
                // Fully qualifies simple identifiers.

                if ( symbol?.Kind == SymbolKind.TypeParameter
                     && symbol is ITypeParameterSymbol typeParameterSymbol
                     && this._templateMemberClassifier.IsRunTimeTemplateTypeParameter( typeParameterSymbol ) )
                {
                    // For run-time type parameters, use ITemplateSyntaxFactory.RunTimeTypeParameterIdentifier
                    // so the correct name is resolved at expansion time via TemplateExpansionContext.
                    return InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeTypeParameterIdentifier) ) )
                        .AddArgumentListArguments( Argument( SyntaxFactoryEx.LiteralExpression( typeParameterSymbol.Name ) ) );
                }
                else if ( symbol?.Kind is SymbolKind.Namespace or SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.DynamicType or SymbolKind.ErrorType
                         or SymbolKind.FunctionPointerType or SymbolKind.PointerType or SymbolKind.TypeParameter
                     && symbol is INamespaceOrTypeSymbol namespaceOrType )
                {
                    return this.Transform( this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( namespaceOrType ) );
                }
                else if ( symbol is { IsStatic: true } && !node.Parent.IsKind( SyntaxKind.SimpleMemberAccessExpression )
                                                       && !node.Parent.IsKind( SyntaxKind.AliasQualifiedName ) )
                {
                    switch ( symbol.Kind )
                    {
                        case SymbolKind.Field:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                        case SymbolKind.Method:
                            // We have an access to a field or method with a "using static", or a non-qualified static member access.

                            if ( symbol.Kind == SymbolKind.Method && symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } )
                            {
                                // If the method is a static local function, don't qualify it.
                                break;
                            }

                            if ( !this._templateMemberClassifier.SymbolClassifier.GetTemplateInfo( symbol ).IsNone )
                            {
                                // If the field is a template, assume it's an introduction and don't qualify it.
                                break;
                            }

                            return this.MetaSyntaxFactory.MemberAccessExpression(
                                this.MetaSyntaxFactory.Kind( SyntaxKind.SimpleMemberAccessExpression ),
                                this.Transform( this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( symbol.ContainingType ) ),
                                this.MetaSyntaxFactory.IdentifierName( SyntaxFactoryEx.LiteralExpression( node.Identifier.Text ) ) );
                    }
                }

                // When TryVisitNamespaceOrTypeName calls Transform with the result ot the syntax generator, Transform eventually
                // calls the current method for each compile-time parameter. We need to change it to the value of the template
                // parameter.
                else if ( this._templateCompileTimeTypeParameterNames.Contains( node.Identifier.ValueText ) )
                {
#pragma warning disable LAMA0850 // Intentional use: node.Identifier is a SyntaxToken, nameof() is well-known
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName( node.Identifier ),
                        IdentifierName( nameof(TemplateTypeArgument.Syntax) ) );
#pragma warning restore LAMA0850
                }
            }
            else
            {
                // This should qualify the identifier.
                return this._compileTimeOnlyRewriter.Visit( node )!;
            }
        }

        return base.VisitIdentifierName( node );
    }

    private ExpressionSyntax WithCallToAddSimplifierAnnotation( ExpressionSyntax expression )
        => InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.AddSimplifierAnnotations) ),
            ArgumentList( SingletonSeparatedList( Argument( expression ) ) ) );

    private ExpressionSyntax WithCallToSimplifyAnonymousFunction( ExpressionSyntax expression )
        => InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.SimplifyAnonymousFunction) ),
            ArgumentList( SingletonSeparatedList( Argument( expression ) ) ) );

    /// <summary>
    /// Transforms a type or namespace so that it is fully qualified, but return <c>false</c> if the input <paramref name="node"/>
    /// is not a type or namespace.
    /// </summary>
    /// <param name="node"></param>
    /// <param name="transformedNode"></param>
    /// <returns></returns>
    private bool TryVisitNamespaceOrTypeName( SyntaxNode node, [NotNullWhen( true )] out SyntaxNode? transformedNode )
    {
        var symbol = this._syntaxTreeAnnotationMap.GetSymbol( node );

        switch ( symbol?.Kind )
        {
            case SymbolKind.Namespace or SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.DynamicType or SymbolKind.ErrorType
                or SymbolKind.FunctionPointerType or SymbolKind.PointerType or SymbolKind.TypeParameter when symbol is INamespaceOrTypeSymbol namespaceOrType:
                // If we have a generic type, we do not write the generic arguments.
                var nameExpression = this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( namespaceOrType );

                transformedNode = this.GetTransformationKind( node ) == TransformationKind.Transform
                    ? this.WithCallToAddSimplifierAnnotation( this.Transform( nameExpression ) )
                    : nameExpression;

                // Keep the annotations if this type is in a typeof expression. Creating the runtime expression afterwards requires the annotation.
                if ( node.Parent.IsKind( SyntaxKind.TypeOfExpression ) )
                {
                    transformedNode = node.CopyAnnotationsTo( transformedNode );
                }

#pragma warning disable CS8762 // False positive.  
                return true;
#pragma warning restore CS8762

            default:
                transformedNode = null;

                return false;
        }
    }

    public override SyntaxNode? VisitQualifiedName( QualifiedNameSyntax node )
    {
        if ( this.TryVisitNamespaceOrTypeName( node, out var transformedNode ) )
        {
            return transformedNode;
        }
        else
        {
            return base.VisitQualifiedName( node );
        }
    }

    protected override ExpressionSyntax TransformQualifiedName( QualifiedNameSyntax node )
    {
        var transformed = base.TransformQualifiedName( node );

        if ( node.HasAnnotation( Simplifier.Annotation ) )
        {
            transformed = this.WithCallToAddSimplifierAnnotation( transformed );
        }

        return transformed;
    }

    public override SyntaxNode? VisitAliasQualifiedName( AliasQualifiedNameSyntax node )
    {
        if ( this.TryVisitNamespaceOrTypeName( node, out var transformedNode ) )
        {
            return transformedNode;
        }
        else
        {
            return base.VisitAliasQualifiedName( node );
        }
    }

    public override SyntaxNode? VisitGenericName( GenericNameSyntax node )
    {
        if ( this.TryVisitNamespaceOrTypeName( node, out var transformedTypeName ) )
        {
            return transformedTypeName;
        }
        else
        {
            return base.VisitGenericName( node );
        }
    }

    protected override ExpressionSyntax TransformConditionalExpression( ConditionalExpressionSyntax node )
    {
        if ( node.WhenFalse.IsKind( SyntaxKind.ThrowExpression ) || node.WhenTrue.IsKind( SyntaxKind.ThrowExpression ) )
        {
            // If any of the expressions if a throw exception, we cannot reduce it at compile time because it would generate incorrect syntax.
            return base.TransformConditionalExpression( node );
        }

        var transformedCondition = this.Transform( node.Condition );
        var transformedWhenTrue = this.Transform( node.WhenTrue );
        var transformedWhenFalse = this.Transform( node.WhenFalse );

        return InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ConditionalExpression) ),
            ArgumentList( SeparatedList( [Argument( transformedCondition ), Argument( transformedWhenTrue ), Argument( transformedWhenFalse )] ) ) );
    }

    protected override ExpressionSyntax TransformYieldStatement( YieldStatementSyntax node )
    {
        if ( node.IsKind( SyntaxKind.YieldReturnStatement ) && node.Expression.IsKind( SyntaxKind.InvocationExpression )
                                                            && node.Expression is InvocationExpressionSyntax invocation
                                                            && this._templateMemberClassifier.GetMetaMemberKind( invocation.Expression )
                                                            == MetaMemberKind.Proceed )
        {
            // We have a 'yield return meta.Proceed()' statement.

            return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ConditionalExpression) ) );
        }
        else
        {
            return base.TransformYieldStatement( node );
        }
    }

    protected override ExpressionSyntax TransformPostfixUnaryExpression( PostfixUnaryExpressionSyntax node )
    {
        if ( node.IsKind( SyntaxKind.SuppressNullableWarningExpression ) )
        {
            var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( node.Operand );

            if ( expressionType is { TypeKind: TypeKind.Dynamic } )
            {
                expressionType = null;
            }

            if ( node.Parent.AssertNotNull().TargetRequiresUserExpression() )
            {
                // We must return a UserExpression and not an ExpressionSyntax.

                return InvocationExpression(
                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.SuppressNullableWarningUserExpression) ) )
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList(
                            [
                                Argument( this.Transform( node.Operand ) ),
                                Argument( SyntaxFactoryEx.LiteralExpression( expressionType?.GetSerializableTypeId().Id ) )
                            ] ) ) )
                    .WithAdditionalAnnotations( _userExpressionAnnotation );
            }
            else
            {
                return InvocationExpression(
                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.SuppressNullableWarningExpression) ) )
                    .WithArgumentList(
                        ArgumentList(
                            SeparatedList(
                            [
                                Argument( this.Transform( node.Operand ) ),
                                Argument( SyntaxFactoryEx.LiteralExpression( expressionType?.GetSerializableTypeId().Id ) )
                            ] ) ) );
            }
        }
        else
        {
            return base.TransformPostfixUnaryExpression( node );
        }
    }

    public override SyntaxNode? VisitTypeOfExpression( TypeOfExpressionSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            return this.TransformTypeOfExpression( node );
        }
        else if ( this._syntaxTreeAnnotationMap.GetSymbol( node.Type )?.Kind is SymbolKind.NamedType or SymbolKind.ArrayType or SymbolKind.TypeParameter
                      or SymbolKind.DynamicType or SymbolKind.ErrorType or SymbolKind.PointerType or SymbolKind.FunctionPointerType
                  && this._syntaxTreeAnnotationMap.GetSymbol( node.Type ) is ITypeSymbol typeSymbol
                  && this._templateMemberClassifier.SymbolClassifier.GetTemplatingScope( typeSymbol ).GetExpressionValueScope() == TemplatingScope.RunTimeOnly )
        {
            var typeOfString = this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOfExpression( typeSymbol ).ToString();

            return this._typeOfRewriter.RewriteTypeOf(
                    typeSymbol,
                    this.CreateTypeParameterSubstitutionDictionary( nameof(TemplateTypeArgument.Type), this._dictionaryOfITypeType ) )
                .WithAdditionalAnnotations( new SyntaxAnnotation( _rewrittenTypeOfAnnotation, typeOfString ) );
        }

        return base.VisitTypeOfExpression( node );
    }

    protected override ExpressionSyntax TransformCastExpression( CastExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformCastExpression( node ) );

    protected override ExpressionSyntax TransformObjectCreationExpression( ObjectCreationExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformObjectCreationExpression( node ) );

    protected override ExpressionSyntax TransformParenthesizedExpression( ParenthesizedExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformParenthesizedExpression( node ) );

    protected override ExpressionSyntax TransformArrayCreationExpression( ArrayCreationExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformArrayCreationExpression( node ) );

    protected override ExpressionSyntax TransformParenthesizedLambdaExpression( ParenthesizedLambdaExpressionSyntax node )
        => this.WithCallToSimplifyAnonymousFunction( base.TransformParenthesizedLambdaExpression( node ) );

    protected override ExpressionSyntax TransformSimpleLambdaExpression( SimpleLambdaExpressionSyntax node )
        => this.WithCallToSimplifyAnonymousFunction( base.TransformSimpleLambdaExpression( node ) );

    protected override ExpressionSyntax TransformAnonymousMethodExpression( AnonymousMethodExpressionSyntax node )
        => this.WithCallToSimplifyAnonymousFunction( base.TransformAnonymousMethodExpression( node ) );

    protected override ExpressionSyntax TransformMemberAccessExpression( MemberAccessExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformMemberAccessExpression( node ) );

    protected override ExpressionSyntax TransformInvocationExpression( InvocationExpressionSyntax node )
        => this.WithCallToAddSimplifierAnnotation( base.TransformInvocationExpression( node ) );

    public override SyntaxNode? VisitCastExpression( CastExpressionSyntax node )
    {
        var expressionType = this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression );

        // Special processing of casting a run-time expression to IExpression.
        if ( expressionType is { TypeKind: TypeKind.Dynamic } )
        {
            var targetType = (ITypeSymbol?) this._syntaxTreeAnnotationMap.GetSymbol( node.Type );

            if ( targetType?.Kind == SymbolKind.NamedType && targetType is INamedTypeSymbol { Name: nameof(IExpression) } )
            {
                var expressionScope = node.Expression.GetScopeFromAnnotation();
                var transformedExpression = (ExpressionSyntax) this.Visit( node.Expression )!;

                if ( !(expressionType is { TypeKind: TypeKind.Dynamic } || this._runTimeCompilation.HasImplicitConversion( expressionType, targetType ))
                     || expressionScope?.GetExpressionExecutionScope() != TemplatingScope.CompileTimeOnly )
                {
                    var convertToRunTimeExpression = InvocationExpression(
                            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.GetUserExpression) ) )
                        .AddArgumentListArguments( Argument( transformedExpression ) );

                    return convertToRunTimeExpression;
                }
            }
        }

        return base.VisitCastExpression( node );
    }

    public override SyntaxNode? VisitAssignmentExpression( AssignmentExpressionSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            return this.TransformAssignmentExpression( node );
        }

        // Special processing of assigning a run-time expression to IExpression.
        if ( node.Right.GetScopeFromAnnotation()?.GetExpressionExecutionScope() == TemplatingScope.RunTimeOnly )
        {
            var leftType = this._syntaxTreeAnnotationMap.GetExpressionType( node.Left );

            if ( leftType?.Kind == SymbolKind.NamedType && leftType is INamedTypeSymbol { Name: nameof(IExpression) } )
            {
                var transformedRight = this.Transform( node.Right );

                var rightType = this._syntaxTreeAnnotationMap.GetExpressionType( node.Right );

                if ( rightType is null or { TypeKind: TypeKind.Dynamic } )
                {
                    rightType = this._runTimeCompilation.GetSpecialType( SpecialType.System_Object );
                }

                var runtimeExpressionInvocation = InvocationExpression(
                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                    .AddArgumentListArguments(
                        Argument( transformedRight ),
                        Argument( LiteralExpression( SyntaxKind.StringLiteralExpression, Literal( rightType.GetSerializableTypeId().Id ) ) ) );

                var userExpressionInvocation = InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        runtimeExpressionInvocation,
                        SyntaxFactoryEx.WellKnownIdentifierName( nameof(TypedExpressionSyntax.ToUserExpression) ) ),
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.Compilation) ) ) ) ) );

                var transformedLeft = (ExpressionSyntax) this.Visit( node.Left ).AssertNotNull();

                return AssignmentExpression( node.Kind(), transformedLeft, userExpressionInvocation );
            }
        }

        // Fallback to the default implementation.
        return base.VisitAssignmentExpression( node );
    }

    protected override ExpressionSyntax TransformAssignmentExpression( AssignmentExpressionSyntax node )
    {
        var transformedNode = base.TransformAssignmentExpression( node );

        return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RewriteAssignmentExpression) ) )
            .AddArgumentListArguments( Argument( transformedNode ) );
    }
}