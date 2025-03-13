// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.CompileTimeContracts;
using Metalama.Framework.Engine.Advising;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.CodeModel.References;
using Metalama.Framework.Engine.Diagnostics;
using Metalama.Framework.Engine.Formatting;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.SyntaxSerialization;
using Metalama.Framework.Engine.Templating.Expressions;
using Metalama.Framework.Engine.Templating.MetaModel;
using Metalama.Framework.Engine.Transformations;
using Metalama.Framework.Engine.Utilities;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Engine.Utilities.UserCode;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using SpecialType = Metalama.Framework.Code.SpecialType;

namespace Metalama.Framework.Engine.Templating;

internal sealed partial class TemplateExpansionContext : UserCodeExecutionContext
{
    private readonly TemplateMember<IMethod>? _methodTemplate;
    private readonly Func<TemplateKind, IUserExpression>? _proceedExpressionProvider;
    private readonly TemplateClassProvider _templateClassProvider;
    private readonly LocalFunctionInfo? _localFunctionInfo;

    private static SyntaxGenerationContext? CurrentSyntaxGenerationContextOrNull
        => (CurrentOrNull as TemplateExpansionContext)?.SyntaxGenerationContext ??
           _currentSyntaxSerializationContext.Value?.SyntaxGenerationContext;

    /// <summary>
    /// Gets the current <see cref="SyntaxGenerationContext"/> or throws <see cref="InvalidOperationException"/>
    /// if the context is not available.
    /// </summary>
    internal static SyntaxGenerationContext CurrentSyntaxGenerationContext
        => CurrentSyntaxGenerationContextOrNull
           ?? throw new InvalidOperationException( "TemplateExpansionContext.CurrentSyntaxGenerationContext has not be set." );

    private static readonly AsyncLocal<SyntaxSerializationContext?> _currentSyntaxSerializationContext = new();

    private static SyntaxSerializationContext? CurrentSyntaxSerializationContextOrNull
        => (CurrentOrNull as TemplateExpansionContext)?.SyntaxSerializationContext
           ?? _currentSyntaxSerializationContext.Value;

    /// <summary>
    /// Gets the current <see cref="SyntaxSerializationContext"/>  or throws <see cref="InvalidOperationException"/>
    /// if the context is not available.
    /// </summary>
    internal static SyntaxSerializationContext CurrentSyntaxSerializationContext
        => CurrentSyntaxSerializationContextOrNull
           ?? throw new InvalidOperationException( "TemplateExpansionContext.CurrentSyntaxSerializationContext has not been set." );

    internal static IDeclaration? CurrentTargetDeclaration => (CurrentOrNull as TemplateExpansionContext)?.TargetDeclaration;

    internal static AspectLayerId? CurrentAspectLayerId => CurrentOrNull?.AspectLayerId;

    internal static bool IsTransformingDeclaration( IDeclaration declaration )
    {
        var metaApi = (CurrentOrNull as TemplateExpansionContext)?.MetaApi;

        if ( metaApi == null )
        {
            return false;
        }

        declaration = declaration.ForCompilation( metaApi.Compilation );

        if ( metaApi.Declaration.Equals( declaration ) || metaApi.Declaration.ContainingDeclaration?.Equals( declaration ) == true )
        {
            return true;
        }

        switch ( metaApi.Declaration )
        {
            case IProperty property:
                return declaration.Equals( property.GetMethod ) || declaration.Equals( property.SetMethod );

            case IEvent @event:
                return declaration.Equals( @event.AddMethod ) || declaration.Equals( @event.RemoveMethod ) || declaration.Equals( @event.RaiseMethod );
        }

        return false;
    }

    /// <summary>
    /// Sets the <see cref="CurrentSyntaxSerializationContext"/> but not the <see cref="UserCodeExecutionContext.Current"/> property.
    /// This method is used in tests, when the <see cref="CurrentSyntaxSerializationContext"/> property is needed but not the <see cref="UserCodeExecutionContext.Current"/>
    /// one.
    /// </summary>
    internal static IDisposable WithTestingContext( SyntaxSerializationContext serializationContext, in ProjectServiceProvider serviceProvider )
    {
        var handle = WithContext(
            new UserCodeExecutionContext(
                serviceProvider,
                default,
                serializationContext.CompilationContext,
                new AspectLayerId( "(test)" ) ) );

        _currentSyntaxSerializationContext.Value = serializationContext;

        return new DisposeCookie(
            () =>
            {
                handle.Dispose();
                _currentSyntaxSerializationContext.Value = null;
            } );
    }

    public TemplateLexicalScope LexicalScope { get; }

    public ITemplateSyntaxFactory SyntaxFactory { get; }

    public IReadOnlyDictionary<string, IType> TemplateGenericArguments { get; }

    public TemplateExpansionContext(
        TransformationContext transformationContext,
        MetaApi metaApi,
        IDeclaration declarationForLexicalScope,
        BoundTemplateMethod methodTemplate,
        Func<TemplateKind, IUserExpression>? proceedExpressionProvider,
        AspectLayerId aspectLayerId ) : this(
        transformationContext.ServiceProvider,
        metaApi,
        transformationContext.LexicalScopeProvider.GetLexicalScope( declarationForLexicalScope.ToFullRef() ),
        transformationContext.SyntaxGenerationContext,
        methodTemplate,
        methodTemplate.TemplateProvider,
        proceedExpressionProvider,
        aspectLayerId ) { }

    public TemplateExpansionContext(
        TransformationContext transformationContext,
        MetaApi metaApi,
        IDeclaration declarationForLexicalScope,
        TemplateProvider templateProvider,
        AspectLayerId aspectLayerId ) : this(
        transformationContext.ServiceProvider,
        metaApi,
        transformationContext.LexicalScopeProvider.GetLexicalScope( declarationForLexicalScope.ToFullRef() ),
        transformationContext.SyntaxGenerationContext,
        null,
        templateProvider,
        null,
        aspectLayerId ) { }

    // Resharper disable MemberCanBePrivate.Global
    internal TemplateExpansionContext(
        ProjectServiceProvider serviceProvider,
        MetaApi metaApi,
        TemplateLexicalScope lexicalScope,
        SyntaxGenerationContext syntaxGenerationContext,
        BoundTemplateMethod? methodTemplate,
        TemplateProvider templateProvider,
        Func<TemplateKind, IUserExpression>? proceedExpressionProvider,
        AspectLayerId aspectLayerId ) : base(
        serviceProvider,
        UserCodeDescription.Create(
            "executing the template method '{0}' in the context of the aspect '{1}' applied to '{2}'",
            methodTemplate?.TemplateMember.Symbol,
            metaApi.AspectInstance?.AspectClass.FullName,
            metaApi.AspectInstance?.TargetDeclaration ),
        syntaxGenerationContext.CompilationContext,
        aspectLayerId,
        (CompilationModel?) metaApi.Compilation,
        metaApi.Target.Declaration,
        diagnostics: metaApi.Diagnostics,
        metaApi: metaApi )
    {
        this._methodTemplate = methodTemplate?.TemplateMember;
        this.TemplateProvider = templateProvider;
        this.SyntaxSerializationService = serviceProvider.GetRequiredService<SyntaxSerializationService>();
        this.SyntaxSerializationContext = new SyntaxSerializationContext( metaApi.Compilation, syntaxGenerationContext, metaApi.Type );
        this.SyntaxGenerationContext = syntaxGenerationContext;
        this.LexicalScope = lexicalScope;
        this._proceedExpressionProvider = proceedExpressionProvider;
        this._templateClassProvider = serviceProvider.GetRequiredService<TemplateClassProvider>();

        var templateTypeArguments = ImmutableDictionary<string, IType>.Empty.ToBuilder();

        if ( methodTemplate != null )
        {
            templateTypeArguments.AddRange(
                methodTemplate.TemplateArguments.OfType<TemplateTypeArgumentFactory>().Select( x => new KeyValuePair<string, IType>( x.Name, x.Type ) ) );
        }

        if ( metaApi.Target.Declaration is IMethod { TypeParameters.Count: > 0 } targetMethod )
        {
            // Generic method - we need to add type parameters as named arguments for correct serializable id resolution.
            // Any target method type parameter that matches name of template argument can be skipped - template will not have a runtime type parameter of that name.

            // TODO: This presumes mapping of type parameters by name, more appropriate place would be to have a map in BoundTemplateMethod, but there is currently no other use for that.

            foreach ( var targetTypeParameter in targetMethod.TypeParameters )
            {
                if ( !templateTypeArguments.ContainsKey( targetTypeParameter.Name ) )
                {
                    templateTypeArguments.Add( targetTypeParameter.Name, targetTypeParameter );
                }
            }
        }

        this.TemplateGenericArguments = templateTypeArguments.ToImmutable();

        this.SyntaxFactory = new TemplateSyntaxFactoryImpl( this );
    }

    private TemplateExpansionContext( TemplateExpansionContext prototype, LocalFunctionInfo localFunctionInfo ) : base( prototype )
    {
        this._methodTemplate = prototype._methodTemplate;
        this.SyntaxSerializationService = prototype.SyntaxSerializationService;
        this.SyntaxSerializationContext = prototype.SyntaxSerializationContext;
        this.SyntaxGenerationContext = prototype.SyntaxGenerationContext;
        this.LexicalScope = prototype.LexicalScope;
        this.SyntaxFactory = prototype.SyntaxFactory;
        this._localFunctionInfo = localFunctionInfo;
        this.TemplateGenericArguments = prototype.TemplateGenericArguments;
        this._proceedExpressionProvider = prototype._proceedExpressionProvider;
        this._templateClassProvider = prototype._templateClassProvider;
        this.TemplateProvider = prototype.TemplateProvider;
    }

    private TemplateExpansionContext( TemplateExpansionContext prototype, TemplateMember<IMethod> methodTemplate, TemplateProvider templateProvider )
        : base( prototype )
    {
        this._methodTemplate = methodTemplate;
        this.SyntaxSerializationService = prototype.SyntaxSerializationService;
        this.SyntaxSerializationContext = prototype.SyntaxSerializationContext;
        this.SyntaxGenerationContext = prototype.SyntaxGenerationContext;
        this.LexicalScope = prototype.LexicalScope;
        this.SyntaxFactory = new TemplateSyntaxFactoryImpl( this );
        this._localFunctionInfo = prototype._localFunctionInfo;
        this.TemplateGenericArguments = prototype.TemplateGenericArguments;
        this._proceedExpressionProvider = prototype._proceedExpressionProvider;
        this._templateClassProvider = prototype._templateClassProvider;
        this.TemplateProvider = templateProvider.IsNull ? prototype.TemplateProvider : templateProvider;
    }

    public TemplateProvider TemplateProvider { get; }

    public SyntaxSerializationService SyntaxSerializationService { get; }

    public SyntaxSerializationContext SyntaxSerializationContext { get; }

    public SyntaxGenerationContext SyntaxGenerationContext { get; }

    public ContextualSyntaxGenerator SyntaxGenerator => this.SyntaxGenerationContext.SyntaxGenerator;

    public new MetaApi MetaApi => base.MetaApi!;

    public StatementSyntax CreateReturnStatement( ExpressionSyntax? returnExpression, bool awaitResult )
    {
        if ( returnExpression == null )
        {
            Invariant.Assert( !awaitResult );

            return ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );
        }

        switch ( this.MetaApi.Declaration )
        {
            case IField field:
                // This is field initializer template expansion.
                Invariant.Assert( !awaitResult );

                return this.CreateReturnStatementDefault( returnExpression, this._localFunctionInfo?.ReturnType ?? field.Type, false );

            case IProperty property:
                // This is property initializer template expansion.
                Invariant.Assert( !awaitResult );

                return this.CreateReturnStatementDefault( returnExpression, this._localFunctionInfo?.ReturnType ?? property.Type, false );

            case IEvent @event:
                // This is event initializer template expansion.
                Invariant.Assert( !awaitResult );

                return this.CreateReturnStatementDefault( returnExpression, this._localFunctionInfo?.ReturnType ?? @event.Type, false );

            default:
                {
                    if ( this._localFunctionInfo == null )
                    {
                        var method = this.MetaApi.Method;
                        var returnType = method.ReturnType;

                        if ( this._methodTemplate != null && this._methodTemplate.MustInterpretAsAsyncTemplate() )
                        {
                            // If we are in an awaitable async method, the consider the return type as seen by the method body,
                            // not the one as seen from outside.
                            var asyncInfo = method.GetAsyncInfoImpl();

                            if ( asyncInfo.IsAwaitableOrVoid )
                            {
                                returnType = asyncInfo.ResultType;
                            }
                        }

                        if ( returnType.Equals( SpecialType.Void ) )
                        {
                            return CreateReturnStatementVoid( returnExpression );
                        }
                        else if ( method.GetIteratorInfoImpl() is
                                      { EnumerableKind: EnumerableKind.IAsyncEnumerable or EnumerableKind.IAsyncEnumerator } iteratorInfo &&
                                  this._methodTemplate != null && this._methodTemplate.MustInterpretAsAsyncIteratorTemplate() )
                        {
                            switch ( iteratorInfo.EnumerableKind )
                            {
                                case EnumerableKind.IAsyncEnumerable:

                                    return this.CreateReturnStatementAsyncEnumerable( returnExpression );

                                case EnumerableKind.IAsyncEnumerator:
                                    return this.CreateReturnStatementAsyncEnumerator( returnExpression );

                                default:
                                    throw new AssertionFailedException( $"Unexpected EnumerableKind: {iteratorInfo.EnumerableKind}." );
                            }
                        }
                        else
                        {
                            return this.CreateReturnStatementDefault( returnExpression, returnType, awaitResult );
                        }
                    }
                    else
                    {
                        var returnType = this._localFunctionInfo.ReturnType;

                        if ( this._localFunctionInfo.IsAsync )
                        {
                            returnType = returnType.GetAsyncInfo().ResultType;
                        }

                        if ( returnType.Equals( SpecialType.Void ) )
                        {
                            return CreateReturnStatementVoid( returnExpression );
                        }
                        else
                        {
                            return this.CreateReturnStatementDefault( returnExpression, returnType, awaitResult );
                        }
                    }
                }
        }
    }

    private StatementSyntax CreateReturnStatementDefault( ExpressionSyntax returnExpression, IType returnType, bool awaitResult )
    {
        if ( returnExpression.Kind() is SyntaxKind.DefaultLiteralExpression or SyntaxKind.NullLiteralExpression )
        {
            // If we need to return null or default, there is no need to emit a cast.
            Invariant.Assert( !awaitResult );

            return
                ReturnStatement(
                    Token( SyntaxKind.ReturnKeyword ).WithTrailingTrivia( ElasticSpace ),
                    returnExpression,
                    Token( SyntaxKind.SemicolonToken ) );
        }
        else
        {
            var compilation = returnType.GetCompilationModel();

            var expression = awaitResult
                ? AwaitExpression( Token( SyntaxKind.AwaitKeyword ).WithTrailingTrivia( ElasticSpace ), returnExpression )
                : returnExpression;

            if ( TypeAnnotationMapper.TryFindExpressionTypeFromAnnotation(
                     returnExpression,
                     compilation,
                     out var expressionType ) &&
                 compilation.Comparers.Default.IsConvertibleTo( expressionType, returnType, ConversionKind.Implicit ) )
            {
                // No need to emit a cast.
            }
            else if ( returnExpression.IgnoreSuppressNullWarning().Kind() is SyntaxKind.NullLiteralExpression or SyntaxKind.DefaultLiteralExpression )
            {
                // No need to emit a cast.
            }
            else
            {
                expression = this.SyntaxGenerator.CastExpression( returnType, expression );
            }

            return ReturnStatement(
                Token( SyntaxKind.ReturnKeyword ).WithTrailingTrivia( ElasticSpace ),
                expression,
                Token( SyntaxKind.SemicolonToken ) );
        }
    }

    private StatementSyntax CreateReturnStatementAsyncEnumerable( ExpressionSyntax returnExpression )
    {
        // We are in an async iterator (or async stream), and we cannot have a return statement.
        // Generate this instead:
        // foreach ( var r in result ) { yield return r; }

        var resultItem = this.LexicalScope.GetUniqueIdentifier( "r" );

        // TODO: Possible optimization
        // We could assume that the result is an AsyncEnumerableList. The class also implements IEnumerable without
        // performance overhead. It is more efficient to do a foreach than an `await foreach`, so we do it.
        // However, the user may have stored the result in a differently-typed variable, so we need to cast.

        var forEach = ForEachStatement(
                SyntaxFactoryEx.VarIdentifier(),
                Identifier( resultItem ),
                returnExpression,
                Block(
                    SingletonList<StatementSyntax>(
                        YieldStatement(
                            SyntaxKind.YieldReturnStatement,
                            IdentifierName( resultItem ) ) ) ) )
            .WithAwaitKeyword( Token( SyntaxKind.AwaitKeyword ) );

        return Block( forEach, CreateYieldBreakStatement() ).WithFlattenBlockAnnotation();
    }

    private StatementSyntax CreateReturnStatementAsyncEnumerator( ExpressionSyntax returnExpression )
    {
        // We are in an async iterator (or async stream), and we cannot have a return statement.
        // Generate this instead:
        // await using ( var enumerator = METHOD() )
        // {
        //     while ( await enumerator.MoveNextAsync() )
        //     {
        //         yield return enumerator.Current;
        //             
        //         cancellationToken.ThrowIfCancellationRequested();
        //     }
        // }

        // TODO: Possible optimization
        // We could assume that the result is an AsyncEnumerableList. The class also implements IEnumerable without
        // performance overhead. It is more efficient to do a foreach than an `await foreach`, so we do it.
        // However, the user may have stored the result in a differently-typed variable, so we need to cast.

        /*
         * ExpressionStatement(
                                InvocationExpression(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        IdentifierName("cancellationToken"),
                                        IdentifierName("ThrowIfCancellationRequested"))))
         */

        VariableDeclarationSyntax? local;
        ExpressionSyntax? usingExpression;
        IdentifierNameSyntax? enumeratorIdentifier;

        if ( returnExpression is IdentifierNameSyntax returnIdentifier )
        {
            local = null;
            usingExpression = returnExpression;
            enumeratorIdentifier = returnIdentifier;
        }
        else
        {
            var enumerator = this.LexicalScope.GetUniqueIdentifier( "enumerator" );

            local = VariableDeclaration( SyntaxFactoryEx.VarIdentifier() )
                .WithVariables(
                    SingletonSeparatedList(
                        VariableDeclarator( Identifier( enumerator ) )
                            .WithInitializer( EqualsValueClause( returnExpression ) ) ) );

            usingExpression = null;
            enumeratorIdentifier = IdentifierName( enumerator );
        }

        var whileStatement = WhileStatement(
            AwaitExpression(
                InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        enumeratorIdentifier,
                        IdentifierName( "MoveNextAsync" ) ) ) ),
            Block(
                YieldStatement(
                    SyntaxKind.YieldReturnStatement,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        enumeratorIdentifier,
                        IdentifierName( "Current" ) ) ) ) );

        var usingStatement = UsingStatement(
            Token( SyntaxKind.AwaitKeyword ),
            Token( SyntaxKind.UsingKeyword ),
            Token( SyntaxKind.OpenParenToken ),
            local!,
            usingExpression!,
            Token( SyntaxKind.CloseParenToken ),
            Block( whileStatement ) );

        return Block( usingStatement, CreateYieldBreakStatement() )
            .WithFlattenBlockAnnotation();
    }

    private static YieldStatementSyntax CreateYieldBreakStatement()
        => YieldStatement( SyntaxKind.YieldBreakStatement ).WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );

    private static StatementSyntax CreateReturnStatementVoid( ExpressionSyntax? returnExpression )
    {
        // We have a void method, so we cannot a emit a "return X" where X is an expression.
        // However, we have an expression, and it may have a side effect, therefore we cannot just drop X.
        // We try to detect if the can have a side effect. If yes, we add it as an expression statement.

        switch ( returnExpression )
        {
            case PostfixUnaryExpressionSyntax { RawKind: (int) SyntaxKind.SuppressNullableWarningExpression, Operand: var operand }:
                // We're ignoring the value, so we don't care about its nullability and removing the ! operator might bring further opportunities for simplification.
                return CreateReturnStatementVoid( operand );

            case ConditionalAccessExpressionSyntax { WhenNotNull: InvocationExpressionSyntax }:
            case InvocationExpressionSyntax:
            // Do not use discard on invocations, because it may be void.

            case AwaitExpressionSyntax:
                // We have to await in a statement, then return in another statement.
                return
                    Block(
                            ExpressionStatement( returnExpression ),
                            ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );

            case null:
            case LiteralExpressionSyntax:
            case IdentifierNameSyntax:
                // No need to call the expression  because we are guaranteed to have no side effect and we don't 
                // care about the value.
                return ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );

            default:
                // Anything else should use discard.
                return
                    Block(
                            SyntaxFactoryEx.DiscardStatement(
                                CurrentSyntaxSerializationContext.SyntaxGenerator.SafeCastExpression(
                                    PredefinedType( Token( SyntaxKind.ObjectKeyword ) ),
                                    returnExpression ) ),
                            ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation ) )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
        }
    }

    public UserDiagnosticSink DiagnosticSink => this.MetaApi.Diagnostics;

    public StatementSyntax CreateReturnStatement( IUserExpression? returnUserExpression, bool awaitResult )
    {
        if ( returnUserExpression == null )
        {
            return ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );
        }
        else
        {
            if ( returnUserExpression.Type.Equals( SpecialType.Void ) )
            {
                var returnType = this._localFunctionInfo?.ReturnType ?? this.MetaApi.Method.ReturnType;

                if ( returnType.Equals( SpecialType.Void )
                     || returnType.GetAsyncInfo().ResultType.Equals( SpecialType.Void ) )
                {
                    var returnStatement = ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation );

                    // Special case for default(void), which cannot be an expression statement
                    // (because it's not a valid expression in the first place, but is useful in templates).
                    if ( returnUserExpression is TypedDefaultUserExpression )
                    {
                        return returnStatement;
                    }

                    var returnExpression = returnUserExpression
                        .ToExpressionSyntax( this.SyntaxSerializationContext, returnType )
                        .RemoveParenthesis();

                    return Block( ExpressionStatement( returnExpression ), returnStatement )
                        .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                }
                else
                {
                    this.Diagnostics.Report(
                        TemplatingDiagnosticDescriptors.CannotConvertProceedReturnToType.CreateRoslynDiagnostic(
                            this.TargetDeclaration?.GetDiagnosticLocation(),
                            (
                                this.AspectLayerId!.Value.AspectShortName,
                                this.TargetDeclaration!,
                                returnUserExpression,
                                returnUserExpression.Type, returnType) ) );

                    return SyntaxFactoryEx.EmptyStatement;
                }
            }
            else if ( awaitResult && returnUserExpression.Type.GetAsyncInfo().ResultType.Equals( SpecialType.Void ) )
            {
                Invariant.Assert(
                    this._methodTemplate != null && (this._methodTemplate.MustInterpretAsAsyncTemplate() || this._localFunctionInfo is { IsAsync: true }) );

                var returnType = this._localFunctionInfo?.ReturnType ?? this.MetaApi.Method.ReturnType;

                if ( returnType.Equals( SpecialType.Void )
                     || returnType.GetAsyncInfo().ResultType.Equals( SpecialType.Void ) )
                {
                    return
                        Block(
                                ExpressionStatement(
                                    AwaitExpression(
                                        Token( SyntaxKind.AwaitKeyword ).WithTrailingTrivia( Space ),
                                        returnUserExpression.ToExpressionSyntax( this.SyntaxSerializationContext, returnType ) ) ),
                                ReturnStatement().WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                }
                else
                {
                    return
                        Block(
                                ExpressionStatement(
                                    AwaitExpression(
                                        Token( SyntaxKind.AwaitKeyword ).WithTrailingTrivia( Space ),
                                        returnUserExpression.ToExpressionSyntax( this.SyntaxSerializationContext, returnType ) ) ),
                                ReturnStatement( SyntaxFactoryEx.Default ).WithAdditionalAnnotations( FormattingAnnotations.PossibleRedundantAnnotation ) )
                            .WithLinkerGeneratedFlags( LinkerGeneratedFlags.FlattenableBlock );
                }
            }
            else
            {
                return this.CreateReturnStatement( returnUserExpression.ToExpressionSyntax( this.SyntaxSerializationContext ), awaitResult );
            }
        }
    }

    public IUserExpression Proceed( string methodName )
    {
        if ( this._proceedExpressionProvider == null )
        {
            throw new AssertionFailedException( "No proceed expression was provided." );
        }

        return new ProceedUserExpression( methodName, this );
    }

    public static IUserExpression ConfigureAwait( IUserExpression expression, bool continueOnCapturedContext )
        => new ConfigureAwaitUserExpression( expression, continueOnCapturedContext );

    public TemplateExpansionContext ForLocalFunction( LocalFunctionInfo localFunctionInfo ) => new( this, localFunctionInfo );

    internal TemplateExpansionContext ForTemplate( TemplateMember<IMethod> template, TemplateProvider templateProvider )
        => new( this, template, templateProvider );

    internal BlockSyntax AddYieldBreakIfNecessary( BlockSyntax block )
    {
        if ( this._methodTemplate?.MustInterpretAsAsyncIteratorTemplate() != true )
        {
            return block;
        }

        if ( new HasAnyYieldVisitor().Visit( block ) )
        {
            return block;
        }

        return block.AddStatements( CreateYieldBreakStatement() );
    }

    private sealed class HasAnyYieldVisitor : SafeSyntaxVisitor<bool>
    {
        public override bool DefaultVisit( SyntaxNode node )
        {
            foreach ( var child in node.ChildNodesAndTokens() )
            {
                if ( child.AsNode() is StatementSyntax statement )
                {
                    if ( this.Visit( statement ) )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override bool VisitYieldStatement( YieldStatementSyntax node ) => true;
    }

    public TemplateClass GetTemplateClass( TemplateProvider templateProvider )
    {
        if ( templateProvider.IsNull )
        {
            return this._methodTemplate.AssertNotNull().TemplateClassMember.TemplateClass;
        }

        return this._templateClassProvider.Get( templateProvider );
    }

    public void CheckTemplateLanguageVersion<T>( TemplateExpansionContext context, TemplateMember<T> templateMember )
        where T : class, IMemberOrNamedType
    {
        var requiredLanguageVersion = templateMember.TemplateClassMember.TemplateInfo.UsedApiVersion?.ToLanguageVersion();
        var targetLanguageVersion = ((CSharpParseOptions?) this.TargetDeclaration?.GetPrimarySyntaxTree()?.Options)?.LanguageVersion;

        if ( requiredLanguageVersion > targetLanguageVersion )
        {
            var aspectClass = context.MetaApi.AspectInstance?.AspectClass;

            this.Diagnostics.Report(
                TemplatingDiagnosticDescriptors.AspectUsesHigherCSharpVersion.CreateRoslynDiagnostic(
                    this.TargetDeclaration?.GetDiagnosticLocation(),
                    (aspectClass?.ShortName, requiredLanguageVersion.Value.ToDisplayString(),
                     targetLanguageVersion.Value.ToDisplayString(), templateMember.GetDeclaration( context.Compilation.AssertNotNull() )),
                    deduplicationKey: aspectClass?.FullName ) );
        }
    }

    private sealed class DisposeCookie : IDisposable
    {
        private readonly Action _action;

        public DisposeCookie( Action action )
        {
            this._action = action;
        }

        public void Dispose() => this._action();
    }
}