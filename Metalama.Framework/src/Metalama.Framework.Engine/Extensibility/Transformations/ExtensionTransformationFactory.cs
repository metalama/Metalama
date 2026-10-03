// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AspectOrdering;
using Metalama.Framework.Engine.Aspects;
using Metalama.Framework.Engine.CodeModel;
using Metalama.Framework.Engine.Linking;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Framework.Fabrics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Extensibility.Transformations;

/// <summary>
/// Creates linker transformations on behalf of a <see cref="PipelineExtension"/>: it redirects source call sites and method references to other
/// methods.
/// </summary>
/// <remarks>
/// <para>
/// The engine creates one instance per high-level stage and passes it to the transforming hook of every extension through
/// <see cref="ExtensionTransformationContext.TransformationFactory"/>. The instance validates every request and throws an
/// <see cref="ArgumentException"/> for a request that it cannot honor.
/// </para>
/// <para>
/// The factory does not change the code model. The rewritten call sites are not visible to aspects and do not appear in design-time generated code.
/// </para>
/// </remarks>
[PublicAPI]
public sealed class ExtensionTransformationFactory
{
    private readonly object _sync = new();
    private readonly CompilationModel _compilation;
    private readonly IReadOnlyList<OrderedAspectLayer> _aspectLayers;
    private readonly SyntaxGenerationOptions _syntaxGenerationOptions;
    private readonly Dictionary<SyntaxTree, Dictionary<SyntaxNode, CallSiteRedirection>> _redirections = new();
    private int _nextRedirectionId;
    private bool _isCompleted;

    internal ExtensionTransformationFactory(
        CompilationModel compilation,
        IReadOnlyList<OrderedAspectLayer> aspectLayers,
        SyntaxGenerationOptions syntaxGenerationOptions )
    {
        this._compilation = compilation;
        this._aspectLayers = aspectLayers;
        this._syntaxGenerationOptions = syntaxGenerationOptions;
    }

    /// <summary>
    /// Gets the compilation that results from all aspects of the stage. In the first stage, its syntax trees are those of the source compilation.
    /// </summary>
    public ICompilation Compilation => this._compilation;

    /// <summary>
    /// Requests that a source invocation be replaced by an invocation of another method.
    /// </summary>
    /// <param name="origin">The aspect or fabric that requested the redirection.</param>
    /// <param name="request">The request.</param>
    /// <exception cref="ArgumentException">The request is not valid.</exception>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same call site, or the factory was completed.</exception>
    public void RedirectInvocation( ExtensionContributionOrigin origin, InvocationRedirectionRequest request )
    {
        var callSite = request.CallSite;
        var annotation = this.GetGeneratedCodeAnnotation( origin );
        var semanticModel = this.GetSemanticModel( callSite );
        var context = this._compilation.CompilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, callSite );

        if ( semanticModel.GetOperation( callSite ) is not IInvocationOperation { TargetMethod: { } sourceMethod } operation
             || sourceMethod.MethodKind is not (MethodKind.Ordinary or MethodKind.ReducedExtension) )
        {
            throw new ArgumentException( $"The call site '{callSite}' is not an invocation of an ordinary or extension method.", nameof(request) );
        }

        var targetMethod = request.Target.Method;

        if ( !targetMethod.IsStatic )
        {
            throw new ArgumentException( $"The target method '{targetMethod}' must be static.", nameof(request) );
        }

        // The receiver of a call to a classic extension method in reduced form is its first argument, whose syntax is not an ArgumentSyntax.
        var isReducedExtensionCall = sourceMethod.IsExtensionMethod && operation.Arguments is [{ Syntax: not ArgumentSyntax }, ..];
        var hasReceiverValue = isReducedExtensionCall || operation.Instance != null;
        var isConditionalAccess = GetConditionalAccessBinding( callSite.Expression ) != null;
        var isBaseCall = callSite.Expression.Kind() == SyntaxKind.SimpleMemberAccessExpression
                         && ((MemberAccessExpressionSyntax) callSite.Expression).Expression.Kind() == SyntaxKind.BaseExpression;
        var argumentPlan = default(ImmutableArray<CallSiteArgumentPlanItem>?);

        switch ( request.ReceiverMode )
        {
            case CallSiteReceiverMode.Drop:
                if ( hasReceiverValue && request.Arguments.IsDefault )
                {
                    throw new ArgumentException(
                        $"The receiver of '{callSite}' must be passed, because the method is not static. Use another receiver mode or pass {nameof(RedirectedArgument)}.{nameof(RedirectedArgument.SourceReceiver)}.",
                        nameof(request) );
                }

                if ( isConditionalAccess )
                {
                    throw new ArgumentException( $"The receiver mode {request.ReceiverMode} cannot be used in the conditional access '{callSite}'.", nameof(request) );
                }

                if ( isBaseCall && IsVirtual( sourceMethod ) && !request.Arguments.IsDefault
                     && request.Arguments.Any( a => a.Kind == RedirectedArgumentKind.SourceReceiver ) )
                {
                    throw new ArgumentException( $"The base call '{callSite}' to a virtual method cannot pass its receiver.", nameof(request) );
                }

                break;

            case CallSiteReceiverMode.FirstArgument or CallSiteReceiverMode.FirstArgumentByRef or CallSiteReceiverMode.FirstArgumentByIn:
                if ( !hasReceiverValue )
                {
                    throw new ArgumentException( $"The call site '{callSite}' has no receiver to pass.", nameof(request) );
                }

                if ( isConditionalAccess )
                {
                    throw new ArgumentException(
                        $"The receiver mode {request.ReceiverMode} cannot be used in the conditional access '{callSite}', because the receiver exists only inside the conditional access. Use {nameof(CallSiteReceiverMode.ExtensionReceiver)}.",
                        nameof(request) );
                }

                if ( isBaseCall && IsVirtual( sourceMethod ) )
                {
                    throw new ArgumentException( $"The base call '{callSite}' to a virtual method cannot pass its receiver.", nameof(request) );
                }

                break;

            case CallSiteReceiverMode.ExtensionReceiver:
                if ( targetMethod.Parameters is not [{ IsThis: true }, ..] )
                {
                    throw new ArgumentException( $"The receiver mode {request.ReceiverMode} requires an extension method, but '{targetMethod}' is not one.", nameof(request) );
                }

                if ( !hasReceiverValue )
                {
                    throw new ArgumentException( $"The call site '{callSite}' has no receiver to pass.", nameof(request) );
                }

                if ( isBaseCall )
                {
                    throw new ArgumentException( $"The receiver mode {request.ReceiverMode} cannot be used with the base call '{callSite}'.", nameof(request) );
                }

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(request), $"Unexpected receiver mode: {request.ReceiverMode}." );
        }

        if ( request.ResultCast != null )
        {
            if ( IsInConditionalAccess( callSite ) )
            {
                throw new ArgumentException( $"A result cast cannot be written inside the conditional access of '{callSite}'.", nameof(request) );
            }

            if ( targetMethod.ReturnType.SpecialType == Code.SpecialType.Void )
            {
                throw new ArgumentException( $"A result cast cannot be written for '{callSite}', because the target method '{targetMethod}' returns void.", nameof(request) );
            }

            // A cast expression is not a statement, so the call must be used as a value. The body of an expression-bodied member or lambda that
            // returns void is also an expression statement in the operation tree.
            if ( operation.Parent is IExpressionStatementOperation )
            {
                throw new ArgumentException( $"A result cast cannot be written for '{callSite}', because the call is a statement.", nameof(request) );
            }
        }

        // The rewrite discards the invoked expression, except the receiver, and the separators of the argument list when the arguments are
        // planned. A directive in a discarded part would leave the directives of the file unbalanced.
        if ( CallSiteRedirection.HasDirectiveInInnerTrivia( callSite, callSite.Expression ) )
        {
            throw new ArgumentException( $"The invoked expression of '{callSite}' contains a preprocessor directive.", nameof(request) );
        }

        if ( !request.Arguments.IsDefault )
        {
            if ( CallSiteRedirection.HasDirectiveInInnerTrivia( callSite, callSite.ArgumentList ) )
            {
                throw new ArgumentException(
                    $"The argument list of '{callSite}' contains a preprocessor directive, so its arguments cannot be rearranged.",
                    nameof(request) );
            }

            argumentPlan = CreateArgumentPlan( request, operation, isReducedExtensionCall, hasReceiverValue, context );
        }

        var callee = request.ReceiverMode == CallSiteReceiverMode.ExtensionReceiver
            ? CreateMethodName( targetMethod, request.TypeArguments, context )
            : CreateStaticCallee( request.Target, request.TypeArguments, context );

        var extraArguments = request.ExtraArguments.IsDefaultOrEmpty
            ? ImmutableArray<ArgumentSyntax>.Empty
            : request.ExtraArguments.SelectAsImmutableArray(
                a => Argument( NameColon( SyntaxFactoryEx.SafeIdentifierName( a.ParameterName ) ), default, a.Value ).WithAdditionalAnnotations( annotation ) );

        var resultCast = request.ResultCast == null ? null : context.SyntaxGenerator.TypeSyntax( request.ResultCast );

        CallSiteRedirection CreateRedirection( int id )
            => new(
                id,
                callSite,
                CallSiteRedirectionKind.Invocation,
                request.ReceiverMode,
                callee.WithAdditionalAnnotations( annotation ),
                argumentPlan,
                extraArguments,
                resultCast,
                request.Description ?? $"the call '{callSite}' redirected to '{targetMethod}' by {origin.DiagnosticSourceDescription}" );

        VerifyBinding( semanticModel, operation, CreateRedirection( -1 ).Rewrite( callSite ), targetMethod, context );

        this.AddRedirection( callSite, CreateRedirection );
    }

    /// <summary>
    /// Verifies that the rewritten call binds to the target method at the position of the call site, and throws an <see cref="ArgumentException"/>
    /// otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rewritten call is bound speculatively, exactly as the linker writes it, without the redirections nested in its arguments. Overload
    /// resolution can select another method than the target, for instance an overload with fewer optional parameters, or an instance method of the
    /// receiver when the target is an extension method. The linker would then emit a call to that method without notice.
    /// </para>
    /// <para>
    /// A call in a conditional access cannot be bound alone, because its receiver exists only in the conditional access. Its receiver is replaced by
    /// <c>default(T)</c>, where <c>T</c> is the type of the receiver in the conditional access, which gives the same member lookup and the same
    /// overload resolution. The check is skipped when <c>T</c> cannot be named.
    /// </para>
    /// </remarks>
    private static void VerifyBinding(
        SemanticModel semanticModel,
        IInvocationOperation operation,
        ExpressionSyntax rewrittenCall,
        IMethod targetMethod,
        SyntaxGenerationContext context )
    {
        if ( targetMethod.GetSymbol() is not { } targetSymbol )
        {
            // An introduced method has no symbol in the compilation of the call site.
            return;
        }

        var callSite = (InvocationExpressionSyntax) operation.Syntax;
        var expression = rewrittenCall;

        // Remove the result cast, ((T)(call)).
        while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.CastExpression )
        {
            expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                ? ((ParenthesizedExpressionSyntax) expression).Expression
                : ((CastExpressionSyntax) expression).Expression;
        }

        if ( !expression.IsKind( SyntaxKind.InvocationExpression ) )
        {
            return;
        }

        var invocation = (InvocationExpressionSyntax) expression;

        if ( GetConditionalAccessBinding( invocation.Expression ) is { } binding )
        {
            // The receiver of the binding is the expression of the innermost conditional access that contains the call site.
            if ( GetConditionalAccessBinding( callSite.Expression )?.FirstAncestorOrSelf<ConditionalAccessExpressionSyntax>() is not { } conditionalAccess
                 || semanticModel.GetTypeInfo( conditionalAccess.Expression ).Type is not { } receiverType )
            {
                return;
            }

            // In the conditional access, a receiver of a nullable value type has its underlying type, and a receiver of a reference type is not null.
            if ( receiverType.Kind == SymbolKind.NamedType
                 && receiverType.OriginalDefinition.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Nullable_T )
            {
                receiverType = ((INamedTypeSymbol) receiverType).TypeArguments[0];
            }

            receiverType = receiverType.WithNullableAnnotation( NullableAnnotation.NotAnnotated );

            if ( !CanBeNamed( receiverType ) )
            {
                return;
            }

            var defaultReceiver = DefaultExpression( context.SyntaxGenerator.TypeSyntax( receiverType ) );

            ExpressionSyntax replacement = binding.Kind() == SyntaxKind.MemberBindingExpression
                ? MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, defaultReceiver, ((MemberBindingExpressionSyntax) binding).Name )
                : ElementAccessExpression( defaultReceiver, ((ElementBindingExpressionSyntax) binding).ArgumentList );

            invocation = invocation.ReplaceNode( binding, replacement );
        }

        var symbolInfo = semanticModel.GetSpeculativeSymbolInfo( callSite.SpanStart, invocation, SpeculativeBindingOption.BindAsExpression );

        if ( symbolInfo.Symbol is { Kind: SymbolKind.Method } and IMethodSymbol boundMethod
             && SymbolEqualityComparer.Default.Equals( (boundMethod.ReducedFrom ?? boundMethod).OriginalDefinition, targetSymbol.OriginalDefinition ) )
        {
            return;
        }

        // The message is also embedded in the diagnostics of the clients of the factory, so it does not repeat the call site.
        var message = symbolInfo.Symbol != null
            ? $"The rewritten call binds to '{symbolInfo.Symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' instead of '{targetMethod}'."
            : $"The rewritten call does not bind to '{targetMethod}' ({symbolInfo.CandidateReason}).";

        throw new ArgumentException( message );
    }

    /// <summary>
    /// Verifies that the method group of the target, written in place of the source method group, converts to the same delegate or function
    /// pointer type and binds to the target method, and throws an <see cref="ArgumentException"/> otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The conversion of a method group depends on its context: an overload of the target can be selected for the delegate type, and a method
    /// group whose delegate type is inferred (<c>var d = M;</c>) has no natural type when the target is overloaded. The method group is therefore
    /// bound in its statement, initializer or expression body, with a speculative semantic model.
    /// </para>
    /// <para>
    /// The check is skipped when the target has no symbol in the compilation of the method group, which is the case of an introduced method, and
    /// when the method group is in a context that has no speculative semantic model, for instance an attribute argument.
    /// </para>
    /// </remarks>
    private static void VerifyMethodReferenceBinding(
        SemanticModel semanticModel,
        ExpressionSyntax node,
        IMethodReferenceOperation operation,
        ExpressionSyntax callee,
        IMethod targetMethod )
    {
        if ( targetMethod.GetSymbol() is not { } targetSymbol )
        {
            return;
        }

        var marker = new SyntaxAnnotation();
        var replacement = callee.WithAdditionalAnnotations( marker );
        SemanticModel? speculativeModel;
        SyntaxNode speculativeRoot;

        if ( node.FirstAncestorOrSelf<StatementSyntax>() is { } statement )
        {
            var newStatement = statement.ReplaceNode( node, replacement );
            speculativeRoot = newStatement;

            if ( !semanticModel.TryGetSpeculativeSemanticModel( statement.SpanStart, newStatement, out speculativeModel ) )
            {
                return;
            }
        }
        else if ( node.FirstAncestorOrSelf<EqualsValueClauseSyntax>() is { } initializer )
        {
            var newInitializer = initializer.ReplaceNode( node, replacement );
            speculativeRoot = newInitializer;

            if ( !semanticModel.TryGetSpeculativeSemanticModel( initializer.SpanStart, newInitializer, out speculativeModel ) )
            {
                return;
            }
        }
        else if ( node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() is { } arrow )
        {
            var newArrow = arrow.ReplaceNode( node, replacement );
            speculativeRoot = newArrow;

            if ( !semanticModel.TryGetSpeculativeSemanticModel( arrow.SpanStart, newArrow, out speculativeModel ) )
            {
                return;
            }
        }
        else
        {
            return;
        }

        var newNode = speculativeRoot.GetAnnotatedNodes( marker ).Single();
        var newOperation = speculativeModel.GetOperation( newNode );

        if ( newOperation is IMethodReferenceOperation { Method: { } boundMethod } newMethodReference
             && SymbolEqualityComparer.Default.Equals( boundMethod.OriginalDefinition, targetSymbol.OriginalDefinition )
             && SymbolEqualityComparer.Default.Equals( newMethodReference.Parent?.Type, operation.Parent?.Type ) )
        {
            return;
        }

        // The message is also embedded in the diagnostics of the clients of the factory, so it does not repeat the method group.
        var message = newOperation is IMethodReferenceOperation { Method: { } otherMethod }
                      && !SymbolEqualityComparer.Default.Equals( otherMethod.OriginalDefinition, targetSymbol.OriginalDefinition )
            ? $"The rewritten method group binds to '{otherMethod.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' instead of '{targetMethod}'."
            : $"The rewritten method group does not convert to '{operation.Parent?.Type?.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' with '{targetMethod}'.";

        throw new ArgumentException( message );
    }

    /// <summary>
    /// Determines whether a type can be written in C#, which excludes anonymous types and the types that contain them.
    /// </summary>
    private static bool CanBeNamed( ITypeSymbol type )
        => type.TypeKind switch
        {
            Microsoft.CodeAnalysis.TypeKind.Error => false,
            Microsoft.CodeAnalysis.TypeKind.Array => CanBeNamed( ((IArrayTypeSymbol) type).ElementType ),
            Microsoft.CodeAnalysis.TypeKind.Pointer => CanBeNamed( ((IPointerTypeSymbol) type).PointedAtType ),
            _ when type.IsAnonymousType => false,
            _ when type.Kind == SymbolKind.NamedType => ((INamedTypeSymbol) type).TypeArguments.All( CanBeNamed )
                                                        && (type.ContainingType == null || CanBeNamed( type.ContainingType )),
            _ => true
        };

    /// <summary>
    /// Requests that a source method group, converted to a delegate or to a function pointer, be replaced by a method group of another method.
    /// </summary>
    /// <param name="origin">The aspect or fabric that requested the redirection.</param>
    /// <param name="request">The request.</param>
    /// <exception cref="ArgumentException">The request is not valid.</exception>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same method group, or the factory was completed.</exception>
    public void RedirectMethodReference( ExtensionContributionOrigin origin, MethodReferenceRedirectionRequest request )
    {
        var annotation = this.GetGeneratedCodeAnnotation( origin );
        var node = NormalizeNode( request.MethodReference );
        var semanticModel = this.GetSemanticModel( node );

        if ( semanticModel.GetOperation( node ) is not IMethodReferenceOperation { Method: { } sourceMethod } operation
             || operation.Parent is not (IDelegateCreationOperation or IAddressOfOperation) )
        {
            throw new ArgumentException( $"The node '{node}' is not a method group converted to a delegate or to a function pointer.", nameof(request) );
        }

        if ( request.ReceiverMode != CallSiteReceiverMode.Drop )
        {
            throw new ArgumentException( $"The receiver mode {request.ReceiverMode} is not supported for a method reference.", nameof(request) );
        }

        if ( !sourceMethod.IsStatic || sourceMethod.MethodKind == MethodKind.ReducedExtension || operation.Instance != null )
        {
            throw new ArgumentException( $"The method group '{node}' has a receiver, so it cannot be redirected with the receiver mode {request.ReceiverMode}.", nameof(request) );
        }

        if ( !request.Target.Method.IsStatic )
        {
            throw new ArgumentException( $"The target method '{request.Target.Method}' must be static.", nameof(request) );
        }

        // The rewrite replaces the whole method group, so a directive inside it would leave the directives of the file unbalanced.
        if ( CallSiteRedirection.HasDirectiveInInnerTrivia( node ) )
        {
            throw new ArgumentException( $"The method group '{node}' contains a preprocessor directive.", nameof(request) );
        }

        var context = this._compilation.CompilationContext.GetSyntaxGenerationContext( this._syntaxGenerationOptions, node );
        var callee = CreateStaticCallee( request.Target, request.TypeArguments, context ).WithAdditionalAnnotations( annotation );

        VerifyMethodReferenceBinding( semanticModel, node, operation, callee, request.Target.Method );

        this.AddRedirection(
            node,
            id => new CallSiteRedirection(
                id,
                node,
                CallSiteRedirectionKind.MethodReference,
                request.ReceiverMode,
                callee,
                null,
                ImmutableArray<ArgumentSyntax>.Empty,
                null,
                request.Description ?? $"the method reference '{node}' redirected to '{request.Target.Method}' by {origin.DiagnosticSourceDescription}" ) );
    }

    /// <summary>
    /// Determines whether a redirection was already requested for a call site or a method group.
    /// </summary>
    /// <remarks>
    /// As in <see cref="RedirectMethodReference"/>, a simple name that is the name of a member access designates the member access.
    /// </remarks>
    public bool IsRedirected( ExpressionSyntax callSite )
    {
        var node = NormalizeNode( callSite );

        lock ( this._sync )
        {
            return this._redirections.TryGetValue( node.SyntaxTree, out var redirections ) && redirections.ContainsKey( node );
        }
    }

    /// <summary>
    /// Returns the member access of which a node is the name, or the node itself.
    /// </summary>
    private static ExpressionSyntax NormalizeNode( ExpressionSyntax node )
        => node.Parent.IsKind( SyntaxKind.SimpleMemberAccessExpression ) && node.Parent is MemberAccessExpressionSyntax parentMemberAccess
                                                                       && parentMemberAccess.Name == node
            ? parentMemberAccess
            : node;

    /// <summary>
    /// Freezes the factory and returns the linker input. Called by the pipeline stage after all extensions.
    /// </summary>
    internal ExtensionLinkerInput Complete()
    {
        lock ( this._sync )
        {
            this._isCompleted = true;

            if ( this._redirections.Count == 0 )
            {
                return ExtensionLinkerInput.Empty;
            }

            return new ExtensionLinkerInput(
                this._redirections.ToDictionary(
                    x => x.Key,
                    x => (IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>) x.Value ) );
        }
    }

    private void AddRedirection( SyntaxNode node, Func<int, CallSiteRedirection> createRedirection )
    {
        lock ( this._sync )
        {
            this.ThrowIfCompleted();

            if ( !this._redirections.TryGetValue( node.SyntaxTree, out var redirections ) )
            {
                redirections = new Dictionary<SyntaxNode, CallSiteRedirection>( SyntaxNodeReferenceComparer.Instance );
                this._redirections.Add( node.SyntaxTree, redirections );
            }

            if ( redirections.ContainsKey( node ) )
            {
                throw new InvalidOperationException( $"A redirection was already requested for '{node}'." );
            }

            redirections.Add( node, createRedirection( this._nextRedirectionId++ ) );
        }
    }

    private void ThrowIfCompleted()
    {
        if ( this._isCompleted )
        {
            throw new InvalidOperationException( "The factory of transformations can no longer be used, because the transforming hook has completed." );
        }
    }

    private SemanticModel GetSemanticModel( SyntaxNode node )
    {
        lock ( this._sync )
        {
            this.ThrowIfCompleted();
        }

        var syntaxTree = node.SyntaxTree;

        if ( !this._compilation.PartialCompilation.TryGetSyntaxTree( syntaxTree.GetDocumentKey(), out var compilationTree )
             || !ReferenceEquals( compilationTree, syntaxTree ) )
        {
            throw new ArgumentException( $"The node '{node}' does not belong to a syntax tree of the compilation of the stage." );
        }

        return this._compilation.CompilationContext.SemanticModelProvider.GetSemanticModel( syntaxTree );
    }

    /// <summary>
    /// Validates the aspect layer of the origin and returns the annotation that marks the syntax that the origin generates.
    /// </summary>
    private SyntaxAnnotation GetGeneratedCodeAnnotation( ExtensionContributionOrigin origin )
    {
        _ = origin ?? throw new ArgumentNullException( nameof(origin) );

        // A project or namespace fabric is processed by the top-level fabric aspect class, whose layer is identified by the type of Fabric.
        var layer = this._aspectLayers.FirstOrDefault( l => l.AspectLayerId == origin.AspectLayerId )
                    ?? (origin.Predecessor.Kind == AspectPredecessorKind.Fabric
                        ? this._aspectLayers.FirstOrDefault( l => l.AspectName == typeof(Fabric).FullName )
                        : null)
                    ?? throw new ArgumentException(
                        $"The aspect layer '{origin.AspectLayerId}' of the origin is not an ordered layer of the pipeline.",
                        nameof(origin) );

        return origin.AspectInstance?.AspectClass is IAspectClassImpl aspectClass
            ? aspectClass.GeneratedCodeAnnotation
            : layer.AspectClass.GeneratedCodeAnnotation;
    }

    private static ImmutableArray<CallSiteArgumentPlanItem> CreateArgumentPlan(
        InvocationRedirectionRequest request,
        IInvocationOperation operation,
        bool isReducedExtensionCall,
        bool hasReceiverValue,
        SyntaxGenerationContext context )
    {
        var callSite = request.CallSite;
        var targetParameters = request.Target.Method.Parameters;
        var parameterOffset = request.ReceiverMode == CallSiteReceiverMode.Drop ? 0 : 1;

        if ( request.Arguments.Length > targetParameters.Count - parameterOffset )
        {
            throw new ArgumentException( $"The argument list has more elements than the parameters of '{request.Target.Method}'.", nameof(request) );
        }

        // Map each parameter of the source method to the index of its argument in the source argument list.
        var sourceArgumentIndices = new Dictionary<int, int>();
        var sourceArgumentSyntaxes = callSite.ArgumentList.Arguments;

        foreach ( var argument in operation.Arguments )
        {
            if ( argument is { ArgumentKind: ArgumentKind.Explicit, Parameter: { } parameter, Syntax: ArgumentSyntax argumentSyntax } )
            {
                sourceArgumentIndices[parameter.Ordinal] = sourceArgumentSyntaxes.IndexOf( argumentSyntax );
            }
        }

        // The elements of an expanded params argument are the source arguments that no parameter maps. They are written last.
        var expandedParams = operation.Arguments.FirstOrDefault( a => a.ArgumentKind is ArgumentKind.ParamArray or ArgumentKind.ParamCollection );

        var expandedElements = expandedParams == null
            ? ImmutableArray<int>.Empty
            : Enumerable.Range( 0, sourceArgumentSyntaxes.Count ).Where( i => !sourceArgumentIndices.ContainsValue( i ) ).ToImmutableArray();

        var usedSourceArguments = new HashSet<int>();
        var receiverUsed = false;
        var names = new HashSet<string>( StringComparer.Ordinal );
        var items = new List<(int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType)>();

        for ( var i = 0; i < request.Arguments.Length; i++ )
        {
            var argument = request.Arguments[i];
            var name = argument.Name ?? targetParameters[i + parameterOffset].Name;

            // The linker writes the argument with this name, so the parameter of this name receives it.
            var receivingParameter = targetParameters.FirstOrDefault( p => p.Name == name ) ?? targetParameters[i + parameterOffset];

            if ( !names.Add( name ) )
            {
                throw new ArgumentException( $"The argument list names the parameter '{name}' twice.", nameof(request) );
            }

            if ( argument.CastType != null && (argument.Kind != RedirectedArgumentKind.SourceArgument || (isReducedExtensionCall && argument.ParameterOrdinal == 0)) )
            {
                throw new ArgumentException( $"The argument '{name}' cannot be cast, because only a source argument other than the receiver can be cast.", nameof(request) );
            }

            switch ( argument.Kind )
            {
                case RedirectedArgumentKind.SourceReceiver:
                    if ( request.ReceiverMode != CallSiteReceiverMode.Drop || receiverUsed )
                    {
                        throw new ArgumentException( "The receiver of the source call site is passed more than once.", nameof(request) );
                    }

                    if ( isReducedExtensionCall || !hasReceiverValue )
                    {
                        throw new ArgumentException(
                            $"The call site '{callSite}' has no receiver, or it calls an extension method in reduced form, whose receiver is its first argument and is passed with {nameof(RedirectedArgument)}.{nameof(RedirectedArgument.SourceArgument)}( 0 ).",
                            nameof(request) );
                    }

                    receiverUsed = true;

                    // The receiver is evaluated before the arguments.
                    items.Add( (-1, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceReceiver, -1, null, name ), receivingParameter, null) );

                    break;

                case RedirectedArgumentKind.SourceArgument:
                    if ( isReducedExtensionCall && argument.ParameterOrdinal == 0 )
                    {
                        if ( request.ReceiverMode != CallSiteReceiverMode.Drop || receiverUsed )
                        {
                            throw new ArgumentException( "The receiver of the source call site is passed more than once.", nameof(request) );
                        }

                        receiverUsed = true;
                        items.Add( (-1, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceReceiver, -1, null, name ), receivingParameter, null) );

                        break;
                    }

                    if ( expandedParams != null && argument.ParameterOrdinal == expandedParams.Parameter!.Ordinal )
                    {
                        if ( argument.CastType != null )
                        {
                            throw new ArgumentException( $"The expanded params argument of the call site '{callSite}' cannot be cast.", nameof(request) );
                        }

                        if ( expandedElements.Any( e => !usedSourceArguments.Add( e ) ) )
                        {
                            throw new ArgumentException( $"The params argument of the call site '{callSite}' is passed more than once.", nameof(request) );
                        }

                        if ( receivingParameter.RefKind != Code.RefKind.None )
                        {
                            throw new ArgumentException(
                                $"The params argument of the call site '{callSite}' cannot be passed to the parameter '{receivingParameter.Name}', which is passed by reference.",
                                nameof(request) );
                        }

                        // An empty collection has no evaluation, so it is written after the other values of the call site.
                        var order = expandedElements.IsEmpty ? sourceArgumentSyntaxes.Count : expandedElements[0];

                        items.Add(
                            (order,
                             CreatePackedArgument( name, expandedElements, expandedParams.Parameter.Type, callSite, semanticModel: operation.SemanticModel!, context ),
                             receivingParameter, null) );

                        break;
                    }

                    if ( !sourceArgumentIndices.TryGetValue( argument.ParameterOrdinal, out var sourceIndex ) || sourceIndex < 0 )
                    {
                        throw new ArgumentException(
                            $"The parameter {argument.ParameterOrdinal} of the source method has no argument written at the call site '{callSite}'.",
                            nameof(request) );
                    }

                    if ( !usedSourceArguments.Add( sourceIndex ) )
                    {
                        throw new ArgumentException( $"The argument {sourceIndex} of the call site '{callSite}' is passed more than once.", nameof(request) );
                    }

                    var sourceArgument = sourceArgumentSyntaxes[sourceIndex];

                    if ( !IsCompatibleRefKind( sourceArgument, receivingParameter ) )
                    {
                        throw new ArgumentException(
                            $"The argument '{sourceArgument}' cannot be passed to the parameter '{receivingParameter.Name}', whose passing mode is different.",
                            nameof(request) );
                    }

                    TypeSyntax? castType = null;

                    if ( argument.CastType != null )
                    {
                        if ( !sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None ) || receivingParameter.RefKind != Code.RefKind.None )
                        {
                            throw new ArgumentException(
                                $"The argument '{sourceArgument}' cannot be cast, because it is not passed by value to the parameter '{receivingParameter.Name}'.",
                                nameof(request) );
                        }

                        castType = context.SyntaxGenerator.TypeSyntax( argument.CastType ).WithSimplifierAnnotationIfNecessary( context );
                    }

                    items.Add(
                        (sourceIndex, new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceArgument, sourceIndex, null, name ) { CastType = castType },
                         receivingParameter, argument.CastType) );

                    break;

                default:
                    // Expressions are evaluated after all the values of the source call site.
                    items.Add( (int.MaxValue, new CallSiteArgumentPlanItem( RedirectedArgumentKind.Value, -1, argument.Expression, name ), receivingParameter, null) );

                    break;
            }
        }

        if ( request.ReceiverMode == CallSiteReceiverMode.Drop && hasReceiverValue && !receiverUsed )
        {
            throw new ArgumentException( $"The receiver of '{callSite}' is not passed.", nameof(request) );
        }

        // The order of the list is the order of evaluation of the source call site. The linker writes named arguments, so the order does not need
        // to match the order of the parameters.
        var orderedItems = items.OrderBy( x => x.Order ).ToImmutableArray();
        var precedingDiscards = new List<int>?[orderedItems.Length];
        var followingDiscards = new List<int>?[orderedItems.Length];
        var semanticModel = operation.SemanticModel!;

        // A value of the source call site that is not passed is still evaluated, in its source order, when it can have a side effect. It is evaluated
        // and discarded before the next argument of the new call, or after the previous one when the next one cannot hold it.
        for ( var i = 0; i < sourceArgumentSyntaxes.Count; i++ )
        {
            if ( usedSourceArguments.Contains( i ) )
            {
                continue;
            }

            var argument = sourceArgumentSyntaxes[i];

            if ( !argument.RefKindKeyword.IsKind( SyntaxKind.None ) )
            {
                throw new ArgumentException( $"The argument '{argument}' of the call site '{callSite}' is passed by reference, so it must be passed.", nameof(request) );
            }

            var value = GetArgumentValue( semanticModel, argument );

            if ( IsWithoutSideEffect( value ) )
            {
                continue;
            }

            if ( !CanBeDiscarded( semanticModel, argument, value, out var reason ) )
            {
                throw new ArgumentException(
                    $"The argument '{argument}' of the call site '{callSite}' is not passed, and it can have a side effect, but it cannot be evaluated into a discard: {reason}",
                    nameof(request) );
            }

            var nextIndex = 0;

            while ( nextIndex < orderedItems.Length && orderedItems[nextIndex].Order < i )
            {
                nextIndex++;
            }

            var previousIndex = nextIndex - 1;

            if ( nextIndex < orderedItems.Length && CanHoldPrecedingDiscard( orderedItems[nextIndex], sourceArgumentSyntaxes ) )
            {
                (precedingDiscards[nextIndex] ??= new List<int>()).Add( i );
            }
            else if ( previousIndex >= 0 && CanHoldFollowingDiscard( semanticModel, orderedItems[previousIndex], sourceArgumentSyntaxes, argument ) )
            {
                (followingDiscards[previousIndex] ??= new List<int>()).Add( i );
            }
            else
            {
                throw new ArgumentException(
                    $"The argument '{argument}' of the call site '{callSite}' is not passed, and it can have a side effect, but no argument of the new call can evaluate it in its source order. An adjacent argument must be passed by value.",
                    nameof(request) );
            }
        }

        if ( (precedingDiscards.Any( d => d != null ) || followingDiscards.Any( d => d != null ))
             && ((CSharpParseOptions) callSite.SyntaxTree.Options).LanguageVersion < LanguageVersion.CSharp9 )
        {
            throw new ArgumentException(
                $"An argument of the call site '{callSite}' that is not passed can have a side effect, and the switch expression that evaluates it requires C# 9 or later.",
                nameof(request) );
        }

        var valueVariableName = followingDiscards.Any( d => d != null ) ? GetValueVariableName( semanticModel, callSite ) : null;

        return orderedItems.Select(
                ( x, index ) => x.Item with
                {
                    PrecedingDiscards = precedingDiscards[index]?.ToImmutableArray() ?? default,
                    FollowingDiscards = followingDiscards[index]?.ToImmutableArray() ?? default,
                    ValueVariableName = followingDiscards[index] != null ? valueVariableName : null
                } )
            .ToImmutableArray();
    }

    /// <summary>
    /// Creates the argument that packs the elements of an expanded <c>params</c> argument into one collection, which is written as a named argument.
    /// </summary>
    /// <remarks>
    /// From C# 12, the collection is a collection expression, which creates the collection that the compiler creates for the expanded argument,
    /// also for a <c>params</c> collection of C# 13. Before C# 12, only <c>params</c> arrays exist, and the collection is an array creation. An
    /// empty array is written <c>Array.Empty&lt;T&gt;()</c> when the method exists, as the compiler does.
    /// </remarks>
    private static CallSiteArgumentPlanItem CreatePackedArgument(
        string name,
        ImmutableArray<int> elements,
        ITypeSymbol parameterType,
        InvocationExpressionSyntax callSite,
        SemanticModel semanticModel,
        SyntaxGenerationContext context )
    {
        var item = new CallSiteArgumentPlanItem( RedirectedArgumentKind.SourceArgument, -1, null, name ) { PackedElements = elements };

        if ( ((CSharpParseOptions) callSite.SyntaxTree.Options).LanguageVersion >= LanguageVersion.CSharp12 )
        {
            return item with { PackedEmptyValue = CollectionExpression() };
        }

        if ( parameterType.TypeKind != Microsoft.CodeAnalysis.TypeKind.Array )
        {
            throw new ArgumentException( $"The params collection of the call site '{callSite}' can be packed only with C# 12 or later." );
        }

        var elementType = context.SyntaxGenerator.TypeSyntax( ((IArrayTypeSymbol) parameterType).ElementType ).WithSimplifierAnnotationIfNecessary( context );

        var hasArrayEmpty = semanticModel.Compilation.GetSpecialType( Microsoft.CodeAnalysis.SpecialType.System_Array )
            .GetMembers( "Empty" )
            .Any( m => m.Kind == SymbolKind.Method && m.IsStatic && m.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public );

        ExpressionSyntax emptyValue = hasArrayEmpty
            ? InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    context.SyntaxGenerator.TypeSyntax( semanticModel.Compilation.GetSpecialType( Microsoft.CodeAnalysis.SpecialType.System_Array ) ),
                    GenericName( Identifier( "Empty" ), TypeArgumentList( SingletonSeparatedList( elementType ) ) ) ) )
            : ArrayCreationExpression(
                SyntaxFactoryEx.TokenWithTrailingSpace( SyntaxKind.NewKeyword ),
                ArrayType(
                    elementType,
                    SingletonList( ArrayRankSpecifier( SingletonSeparatedList<ExpressionSyntax>( LiteralExpression( SyntaxKind.NumericLiteralExpression, Literal( 0 ) ) ) ) ) ),
                null );

        return item with { PackedArrayElementType = elementType, PackedEmptyValue = emptyValue };
    }

    /// <summary>
    /// Determines whether a source argument that can have a side effect can be evaluated alone, as the governing expression of a switch expression,
    /// with the same effects as at the source call site.
    /// </summary>
    private static bool CanBeDiscarded( SemanticModel semanticModel, ArgumentSyntax argument, IOperation? value, out string reason )
    {
        var expression = argument.Expression;

        // The operation tree has no node for parentheses and for the suppression of a nullable warning, so the syntax of the value can be the
        // expression of the argument or an expression inside these nodes.
        var expressionForms = new List<SyntaxNode> { expression };

        while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression )
        {
            expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                ? ((ParenthesizedExpressionSyntax) expression).Expression
                : ((PostfixUnaryExpressionSyntax) expression).Operand;

            expressionForms.Add( expression );
        }

        // The implicit conversion to the type of the parameter is not evaluated when the value is discarded. A user-defined conversion runs user code
        // at the source call site, so it cannot be omitted.
        while ( value is IConversionOperation { IsImplicit: true } conversion )
        {
            if ( conversion.OperatorMethod != null )
            {
                reason = "its conversion to the type of the parameter calls a user-defined operator.";

                return false;
            }

            value = conversion.Operand;
        }

        if ( value == null || !expressionForms.Contains( value.Syntax ) || value is IInterpolatedStringHandlerCreationOperation
             || expression.Kind() is SyntaxKind.ImplicitObjectCreationExpression or SyntaxKind.CollectionExpression )
        {
            reason = "its value depends on the type of the parameter.";

            return false;
        }

        var type = semanticModel.GetTypeInfo( expression ).Type;

        if ( type == null || type.TypeKind is Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer || type.SpecialType == Microsoft.CodeAnalysis.SpecialType.System_Void )
        {
            reason = "it has no type, or it has a pointer type, which a switch expression cannot take.";

            return false;
        }

        reason = "";

        return true;
    }

    /// <summary>
    /// Determines whether an argument of the new call can be written <c>D switch { _ =&gt; value }</c>.
    /// </summary>
    private static bool CanHoldPrecedingDiscard(
        (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
        SeparatedSyntaxList<ArgumentSyntax> sourceArguments )
        => item.Parameter.RefKind == Code.RefKind.None
           && item.Item.Kind switch
           {
               RedirectedArgumentKind.SourceArgument when item.Item.IsPacked => true,
               RedirectedArgumentKind.SourceArgument => sourceArguments[item.Item.SourceArgumentIndex].RefKindKeyword.IsKind( SyntaxKind.None ),
               RedirectedArgumentKind.Value => true,
               _ => false
           };

    /// <summary>
    /// Determines whether an argument of the new call can be written <c>value switch { var t =&gt; D switch { _ =&gt; t } }</c>.
    /// </summary>
    /// <remarks>
    /// The pattern variable has the natural type of the value, so the natural type must convert implicitly to the type of the parameter. This
    /// excludes a value whose conversion depends on the expression, for instance a constant that a constant conversion narrows. The discarded
    /// argument becomes part of an arm, so it must not declare a variable, whose scope would end with the arm.
    /// </remarks>
    private static bool CanHoldFollowingDiscard(
        SemanticModel semanticModel,
        (int Order, CallSiteArgumentPlanItem Item, IParameter Parameter, IType? CastType) item,
        SeparatedSyntaxList<ArgumentSyntax> sourceArguments,
        ArgumentSyntax discardedArgument )
    {
        if ( item.Parameter.RefKind != Code.RefKind.None || item.Item.Kind != RedirectedArgumentKind.SourceArgument || item.Item.IsPacked )
        {
            return false;
        }

        var sourceArgument = sourceArguments[item.Item.SourceArgumentIndex];

        // The pattern variable of the holder takes the natural type of the value, and a target-typed value has none.
        if ( sourceArgument.Expression.Kind() is SyntaxKind.ImplicitObjectCreationExpression or SyntaxKind.CollectionExpression or SyntaxKind.DefaultLiteralExpression
             || semanticModel.GetOperation( sourceArgument.Expression ) is IInterpolatedStringHandlerCreationOperation )
        {
            return false;
        }

        if ( !sourceArgument.RefKindKeyword.IsKind( SyntaxKind.None )
             || discardedArgument.Expression.DescendantNodesAndSelf().Any( n => n.Kind() is SyntaxKind.DeclarationExpression or SyntaxKind.SingleVariableDesignation ) )
        {
            return false;
        }

        // The natural type of a cast argument is the type of the cast.
        var naturalType = item.CastType != null ? item.CastType.GetSymbol() : semanticModel.GetTypeInfo( sourceArgument.Expression ).Type;
        var parameterType = item.Parameter.Type.GetSymbol();

        return naturalType != null && parameterType != null && naturalType.TypeKind is not (Microsoft.CodeAnalysis.TypeKind.Pointer or Microsoft.CodeAnalysis.TypeKind.FunctionPointer)
               && semanticModel.Compilation.ClassifyConversion( naturalType, parameterType ).IsImplicit;
    }

    /// <summary>
    /// Returns the name of the pattern variable that holds the value of an argument while the discards that follow it are evaluated. The name is
    /// not the name of a symbol in scope.
    /// </summary>
    /// <remarks>
    /// The name contains the number of invocations that contain the call site. A call site nested in an argument of another one therefore gets
    /// another name, so the scope of its pattern variable, which is an arm of the switch expression of the outer call site, does not declare the
    /// name twice. Call sites with the same depth are in separate arms, which are separate scopes.
    /// </remarks>
    private static string GetValueVariableName( SemanticModel semanticModel, InvocationExpressionSyntax callSite )
    {
        var depth = callSite.Ancestors().Count( n => n.IsKind( SyntaxKind.InvocationExpression ) );
        var baseName = $"__value{depth}";

        for ( var i = 0;; i++ )
        {
            var name = i == 0 ? baseName : $"{baseName}_{i}";

            if ( semanticModel.LookupSymbols( callSite.SpanStart, name: name ).IsEmpty
                 && !callSite.DescendantTokens().Any( t => t.IsKind( SyntaxKind.IdentifierToken ) && t.ValueText == name ) )
            {
                return name;
            }
        }
    }

    private static bool IsCompatibleRefKind( ArgumentSyntax argument, IParameter parameter )
        => (argument.RefKindKeyword.Kind(), parameter.RefKind) switch
        {
            (SyntaxKind.None, Code.RefKind.None or Code.RefKind.In) => true,
            (SyntaxKind.InKeyword, Code.RefKind.In or Code.RefKind.RefReadOnly) => true,
            (SyntaxKind.RefKeyword, Code.RefKind.Ref or Code.RefKind.RefReadOnly) => true,
            (SyntaxKind.OutKeyword, Code.RefKind.Out) => true,
            _ => false
        };

    /// <summary>
    /// Returns the operation of the value that a source argument passes, including the implicit conversion to the type of the parameter.
    /// </summary>
    private static IOperation? GetArgumentValue( SemanticModel semanticModel, ArgumentSyntax argument )
    {
        if ( semanticModel.GetOperation( argument ) is IArgumentOperation argumentOperation )
        {
            return argumentOperation.Value;
        }

        // An element of an expanded params argument has no argument operation of its own, and neither has an argument whose expression is in
        // parentheses or suppresses a nullable warning, because the operation tree has no node for these expressions. The value is the operation
        // of the inner expression, wrapped in the implicit conversions to the type of the parameter or of the elements.
        var expression = argument.Expression;

        while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.SuppressNullableWarningExpression )
        {
            expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                ? ((ParenthesizedExpressionSyntax) expression).Expression
                : ((PostfixUnaryExpressionSyntax) expression).Operand;
        }

        var operation = semanticModel.GetOperation( expression );

        while ( operation?.Parent is IConversionOperation conversion && (conversion.Syntax == expression || conversion.Syntax == argument.Expression) )
        {
            operation = conversion;
        }

        return operation;
    }

    /// <summary>
    /// Determines whether the evaluation of a value can be omitted without changing the behavior of the program.
    /// </summary>
    /// <remarks>
    /// The method accepts constants, <c>default</c>, <c>typeof</c>, <c>this</c>, the values of locals and parameters, the fields of <c>this</c>,
    /// anonymous functions, and the delegates of static methods or of methods of such a value, through conversions that are not user-defined. It
    /// refuses static fields, because reading a static field can run the static constructor of its type. It refuses any other form, for instance a
    /// property, which runs a getter, and a field of another object, which can throw a <see cref="NullReferenceException"/>.
    /// </remarks>
    private static bool IsWithoutSideEffect( IOperation? operation )
        => operation switch
        {
            { ConstantValue.HasValue: true } => true,
            IDefaultValueOperation or ITypeOfOperation or IInstanceReferenceOperation or ILocalReferenceOperation or IParameterReferenceOperation => true,
            IFieldReferenceOperation { Field.IsStatic: false, Instance: IInstanceReferenceOperation } => true,
            IAnonymousFunctionOperation => true,
            IMethodReferenceOperation methodReference => methodReference.Instance == null || IsWithoutSideEffect( methodReference.Instance ),
            IDelegateCreationOperation delegateCreation => IsWithoutSideEffect( delegateCreation.Target ),
            IConversionOperation { IsImplicit: true, OperatorMethod: null } conversion
                => IsWithoutSideEffect( conversion.Operand ),
            _ => false
        };

    /// <summary>
    /// Determines whether a method can be dispatched virtually.
    /// </summary>
    private static bool IsVirtual( IMethodSymbol method ) => method.IsVirtual || method.IsOverride || method.IsAbstract;

    /// <summary>
    /// Returns the member binding or the element binding that starts the receiver chain of an invoked expression, or <c>null</c> when the
    /// invoked expression is not in the <c>WhenNotNull</c> part of a conditional access.
    /// </summary>
    /// <remarks>
    /// In <c>a?.M()</c>, the invoked expression is the binding <c>.M</c>. In <c>a?.B.M()</c> or <c>a?[0].M()</c>, it is a member access whose
    /// receiver chain starts with the binding <c>.B</c> or <c>[0]</c>. In both cases, the receiver of the call exists only in the conditional access.
    /// </remarks>
    private static ExpressionSyntax? GetConditionalAccessBinding( ExpressionSyntax invokedExpression )
    {
        var node = invokedExpression;

        while ( true )
        {
            switch ( node.Kind() )
            {
                case SyntaxKind.MemberBindingExpression or SyntaxKind.ElementBindingExpression:
                    return node;

                case SyntaxKind.SimpleMemberAccessExpression:
                    node = ((MemberAccessExpressionSyntax) node).Expression;

                    break;

                case SyntaxKind.InvocationExpression:
                    node = ((InvocationExpressionSyntax) node).Expression;

                    break;

                case SyntaxKind.ElementAccessExpression:
                    node = ((ElementAccessExpressionSyntax) node).Expression;

                    break;

                case SyntaxKind.SuppressNullableWarningExpression:
                    node = ((PostfixUnaryExpressionSyntax) node).Operand;

                    break;

                default:
                    return null;
            }
        }
    }

    private static bool IsInConditionalAccess( InvocationExpressionSyntax callSite )
    {
        SyntaxNode node = callSite;

        while ( node.Parent != null )
        {
            if ( node.Parent.IsKind( SyntaxKind.ConditionalAccessExpression ) && ((ConditionalAccessExpressionSyntax) node.Parent).WhenNotNull == node )
            {
                return true;
            }

            if ( node.Parent is not (MemberAccessExpressionSyntax or InvocationExpressionSyntax or ElementAccessExpressionSyntax or ConditionalAccessExpressionSyntax) )
            {
                return false;
            }

            node = node.Parent;
        }

        return false;
    }

    private static SimpleNameSyntax CreateMethodName( IMethod method, ImmutableArray<IType> typeArguments, SyntaxGenerationContext context )
        => typeArguments.IsDefaultOrEmpty
            ? SyntaxFactoryEx.SafeIdentifierName( method.Name )
            : GenericName( SyntaxFactoryEx.SafeIdentifier( method.Name ), TypeArgumentList( SeparatedList( typeArguments.Select( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );

    private static ExpressionSyntax CreateStaticCallee( CallSiteRedirectionTarget target, ImmutableArray<IType> typeArguments, SyntaxGenerationContext context )
    {
        var containingType = target.ContainingTypeAtCallSite ?? target.Method.DeclaringType;

        return MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                context.SyntaxGenerator.TypeExpression( containingType ),
                CreateMethodName( target.Method, typeArguments, context ) )
            .WithSimplifierAnnotationIfNecessary( context );
    }
}

/// <summary>
/// Compares syntax nodes by reference, because a redirection is keyed by the identity of its source node.
/// </summary>
internal sealed class SyntaxNodeReferenceComparer : IEqualityComparer<SyntaxNode>
{
    public static SyntaxNodeReferenceComparer Instance { get; } = new();

    private SyntaxNodeReferenceComparer() { }

    public bool Equals( SyntaxNode? x, SyntaxNode? y ) => ReferenceEquals( x, y );

    public int GetHashCode( SyntaxNode obj ) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode( obj );
}
