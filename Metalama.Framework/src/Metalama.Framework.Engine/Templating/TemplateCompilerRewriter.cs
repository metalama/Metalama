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

    private TemplateMetaSyntaxFactoryImpl _templateMetaSyntaxFactory;
    private MetaContext? _currentMetaContext;
    private int _nextStatementListId;
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

        return IdentifierName( metaVariableIdentifier );
    }

    private IdentifierNameSyntax ReserveRunTimeVariableName( string name )
    {
        var metaVariableIdentifier = this._currentMetaContext!.GetTemplateVariableName( name );

        this.DeclareMetaVariable( name, metaVariableIdentifier );

        return IdentifierName( metaVariableIdentifier.AddMetaVariableAnnotation() );
    }

    private void DeclareMetaVariable( string hint, SyntaxToken metaVariableIdentifier )
    {
        var callGetUniqueIdentifier =
            this._templateMetaSyntaxFactory.GetUniqueIdentifier( hint );

        var localDeclaration =
            LocalDeclarationStatement(
                    VariableDeclaration( this.MetaSyntaxFactory.Type( typeof(SyntaxToken) ) )
                        .WithVariables(
                            SingletonSeparatedList(
                                VariableDeclarator( metaVariableIdentifier )
                                    .WithInitializer( EqualsValueClause( callGetUniqueIdentifier ) ) ) ) )
                .NormalizeWhitespace();

        this._currentMetaContext!.Statements.Add( localDeclaration );
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

            switch ( parent )
            {
                case null:
                    // This situation seems to happen only when Transform is called from a newly created syntax node,
                    // which has not been added to the syntax tree yet. Transform then calls Visit and, which then calls GetTransformationKind
                    // so we need to return Transform here. This is not nice and would need to be refactored.

                    return false;

                case IfStatementSyntax:
                case ElseClauseSyntax:
                case SwitchSectionSyntax:
                case ForEachStatementSyntax:
                case WhileStatementSyntax:
                case DoStatementSyntax:
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

            // If the tuple element has a name (i.e. it's not just ItemX), set it explicitly.
            if ( !tupleElement.Name.Equals( tupleElement.CorrespondingTupleField!.Name, StringComparison.Ordinal ) )
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
                if ( identifierSymbol is IParameterSymbol { Name: "_" } )
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

                return IdentifierName( declaredSymbolNameLocal.Text );
            }
            else if ( token.HasMetaVariableAnnotation() )
            {
                return IdentifierName( token );
            }
            else
            {
                // This is not a symbol declaration but a symbol reference.
            }
        }

        var transformedToken = base.Transform( token );

        var tokenKind = transformedToken.Kind().ToString();

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
        if ( node.ElementType is IdentifierNameSyntax identifier )
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
                return this.MetaSyntaxFactory.IdentifierName( IdentifierName( declaredSymbolNameLocal.Text ) );
            }
            else if ( identifierSymbol is IParameterSymbol parameterSymbol
                      && SymbolEqualityComparer.Default.Equals( parameterSymbol.ContainingSymbol, this._rootTemplateSymbol ) )
            {
                // We have a reference to a template parameter. Currently, only introductions can have template parameters, and these don't need
                // to be renamed.

                return base.TransformIdentifierName( node );
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
                                IdentifierName( "WithNameColon" ) ) )
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
                                           .FirstOrDefault( n => n is InvocationExpressionSyntax or BinaryExpressionSyntax )
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

        if ( symbol is IParameterSymbol parameter && this._templateMemberClassifier.IsRunTimeTemplateParameter( parameter ) )
        {
            // Run-time template parameters are always bound to a run-time meta-expression.
            return expression;
        }
        else if ( symbol is ITypeParameterSymbol typeParameter && this._templateMemberClassifier.IsCompileTimeTemplateTypeParameter( typeParameter ) )
        {
            return MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName( nameof(TemplateTypeArgument.Syntax) ) );
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
                literalExpression = this.MetaSyntaxFactory.LiteralExpression(
                    this.Transform( syntaxKind ),
                    this.MetaSyntaxFactory.Literal( expression ) );
            }

            return InvocationExpression( this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.RunTimeExpression) ) )
                .AddArgumentListArguments(
                    Argument( literalExpression ),
                    Argument(
                        LiteralExpression(
                            SyntaxKind.StringLiteralExpression,
                            Literal( expressionType.GetSerializableTypeId().Id ) ) ) );
        }

        if ( expressionType is IErrorTypeSymbol )
        {
            // There is a compile-time error. Return default.
            return LiteralExpression( SyntaxKind.DefaultLiteralExpression, Token( SyntaxKind.DefaultKeyword ) );
        }

        bool ExpressionTypeIsGenericDynamic()
        {
            return expressionType is INamedTypeSymbol { TypeArguments: [IDynamicTypeSymbol] };
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
                    .AddArgumentListArguments( Argument( expression ) );

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
                if ( expressionType.SpecialType == SpecialType.System_Object ||
                     this._serializableTypes.IsSerializable( expressionType, this._syntaxTreeAnnotationMap.GetLocation( expression ), this ) )
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
                                                                SyntaxKind.StringLiteralExpression,
                                                                Literal( name ) ) ) ) ) ),
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName( name ),
                                            IdentifierName( propertyName ) ) ) ) ) ) )
                .NormalizeWhitespace();
        }
    }

    public override SyntaxNode? VisitMemberAccessExpression( MemberAccessExpressionSyntax node )
    {
        if ( this.TryVisitNamespaceOrTypeName( node, out var transformedNode ) )
        {
            return transformedNode;
        }

        if ( this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression ) is IDynamicTypeSymbol &&
             !this._templateMemberClassifier.IsTemplateParameter( node.Expression ) )
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
            if ( this._syntaxTreeAnnotationMap.GetSymbol( node ) is IMethodSymbol { ReducedFrom: not null } method )
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

        if ( transformationKind != TransformationKind.Transform &&
             this._syntaxTreeAnnotationMap.GetExpressionType( node.Expression ) is IDynamicTypeSymbol )
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
        if ( node.Expression is AssignmentExpressionSyntax { Left: IdentifierNameSyntax identifier } assignment )
        {
            var identifierSymbol = this._syntaxTreeAnnotationMap.GetSymbol( identifier );

            if ( IsLocalSymbol( identifierSymbol ) || identifierSymbol is IDiscardSymbol )
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
                else if ( assignment.Right is AwaitExpressionSyntax awaitExpression && this.IsCompileTimeDynamic( awaitExpression.Expression ) )
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
        if ( node.Expression is ConditionalAccessExpressionSyntax conditionalAccessExpression
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

            if ( argumentSymbol is IParameterSymbol parameter && this._templateMemberClassifier.IsRunTimeTemplateParameter( parameter ) )
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
                        MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, expression, IdentifierName( nameof(this.ToString) ) ) );
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
                var returnStatement = ReturnStatement( node.ArgumentList.Arguments.SingleOrDefault()?.Expression );

                var transformedReturnStatement = (ExpressionSyntax) this.VisitReturnStatement( returnStatement );

                this.AddAddStatementStatement( node, transformedReturnStatement );

                return null;

            case MetaMemberKind.DefineLocalVariable:
                {
                    var arguments = new List<ArgumentSyntax>
                    {
                        Argument( IdentifierName( this._currentMetaContext.AssertNotNull().StatementListVariableName ) )
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

                var (receiver, name) = node.Expression switch
                {
                    SimpleNameSyntax simpleName => (null, simpleName),
                    MemberAccessExpressionSyntax memberAccess => (
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

                    this._currentMetaContext.Statements.Add( variableDeclaration );

                    receiver = IdentifierName( variableIdentifier );
                }

                if ( name is GenericNameSyntax genericName )
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
                        ? IdentifierName( compiledTemplateName )
                        : MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, receiver, IdentifierName( compiledTemplateName ) );

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
            if ( symbol is IMethodSymbol { ReducedFrom: not null } method )
            {
                if ( node.Expression is MemberAccessExpressionSyntax memberAccessExpression )
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

        if ( this.GetTransformationKind( node ) == TransformationKind.None &&
             this.GetTransformationKind( node.Expression ) == TransformationKind.Transform &&
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
                .AddArgumentListArguments( Argument( IdentifierName( this._currentMetaContext!.StatementListVariableName ) ) )
                .AddArgumentListArguments( arguments ) );

        addStatementStatement = this.DeepIndent(
            addStatementStatement.WithLeadingTrivia(
                this.GetCommentFromNode( node.Parent! )
                    .AddRange( addStatementStatement.GetLeadingTrivia() ) ) );

        this._currentMetaContext.Statements.Add( addStatementStatement );
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
        => Parameter( default, default, this._templateSyntaxFactoryType, Identifier( _templateSyntaxFactoryParameterName ), null );

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
                                IdentifierName( parameter.Identifier ),
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
            var isVoid = node.ReturnType is PredefinedTypeSyntax predefinedType && predefinedType.Keyword.IsKind( SyntaxKind.VoidKeyword );

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
                Parameter( default, default, SyntaxFactoryEx.ExpressionSyntaxType, Identifier( "value" ), null )
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
        if ( this._syntaxKind == TemplateCompilerSemantics.Initializer )
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
                Identifier( this._templateName ) )
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

        if ( templateSymbol is IMethodSymbol { AssociatedSymbol: null } )
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

                this._currentMetaContext!.Statements.AddRange( metaStatements );

                return Block( this._currentMetaContext.Statements );
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

        if ( node is ThrowExpressionSyntax throwExpression )
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
        switch ( node?.Parent )
        {
            case LocalFunctionStatementSyntax localFunction:
                var localFunctionSymbol = (IMethodSymbol?) this._syntaxTreeAnnotationMap.GetDeclaredSymbol( localFunction );

                if ( localFunctionSymbol == null )
                {
                    return null;
                }

                var returnType = localFunctionSymbol.ReturnType;

                return new FunctionLikeRunTimeBlockInfo( returnType, localFunctionSymbol.IsAsync );

            case AnonymousFunctionExpressionSyntax anonymousFunction:
                var anonymousFunctionSymbol = (IMethodSymbol?) this._syntaxTreeAnnotationMap.GetSymbol( anonymousFunction );

                if ( anonymousFunctionSymbol == null )
                {
                    return null;
                }

                return new FunctionLikeRunTimeBlockInfo( anonymousFunctionSymbol.ReturnType, anonymousFunctionSymbol.IsAsync );

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
        FunctionLikeRunTimeBlockInfo? localFunctionInfo = null )
    {
        using ( this.WithMetaContext( MetaContext.CreateForRunTimeBlock( this._currentMetaContext, $"__s{++this._nextStatementListId}" ) ) )
        {
            // List<StatementOrTrivia> statements = new List<StatementOrTrivia>();
            var listType = this.MetaSyntaxFactory.Type( typeof(List<StatementOrTrivia>) );

            this._currentMetaContext!.Statements.Add(
                LocalDeclarationStatement(
                        VariableDeclaration( listType )
                            .WithVariables(
                                SingletonSeparatedList(
                                    VariableDeclarator( Identifier( this._currentMetaContext.StatementListVariableName ) )
                                        .WithInitializer( EqualsValueClause( ObjectCreationExpression( listType, ArgumentList(), default ) ) ) ) ) )
                    .NormalizeWhitespace()
                    .WithLeadingTrivia( this.GetIndentation() )
                    .WithTrailingTrivia( LineFeed ) );

            var previousTemplateMetaSyntaxFactory = this._templateMetaSyntaxFactory;

            // If we are in a local function, use a different TemplateMetaSyntaxFactory. 
            if ( localFunctionInfo != null )
            {
                this._templateMetaSyntaxFactory = new TemplateMetaSyntaxFactoryImpl( _templateSyntaxFactoryLocalName );

                // var localSyntaxFactory = syntaxFactory.ForLocalFunction( "typeof(X)", map );
                var map = this.CreateTypeParameterSubstitutionDictionary( nameof(TemplateTypeArgument.Type), this._dictionaryOfITypeType );

                this._currentMetaContext!.Statements.Add(
                    LocalDeclarationStatement(
                            VariableDeclaration( this._templateSyntaxFactoryType )
                                .WithVariables(
                                    SingletonSeparatedList(
                                        VariableDeclarator( Identifier( _templateSyntaxFactoryLocalName ) )
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

            this._currentMetaContext.Statements.AddRange( this.ToMetaStatements( statements ) );

            this._templateMetaSyntaxFactory = previousTemplateMetaSyntaxFactory;

            // TemplateSyntaxFactory.ToStatementList( __s1 )
            var toArrayStatementExpression = InvocationExpression(
                this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.ToStatementList) ),
                ArgumentList( SingletonSeparatedList( Argument( IdentifierName( this._currentMetaContext.StatementListVariableName ) ) ) ) );

            if ( generateExpression )
            {
                // return TemplateSyntaxFactory.ToStatementArray( __s1 );

                var returnStatementSyntax = ReturnStatement( toArrayStatementExpression ).WithLeadingTrivia( this.GetIndentation() ).NormalizeWhitespace();
                this._currentMetaContext.Statements.Add( returnStatementSyntax );

                // Block( Func<SyntaxList<StatementSyntax>>( delegate { ... } )
                return this.DeepIndent(
                    this.MetaSyntaxFactory.Block(
                        SyntaxFactoryEx.Default,
                        InvocationExpression(
                            ObjectCreationExpression( this.MetaSyntaxFactory.Type( typeof(Func<SyntaxList<StatementSyntax>>) ) )
                                .NormalizeWhitespace()
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(
                                                AnonymousMethodExpression()
                                                    .WithBody(
                                                        Block( this._currentMetaContext.Statements )
                                                            .AddNoDeepIndentAnnotation() ) ) ) ) ) ) ) );
            }
            else
            {
                // return __s;
                this._currentMetaContext.Statements.Add(
                    ReturnStatement(
                        this.MetaSyntaxFactory.Block( SyntaxFactoryEx.Default, toArrayStatementExpression ).WithLeadingTrivia( this.GetIndentation() ) ) );

                return Block( this._currentMetaContext.Statements );
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
    private IEnumerable<StatementSyntax> ToMetaStatements( in SyntaxList<StatementSyntax> statements ) => statements.SelectMany( this.ToMetaStatements );

    /// <summary>
    /// Transforms a <see cref="StatementSyntax"/> of the source template into a single <see cref="StatementSyntax"/> for the compiled template.
    /// This method is guaranteed to return a single <see cref="StatementSyntax"/>. If the source statement results in several compiled statements,
    /// they will be wrapped into a block.
    /// </summary>
    private StatementSyntax ToMetaStatement( StatementSyntax statement )
    {
        var statements = this.ToMetaStatements( statement );

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
    /// <returns>A list of statements for the compiled template.</returns>
    private List<StatementSyntax> ToMetaStatements( StatementSyntax statement )
    {
        MetaContext newContext;

        if ( statement is BlockSyntax block )
        {
            // Push the compile-time template block.
            newContext = MetaContext.CreateForCompileTimeBlock( this._currentMetaContext! );

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
            // Push a new MetaContext so statements got added to a new list of statements, but
            // this MetaContext is neither a run-time nor a compile-time lexical scope. 
            newContext = MetaContext.CreateHelperContext( this._currentMetaContext! );

            using ( this.WithMetaContext( newContext ) )
            {
                ProcessStatement( statement );
            }
        }

        // Returns the statements collected during this call.
        return newContext.Statements;

        void ProcessStatement( StatementSyntax singleStatement )
        {
            var transformedNode = this.Visit( singleStatement );

            switch ( transformedNode )
            {
                case null:
                    break;

                case StatementSyntax statementSyntax:
                    // The statement is already build-time code so there is nothing to transform.

                    newContext.Statements.Add( statementSyntax.WithLeadingTrivia( this.GetIndentation() ) );

                    break;

                case ExpressionSyntax expressionSyntax:
                    {
                        // The statement is run-time code and has been transformed into an expression creating the StatementSyntax.
                        // We need to generate the code adding this code to the list of statements, i.e. `statements.Add( expression )`.

                        var leadingTrivia = TriviaList( this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia )
                            .AddRange( this.GetIndentation() )
                            .AddRange( this.GetCommentFromNode( singleStatement ) )
                            .Add( this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia )
                            .AddRange( this.GetIndentation() );

                        var eol = this.MetaSyntaxFactory.SyntaxGenerationContext.ElasticEndOfLineTrivia;
                        var trailingTrivia = TriviaList( eol, eol );

                        // TemplateSyntaxFactory.Add( __s, expression )
                        var add =
                            this.DeepIndent(
                                ExpressionStatement(
                                    InvocationExpression(
                                        this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.AddStatement) ),
                                        ArgumentList(
                                            SeparatedList(
                                            [
                                                Argument( IdentifierName( this._currentMetaContext!.StatementListVariableName ) ),
                                                Argument( expressionSyntax )
                                            ] ) ) ) ) );

                        newContext.Statements.Add( add.WithLeadingTrivia( leadingTrivia ).WithTrailingTrivia( trailingTrivia ) );

                        break;
                    }

                default:
                    throw new AssertionFailedException( $"Unexpected node kind {transformedNode.Kind()} at '{singleStatement.GetLocation()};." );
            }
        }
    }

    protected override ExpressionSyntax TransformInterpolatedStringExpression( InterpolatedStringExpressionSyntax node )
    {
        List<ExpressionSyntax> transformedContents = new( node.Contents.Count );

        foreach ( var content in node.Contents )
        {
            switch ( content )
            {
                case InterpolatedStringTextSyntax text:
                    transformedContents.Add( this.TransformInterpolatedStringText( text ) );

                    break;

                case InterpolationSyntax interpolation:
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

        var fixedNode = InvocationExpression(
            this._templateMetaSyntaxFactory.TemplateSyntaxFactoryMember( nameof(ITemplateSyntaxFactory.FixInterpolationSyntax) ),
            ArgumentList( SingletonSeparatedList( Argument( transformedNode ) ) ) );

        return fixedNode;
    }

    public override SyntaxNode? VisitInterpolation( InterpolationSyntax node )
    {
        var transformedNode = base.VisitInterpolation( node );

        if ( transformedNode is InterpolationSyntax transformedInterpolation )
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
                var transformedStatements = this.ToMetaStatements( section.Statements ).ToMutableList();

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

    public override SyntaxNode VisitIfStatement( IfStatementSyntax node )
    {
        if ( this.GetTransformationKind( node ) == TransformationKind.Transform )
        {
            // Run-time if. Just serialize to syntax.
            return this.TransformIfStatement( node );
        }
        else
        {
            var transformedStatement = this.ToMetaStatement( node.Statement );
            var transformedElseStatement = node.Else != null ? this.ToMetaStatement( node.Else.Statement ) : null;

            // The condition may contains constructs like typeof or nameof that need to be transformed.
            var condition = (ExpressionSyntax) this.Visit( node.Condition )!;

            // If the statement is not a block, wrap it in a block, to ensure chains of if-else-if statements are properly nested.
            if ( transformedStatement is not BlockSyntax )
            {
                transformedStatement = Block( transformedStatement );
            }

            if ( transformedElseStatement is not null and not BlockSyntax )
            {
                transformedElseStatement = Block( transformedElseStatement );
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
        var runTimeCondition =
            this.GetTransformationKind( node.Condition ) == TransformationKind.Transform ||
            node.Condition.GetScopeFromAnnotation().GetValueOrDefault().GetExpressionValueScope() == TemplatingScope.RunTimeOnly ||
            node.WhenTrue is ThrowExpressionSyntax ||
            node.WhenFalse is ThrowExpressionSyntax;

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
            var transformedStatement = this.ToMetaStatement( node.Statement );

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
            var transformedStatement = this.ToMetaStatement( node.Statement );

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

        var statement = this.ToMetaStatement( node.Statement );

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
               && (
                   this._syntaxTreeAnnotationMap.GetExpressionType( expression ) is IDynamicTypeSymbol
                   || this._syntaxTreeAnnotationMap.GetExpressionType( expression ) is INamedTypeSymbol
                   {
                       Name: "Task" or "ConfiguredTaskAwaitable" or "IEnumerable" or "IAsyncEnumerator", TypeArguments: [IDynamicTypeSymbol]
                   });
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
        else if ( node.Expression is AwaitExpressionSyntax awaitExpression && this.IsCompileTimeDynamic( awaitExpression.Expression ) )
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

        return this.WithCallToAddSimplifierAnnotation( invocationExpression );
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

                if ( symbol is INamespaceOrTypeSymbol namespaceOrType )
                {
                    return this.Transform( this.MetaSyntaxFactory.SyntaxGenerationContext.SyntaxGenerator.TypeOrNamespace( namespaceOrType ) );
                }
                else if ( symbol is { IsStatic: true } && node.Parent is not MemberAccessExpressionSyntax && node.Parent is not AliasQualifiedNameSyntax )
                {
                    switch ( symbol.Kind )
                    {
                        case SymbolKind.Field:
                        case SymbolKind.Property:
                        case SymbolKind.Event:
                        case SymbolKind.Method:
                            // We have an access to a field or method with a "using static", or a non-qualified static member access.

                            if ( symbol is IMethodSymbol { MethodKind: MethodKind.LocalFunction } )
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
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName( node.Identifier ),
                        IdentifierName( nameof(TemplateTypeArgument.Syntax) ) );
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

        switch ( symbol )
        {
            case INamespaceOrTypeSymbol namespaceOrType:
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
        if ( node.WhenFalse is ThrowExpressionSyntax || node.WhenTrue is ThrowExpressionSyntax )
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
        if ( node.Kind() == SyntaxKind.YieldReturnStatement && node.Expression is InvocationExpressionSyntax invocation &&
             this._templateMemberClassifier.GetMetaMemberKind( invocation.Expression ) == MetaMemberKind.Proceed )
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
        if ( node.Kind() == SyntaxKind.SuppressNullableWarningExpression )
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
        else if ( this._syntaxTreeAnnotationMap.GetSymbol( node.Type ) is ITypeSymbol typeSymbol &&
                  this._templateMemberClassifier.SymbolClassifier.GetTemplatingScope( typeSymbol ).GetExpressionValueScope() == TemplatingScope.RunTimeOnly )
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

            if ( targetType is INamedTypeSymbol { Name: nameof(IExpression) } )
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

            if ( leftType is INamedTypeSymbol { Name: nameof(IExpression) } )
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
                        IdentifierName( nameof(TypedExpressionSyntax.ToUserExpression) ) ),
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
}