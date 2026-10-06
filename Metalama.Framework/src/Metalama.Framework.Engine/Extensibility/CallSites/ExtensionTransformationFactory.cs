// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Engine.AspectOrdering;
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using MethodKind = Microsoft.CodeAnalysis.MethodKind;

namespace Metalama.Framework.Engine.Extensibility.CallSites;

/// <summary>
/// Creates linker transformations on behalf of a <see cref="PipelineExtension"/>: it redirects source call sites and method references to other
/// methods.
/// </summary>
/// <remarks>
/// <para>
/// The engine creates one instance per pipeline execution and passes it to the transforming hook of every extension through
/// <see cref="ExtensionTransformationContext.TransformationFactory"/>. The instance validates every request and throws an
/// <see cref="ArgumentException"/> for a request that it cannot honor.
/// </para>
/// <para>
/// The factory does not change the code model. The rewritten call sites are not visible to aspects and do not appear in design-time generated code.
/// </para>
/// </remarks>
[PublicAPI]
public sealed partial class ExtensionTransformationFactory
{
    /// <summary>
    /// The lock that protects <see cref="_redirections"/>, <see cref="_forwarders"/>, <see cref="_nextRedirectionId"/> and <see cref="_isCompleted"/>.
    /// </summary>
    private readonly object _sync = new();

    /// <summary>
    /// The compilation that results from all aspects that execute before any low-level aspect weaver.
    /// </summary>
    private readonly CompilationModel _compilation;

    /// <summary>
    /// The ordered aspect layers of the pipeline, against which the aspect layer of an origin is validated.
    /// </summary>
    private readonly IReadOnlyList<OrderedAspectLayer> _aspectLayers;

    /// <summary>
    /// The options used to generate the syntax of the new call sites.
    /// </summary>
    private readonly SyntaxGenerationOptions _syntaxGenerationOptions;

    /// <summary>
    /// The requested redirections, indexed by syntax tree and then by source node.
    /// </summary>
    private readonly Dictionary<SyntaxTree, Dictionary<SyntaxNode, CallSiteRedirection>> _redirections = new();

    /// <summary>
    /// The identifier that is assigned to the next redirection.
    /// </summary>
    private int _nextRedirectionId;

    /// <summary>
    /// Indicates whether <see cref="Complete"/> has been called, after which no request is accepted.
    /// </summary>
    private bool _isCompleted;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExtensionTransformationFactory"/> class.
    /// </summary>
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
    /// Gets the compilation that results from all aspects that execute before any low-level aspect weaver. Its syntax trees are those of the
    /// source compilation.
    /// </summary>
    public ICompilation Compilation => this._compilation;

    /// <summary>
    /// Requests that a source invocation be replaced by an invocation of another method.
    /// </summary>
    /// <param name="origin">The aspect or fabric that requested the redirection.</param>
    /// <param name="request">The request.</param>
    /// <exception cref="ArgumentNullException"><paramref name="origin"/> or <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The request is not valid.</exception>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same call site, or the factory was completed.</exception>
    public void RedirectInvocation( ExtensionContributionOrigin origin, InvocationRedirectionRequest request )
    {
        _ = origin ?? throw new ArgumentNullException( nameof(origin) );
        _ = request ?? throw new ArgumentNullException( nameof(request) );

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
        var usesForwarder = false;

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
                    // The receiver exists only inside the conditional access, so it is passed as the receiver of a forwarder.
                    if ( request.ReceiverMode != CallSiteReceiverMode.FirstArgument )
                    {
                        throw new ArgumentException(
                            $"The receiver mode {request.ReceiverMode} cannot be used in the conditional access '{callSite}', because the receiver exists only inside the conditional access and cannot be passed by reference.",
                            nameof(request) );
                    }

                    usesForwarder = true;
                }

                if ( isBaseCall && IsVirtual( sourceMethod ) )
                {
                    throw new ArgumentException( $"The base call '{callSite}' to a virtual method cannot pass its receiver.", nameof(request) );
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

            argumentPlan = CreateArgumentPlan( request, operation, isReducedExtensionCall, hasReceiverValue, this._compilation, context );
        }

        var extraArguments = request.ExtraArguments.IsDefaultOrEmpty
            ? ImmutableArray<ArgumentSyntax>.Empty
            : request.ExtraArguments.SelectAsImmutableArray(
                a => Argument( NameColon( SyntaxFactoryEx.SafeIdentifierName( a.ParameterName ) ), default, a.Value ).WithAdditionalAnnotations( annotation ) );

        var resultCast = request.ResultCast == null ? null : context.SyntaxGenerator.TypeSyntax( request.ResultCast );

        CallSiteRedirection CreateRedirection( int id, ExpressionSyntax callee )
            => new(
                id,
                callSite,
                CallSiteRedirectionKind.Invocation,
                request.ReceiverMode,
                usesForwarder,
                callee.WithAdditionalAnnotations( annotation ),
                argumentPlan,
                extraArguments,
                resultCast,
                request.Description ?? $"the call '{callSite}' redirected to '{targetMethod}' by {origin.DiagnosticSourceDescription}" );

        ExpressionSyntax callee;
        ExpressionSyntax rewrittenCall;

        if ( usesForwarder )
        {
            // The arguments of the forwarder call do not depend on its name, which is computed from the binding of the static form of the call.
            var argumentList = ((InvocationExpressionSyntax) CreateRedirection( -1, IdentifierName( "_" ) ).Rewrite( callSite )).ArgumentList;
            callee = this.GetForwarderCallee( request, operation, semanticModel, argumentList, context );
            rewrittenCall = CreateRedirection( -1, callee ).Rewrite( callSite );
        }
        else
        {
            callee = CreateStaticCallee( request.Target, request.TypeArguments, context );
            rewrittenCall = CreateRedirection( -1, callee ).Rewrite( callSite );

            // When the rewritten call binds to another overload, the default values of the optional parameters that receive no argument are
            // written by name, which selects the target method when the overload has fewer parameters.
            if ( GetBindingError( semanticModel, operation, rewrittenCall, targetMethod ) != null
                 && GetOmittedDefaultArguments( rewrittenCall, targetMethod, context ) is { IsEmpty: false } defaultArguments )
            {
                extraArguments = extraArguments.AddRange( defaultArguments.Select( a => a.WithAdditionalAnnotations( annotation ) ) );
                rewrittenCall = CreateRedirection( -1, callee ).Rewrite( callSite );
            }

            if ( GetBindingError( semanticModel, operation, rewrittenCall, targetMethod ) is { } bindingError )
            {
                throw new ArgumentException( bindingError, nameof(request) );
            }
        }

        if ( request.ReceiverMode == CallSiteReceiverMode.FirstArgumentByRef )
        {
            VerifyReferenceArguments( semanticModel, callSite, rewrittenCall, CancellationToken.None );
        }

        this.AddRedirection( callSite, id => CreateRedirection( id, callee ) );
    }

    /// <summary>
    /// Returns <c>null</c> when the rewritten call binds to the target method at the position of the call site, and otherwise the message that
    /// describes the method to which it binds.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The rewritten call is bound speculatively, exactly as the linker writes it, without the redirections nested in its arguments. Overload
    /// resolution can select another method than the target, for instance an overload with fewer optional parameters, or an instance method of the
    /// receiver when the target is an extension method. The linker would then emit a call to that method without notice.
    /// </para>
    /// <para>
    /// A call in a conditional access is verified by <see cref="GetForwarderCallee"/> instead, because it calls a forwarder.
    /// </para>
    /// </remarks>
    private static string? GetBindingError(
        SemanticModel semanticModel,
        IInvocationOperation operation,
        ExpressionSyntax rewrittenCall,
        IMethod targetMethod )
    {
        if ( targetMethod.GetSymbol() is not { } targetSymbol )
        {
            // An introduced method has no symbol in the compilation of the call site.
            return null;
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
            return null;
        }

        var invocation = (InvocationExpressionSyntax) expression;

        var symbolInfo = semanticModel.GetSpeculativeSymbolInfo( callSite.SpanStart, invocation, SpeculativeBindingOption.BindAsExpression );

        if ( symbolInfo.Symbol is { Kind: SymbolKind.Method } and IMethodSymbol boundMethod
             && SymbolEqualityComparer.Default.Equals( (boundMethod.ReducedFrom ?? boundMethod).OriginalDefinition, targetSymbol.OriginalDefinition ) )
        {
            return null;
        }

        // The message is also embedded in the diagnostics of the clients of the factory, so it does not repeat the call site.
        return symbolInfo.Symbol != null
            ? $"The rewritten call binds to '{symbolInfo.Symbol.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' instead of '{targetMethod}'."
            : $"The rewritten call does not bind to '{targetMethod}' ({symbolInfo.CandidateReason}).";
    }

    /// <summary>
    /// Returns a named argument for each optional parameter of the target method that receives no argument in the rewritten call and that has a
    /// default value that can be written.
    /// </summary>
    /// <remarks>
    /// A positional argument binds to the parameter at its position, and a named argument to the parameter of its name. A <c>params</c> parameter
    /// is never given a default value, because it receives an empty collection.
    /// </remarks>
    private static ImmutableArray<ArgumentSyntax> GetOmittedDefaultArguments( ExpressionSyntax rewrittenCall, IMethod targetMethod, SyntaxGenerationContext context )
    {
        var expression = rewrittenCall;

        while ( expression.Kind() is SyntaxKind.ParenthesizedExpression or SyntaxKind.CastExpression )
        {
            expression = expression.Kind() == SyntaxKind.ParenthesizedExpression
                ? ((ParenthesizedExpressionSyntax) expression).Expression
                : ((CastExpressionSyntax) expression).Expression;
        }

        if ( !expression.IsKind( SyntaxKind.InvocationExpression ) )
        {
            return ImmutableArray<ArgumentSyntax>.Empty;
        }

        var parameters = targetMethod.Parameters;
        var covered = new bool[parameters.Count];
        var position = 0;

        foreach ( var argument in ((InvocationExpressionSyntax) expression).ArgumentList.Arguments )
        {
            if ( argument.NameColon != null )
            {
                var name = argument.NameColon.Name.Identifier.ValueText;

                for ( var i = 0; i < parameters.Count; i++ )
                {
                    if ( parameters[i].Name == name )
                    {
                        covered[i] = true;
                    }
                }
            }
            else if ( position < parameters.Count )
            {
                covered[position] = true;

                if ( !parameters[position].IsParams )
                {
                    position++;
                }
            }
        }

        var arguments = ImmutableArray.CreateBuilder<ArgumentSyntax>();

        for ( var i = 0; i < parameters.Count; i++ )
        {
            var parameter = parameters[i];

            if ( !covered[i] && !parameter.IsParams && parameter.DefaultValue != null )
            {
                arguments.Add(
                    Argument(
                        NameColon( SyntaxFactoryEx.SafeIdentifierName( parameter.Name ) ),
                        default,
                        context.SyntaxGenerator.TypedConstant( parameter.DefaultValue.Value ) ) );
            }
        }

        return arguments.ToImmutable();
    }

    /// <summary>
    /// Returns <c>null</c> when the method group of the target, written in place of the source method group, converts to the same delegate or
    /// function pointer type and binds to the target method, and otherwise the message that describes the difference.
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
    private static string? GetMethodReferenceBindingError(
        SemanticModel semanticModel,
        ExpressionSyntax node,
        IMethodReferenceOperation operation,
        ExpressionSyntax callee,
        IMethod targetMethod )
    {
        if ( targetMethod.GetSymbol() is not { } targetSymbol )
        {
            return null;
        }

        if ( !TryBindInContext( semanticModel, node, callee, out var speculativeModel, out var newNode ) )
        {
            return null;
        }

        // The rewritten expression is a method group, or an explicit delegate creation whose argument is the method group.
        var newOperation = speculativeModel.GetOperation( newNode );

        var newMethodReference = newOperation switch
        {
            IMethodReferenceOperation methodReference => methodReference,
            IDelegateCreationOperation { Target: IMethodReferenceOperation methodReference } => methodReference,
            _ => null
        };

        var convertedType = newOperation is IDelegateCreationOperation delegateCreation ? delegateCreation.Type : newMethodReference?.Parent?.Type;

        if ( newMethodReference is { Method: { } boundMethod }
             && SymbolEqualityComparer.Default.Equals( boundMethod.OriginalDefinition, targetSymbol.OriginalDefinition )
             && SymbolEqualityComparer.Default.Equals( convertedType, operation.Parent?.Type ) )
        {
            return null;
        }

        // The message is also embedded in the diagnostics of the clients of the factory, so it does not repeat the method group.
        return newMethodReference is { Method: { } otherMethod }
               && !SymbolEqualityComparer.Default.Equals( otherMethod.OriginalDefinition, targetSymbol.OriginalDefinition )
            ? $"The rewritten method group binds to '{otherMethod.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' instead of '{targetMethod}'."
            : $"The rewritten method group does not convert to '{operation.Parent?.Type?.ToDisplayString( SymbolDisplayFormat.CSharpShortErrorMessageFormat )}' with '{targetMethod}'.";
    }

    /// <summary>
    /// Verifies that every argument that the rewritten call passes by reference is a writable variable, and throws an
    /// <see cref="ArgumentException"/> otherwise.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The receiver of <see cref="CallSiteReceiverMode.FirstArgumentByRef"/> is passed with the <c>ref</c> modifier, which the C# compiler accepts
    /// only for a writable variable. A readonly field outside of a constructor, an <c>in</c> parameter, the <c>this</c> of a readonly member or of a
    /// class, and a value are not writable variables.
    /// </para>
    /// <para>
    /// A speculative semantic model reports no diagnostic, and the operation of such an argument is valid, so the rewritten call is bound in a copy
    /// of its syntax tree, and the errors located on the arguments passed by reference are read. Only the member that contains the call is bound.
    /// </para>
    /// </remarks>
    private static void VerifyReferenceArguments(
        SemanticModel semanticModel,
        InvocationExpressionSyntax callSite,
        ExpressionSyntax rewrittenCall,
        CancellationToken cancellationToken )
    {
        var marker = new SyntaxAnnotation();
        var syntaxTree = callSite.SyntaxTree;
        var newRoot = syntaxTree.GetRoot( cancellationToken ).ReplaceNode( callSite, rewrittenCall.WithAdditionalAnnotations( marker ) );
        var newTree = syntaxTree.WithRootAndOptions( newRoot, syntaxTree.Options );
        var newCompilation = semanticModel.Compilation.ReplaceSyntaxTree( syntaxTree, newTree );
        var newCall = newTree.GetRoot( cancellationToken ).GetAnnotatedNodes( marker ).Single();

        var invocation = newCall.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();

        if ( invocation == null )
        {
            return;
        }

        var referenceArguments = invocation.ArgumentList.Arguments.Where( a => a.RefKindKeyword.IsKind( SyntaxKind.RefKeyword ) ).ToList();

        if ( referenceArguments.Count == 0 )
        {
            return;
        }

        var errors = newCompilation.GetSemanticModel( newTree ).GetDiagnostics( invocation.Span, cancellationToken )
            .Where( d => d.Severity == DiagnosticSeverity.Error )
            .ToList();

        foreach ( var argument in referenceArguments )
        {
            if ( errors.Any( e => argument.Span.Contains( e.Location.SourceSpan ) ) )
            {
                throw new ArgumentException( $"The argument '{argument.Expression}' is passed by reference, but it is not a writable variable.", "request" );
            }
        }
    }

    /// <summary>
    /// Replaces a node by another one in its enclosing statement, initializer or expression body, and binds the result with a speculative semantic
    /// model.
    /// </summary>
    /// <param name="semanticModel">The semantic model of the syntax tree of <paramref name="node"/>.</param>
    /// <param name="node">The node to replace.</param>
    /// <param name="replacement">The replacing node.</param>
    /// <param name="speculativeModel">The speculative semantic model.</param>
    /// <param name="newNode">The replacing node in the tree of <paramref name="speculativeModel"/>.</param>
    /// <returns><c>false</c> when <paramref name="node"/> is in a context that has no speculative semantic model, for instance an attribute
    /// argument.</returns>
    private static bool TryBindInContext(
        SemanticModel semanticModel,
        SyntaxNode node,
        SyntaxNode replacement,
        [NotNullWhen( true )] out SemanticModel? speculativeModel,
        [NotNullWhen( true )] out SyntaxNode? newNode )
    {
        var marker = new SyntaxAnnotation();
        replacement = replacement.WithAdditionalAnnotations( marker );
        SyntaxNode speculativeRoot;
        bool success;

        if ( node.FirstAncestorOrSelf<StatementSyntax>() is { } statement )
        {
            var newStatement = statement.ReplaceNode( node, replacement );
            speculativeRoot = newStatement;
            success = semanticModel.TryGetSpeculativeSemanticModel( statement.SpanStart, newStatement, out speculativeModel );
        }
        else if ( node.FirstAncestorOrSelf<EqualsValueClauseSyntax>() is { } initializer )
        {
            var newInitializer = initializer.ReplaceNode( node, replacement );
            speculativeRoot = newInitializer;
            success = semanticModel.TryGetSpeculativeSemanticModel( initializer.SpanStart, newInitializer, out speculativeModel );
        }
        else if ( node.FirstAncestorOrSelf<ArrowExpressionClauseSyntax>() is { } arrow )
        {
            var newArrow = arrow.ReplaceNode( node, replacement );
            speculativeRoot = newArrow;
            success = semanticModel.TryGetSpeculativeSemanticModel( arrow.SpanStart, newArrow, out speculativeModel );
        }
        else
        {
            speculativeModel = null;
            newNode = null;

            return false;
        }

        if ( !success || speculativeModel == null )
        {
            newNode = null;

            return false;
        }

        newNode = speculativeRoot.GetAnnotatedNodes( marker ).Single();

        return true;
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
    /// <exception cref="ArgumentNullException"><paramref name="origin"/> or <paramref name="request"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">The request is not valid.</exception>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the same method group, or the factory was completed.</exception>
    public void RedirectMethodReference( ExtensionContributionOrigin origin, MethodReferenceRedirectionRequest request )
    {
        _ = origin ?? throw new ArgumentNullException( nameof(origin) );
        _ = request ?? throw new ArgumentNullException( nameof(request) );

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

        // A method group of an overloaded target can lose the natural type of the source method group (var d = M;). The delegate creation is
        // then written explicitly with the delegate type of the source, which selects the target and keeps the type of the expression.
        if ( GetMethodReferenceBindingError( semanticModel, node, operation, callee, request.Target.Method ) != null
             && operation.Parent is IDelegateCreationOperation { Type: { } delegateType } )
        {
            callee = ObjectCreationExpression(
                    context.SyntaxGenerator.TypeSyntax( delegateType ),
                    ArgumentList( SingletonSeparatedList( Argument( callee ) ) ),
                    null )
                .WithAdditionalAnnotations( annotation );
        }

        if ( GetMethodReferenceBindingError( semanticModel, node, operation, callee, request.Target.Method ) is { } bindingError )
        {
            throw new ArgumentException( bindingError, nameof(request) );
        }

        this.AddRedirection(
            node,
            id => new CallSiteRedirection(
                id,
                node,
                CallSiteRedirectionKind.MethodReference,
                request.ReceiverMode,
                false,
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
    /// Freezes the factory and returns the linker input. Called by the pipeline after all extensions.
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
                    x => (IReadOnlyDictionary<SyntaxNode, CallSiteRedirection>) x.Value ),
                this.CreateForwarderCompilationUnit() );
        }
    }

    /// <summary>
    /// Records the redirection of a node and assigns it the next sequential identifier.
    /// </summary>
    /// <param name="node">The source node to redirect.</param>
    /// <param name="createRedirection">A delegate that creates the redirection from its identifier.</param>
    /// <exception cref="InvalidOperationException">A redirection was already requested for the node, or the factory was completed.</exception>
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

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when <see cref="Complete"/> has been called. The caller must hold <see cref="_sync"/>.
    /// </summary>
    private void ThrowIfCompleted()
    {
        if ( this._isCompleted )
        {
            throw new InvalidOperationException( "The factory of transformations can no longer be used, because the transforming hook has completed." );
        }
    }

    /// <summary>
    /// Returns the semantic model of the syntax tree of a node, after checking that the factory is not completed and that the syntax tree belongs
    /// to the compilation of the pipeline.
    /// </summary>
    /// <exception cref="ArgumentException">The node does not belong to a syntax tree of the compilation of the pipeline.</exception>
    /// <exception cref="InvalidOperationException">The factory was completed.</exception>
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
            throw new ArgumentException( $"The node '{node}' does not belong to a syntax tree of the compilation of the pipeline.", "request" );
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

        // A layer that was created from the name of the aspect type only, which happens in tests, has no aspect class.
        return (origin.AspectInstance?.AspectClass ?? layer.AspectClassIfAny)?.GeneratedCodeAnnotation
               ?? throw new ArgumentException( $"The aspect layer '{origin.AspectLayerId}' of the origin has no aspect class.", nameof(origin) );
    }

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

    /// <summary>
    /// Determines whether a call site is in the <c>WhenNotNull</c> part of a conditional access, either directly or through a chain of member
    /// accesses, invocations and element accesses.
    /// </summary>
    /// <remarks>
    /// A cast of the result of such a call site would apply to a part of the conditional access instead of to its whole value.
    /// </remarks>
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

    /// <summary>
    /// Creates the simple name of a method, with a type argument list when <paramref name="typeArguments"/> is not empty.
    /// </summary>
    private static SimpleNameSyntax CreateMethodName( IMethod method, ImmutableArray<IType> typeArguments, SyntaxGenerationContext context )
        => typeArguments.IsDefaultOrEmpty
            ? SyntaxFactoryEx.SafeIdentifierName( method.Name )
            : GenericName( SyntaxFactoryEx.SafeIdentifier( method.Name ), TypeArgumentList( SeparatedList( typeArguments.Select( t => context.SyntaxGenerator.TypeSyntax( t ) ) ) ) );

    /// <summary>
    /// Creates the member access that designates a static target method, qualified by <see cref="CallSiteRedirectionTarget.ContainingTypeAtCallSite"/>
    /// or else by the declaring type of the method.
    /// </summary>
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
