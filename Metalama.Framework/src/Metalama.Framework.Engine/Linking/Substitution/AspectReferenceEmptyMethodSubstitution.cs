// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.CodeModel.Helpers;
using Metalama.Framework.Engine.Services;
using Metalama.Framework.Engine.SyntaxGeneration;
#pragma warning disable IDE0005 // Using directive is unnecessary - needed by some but not all project configurations
using Metalama.Framework.Engine.Formatting;
#pragma warning restore IDE0005
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RoslynSpecialType = Microsoft.CodeAnalysis.SpecialType;

namespace Metalama.Framework.Engine.Linking.Substitution;

/// <summary>
/// Substitutes an aspect reference to an empty method (introduced method with no base implementation)
/// by replacing the invocation with a semantically appropriate empty expression (for expression context)
/// or removing the statement (for statement context), instead of generating a separate empty method.
/// </summary>
internal sealed class AspectReferenceEmptyMethodSubstitution : SyntaxNodeSubstitution
{
    private readonly bool _isStatementContext;
    private readonly bool _isAwaitContext;
    private readonly IMethodSymbol _methodSymbol;

    public override SyntaxNode ReplacedNode { get; }

    public AspectReferenceEmptyMethodSubstitution(
        CompilationContext compilationContext,
        ResolvedAspectReference aspectReference ) : base( compilationContext )
    {
        // Support only base semantics for introduced methods with no base.
        Invariant.Assert( aspectReference.ResolvedSemantic.Kind == IntermediateSymbolSemanticKind.Base );

        Invariant.Assert(
            !aspectReference.ResolvedSemantic.Symbol.IsOverride
            && !aspectReference.ResolvedSemantic.Symbol.TryGetHiddenSymbol( compilationContext.Compilation, out _ ) );

        this._methodSymbol = (IMethodSymbol) aspectReference.ResolvedSemantic.Symbol;

        // Determine the invocation expression (parent of the root node).
        var invocationNode = FindInvocationExpression( aspectReference.RootNode );

        // Check if the invocation is wrapped in an await expression.
        var effectiveNode = invocationNode;

        if ( invocationNode.Parent.IsKind( SyntaxKind.AwaitExpression ) && invocationNode.Parent is AwaitExpressionSyntax awaitExpression )
        {
            this._isAwaitContext = true;
            effectiveNode = awaitExpression;
        }

        // Determine whether the effective node is used as a statement or as an expression.
        if ( effectiveNode.Parent.IsKind( SyntaxKind.ExpressionStatement ) && effectiveNode.Parent is ExpressionStatementSyntax expressionStatement )
        {
            this._isStatementContext = true;
            this.ReplacedNode = expressionStatement;
        }
        else
        {
            this._isStatementContext = false;
            this.ReplacedNode = effectiveNode;
        }
    }

    private static SyntaxNode FindInvocationExpression( SyntaxNode rootNode )
    {
        // The RootNode is typically the MemberAccessExpressionSyntax (e.g., this.Foo).
        // Its parent should be the InvocationExpressionSyntax (e.g., this.Foo()).
        // For the Link(This.Foo, Base)() pattern in linker tests, the RootNode is the inner
        // InvocationExpressionSyntax, and the outer InvocationExpressionSyntax is the parent.

        var current = rootNode;

        // Walk up to find the invocation expression that calls the referenced method.
        if ( current.Parent != null )
        {
            if ( current.Parent.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent is InvocationExpressionSyntax invocation
                 && invocation.Expression == current )
            {
                return invocation;
            }

            // For cases where rootNode itself is an invocation (e.g., Link(This.Foo, Base)),
            // and the parent is the outer invocation: Link(This.Foo, Base)().
            if ( current.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent.IsKind( SyntaxKind.InvocationExpression )
                 && current.Parent is InvocationExpressionSyntax outerInvocation
                 && outerInvocation.Expression == current )
            {
                return outerInvocation;
            }
        }

        // Fallback: if rootNode itself is an invocation.
        if ( rootNode.IsKind( SyntaxKind.InvocationExpression ) )
        {
            return rootNode;
        }

        throw new AssertionFailedException( $"Could not find invocation expression for root node: {rootNode}" );
    }

    public override SyntaxNode? Substitute( SyntaxNode currentNode, SubstitutionContext substitutionContext )
    {
        if ( this._isStatementContext )
        {
            // For statement context (void or non-void used as statement), remove the statement entirely.
            return null;
        }

        return this.CreateEmptyExpression( substitutionContext.SyntaxGenerationContext.SyntaxGenerator )
            .WithSimplifierAnnotation();
    }

    private ExpressionSyntax CreateEmptyExpression( ContextualSyntaxGenerator syntaxGenerator )
    {
        var returnType = this._methodSymbol.ReturnType;

        // When the invocation is wrapped in `await`, we produce the awaited result type's default expression.
        if ( this._isAwaitContext
             && AsyncHelper.TryGetAsyncInfo( returnType, out var awaitedResultType, out var awaitedHasMethodBuilder )
             && awaitedHasMethodBuilder )
        {
            if ( awaitedResultType.SpecialType == RoslynSpecialType.System_Void )
            {
                // await Task → should have been removed as a statement; this branch should not be reached.
                throw new AssertionFailedException( "await Task in expression context is not supported." );
            }

            // await Task<T> → default(T)
            return DefaultExpression( syntaxGenerator.TypeSyntax( awaitedResultType ) );
        }

        // Check for async types (Task, Task<T>) without await.
        if ( AsyncHelper.TryGetAsyncInfo( returnType, out var asyncResultType, out var hasMethodBuilder ) && hasMethodBuilder )
        {
            var taskTypeSyntax = CreateFullyQualifiedName( "System", "Threading", "Tasks", "Task" );

            if ( asyncResultType.SpecialType == RoslynSpecialType.System_Void )
            {
                // Task → Task.CompletedTask
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    taskTypeSyntax,
                    SyntaxFactoryEx.WellKnownIdentifierName( "CompletedTask" ) );
            }
            else
            {
                // Task<T> → Task.FromResult(default(T))
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        taskTypeSyntax,
                        GenericName( SyntaxFactoryEx.WellKnownIdentifier( "FromResult" ) )
                            .WithTypeArgumentList(
                                TypeArgumentList( SingletonSeparatedList( syntaxGenerator.TypeSyntax( asyncResultType ) ) ) ) ),
                    ArgumentList( SingletonSeparatedList( Argument( DefaultExpression( syntaxGenerator.TypeSyntax( asyncResultType ) ) ) ) ) );
            }
        }

        // Check for enumerable/enumerator types.
        var enumerableKind = this._methodSymbol.GetEnumerableKind();

        switch ( enumerableKind )
        {
            case EnumerableKind.IEnumerable:
                // IEnumerable<T> → Enumerable.Empty<T>()
                return CreateEnumerableEmptyExpression( syntaxGenerator, returnType );

            case EnumerableKind.IEnumerator:
                // IEnumerator<T> → Enumerable.Empty<T>().GetEnumerator()
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        CreateEnumerableEmptyExpression( syntaxGenerator, this.GetEnumerableTypeFromEnumerator( returnType ) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "GetEnumerator" ) ) );

            case EnumerableKind.IAsyncEnumerable:
                // IAsyncEnumerable<T> → AsyncEnumerableArray<T>.Empty
                return CreateAsyncEnumerableEmptyExpression( syntaxGenerator, returnType );

            case EnumerableKind.IAsyncEnumerator:
                // IAsyncEnumerator<T> → AsyncEnumerableArray<T>.Empty.GetAsyncEnumerator()
                return InvocationExpression(
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        CreateAsyncEnumerableEmptyExpression( syntaxGenerator, this.GetAsyncEnumerableTypeFromEnumerator( returnType ) ),
                        SyntaxFactoryEx.WellKnownIdentifierName( "GetAsyncEnumerator" ) ) );

            case EnumerableKind.UntypedIEnumerable:
            case EnumerableKind.UntypedIEnumerator:
            default:
                break;
        }

        // Default case: use default(ReturnType).
        return DefaultExpression( syntaxGenerator.TypeSyntax( returnType ) );
    }

    /// <summary>
    /// Creates an expression for <c>Enumerable.Empty&lt;T&gt;()</c>.
    /// </summary>
    private static ExpressionSyntax CreateEnumerableEmptyExpression( ContextualSyntaxGenerator syntaxGenerator, ITypeSymbol enumerableType )
    {
        var typeArg = GetTypeArgument( enumerableType );

        return InvocationExpression(
            MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateFullyQualifiedName( "System", "Linq", "Enumerable" ),
                GenericName( SyntaxFactoryEx.WellKnownIdentifier( "Empty" ) )
                    .WithTypeArgumentList(
                        TypeArgumentList( SingletonSeparatedList( syntaxGenerator.TypeSyntax( typeArg ) ) ) ) ) );
    }

    /// <summary>
    /// Creates an expression for <c>AsyncEnumerableArray&lt;T&gt;.Empty</c>.
    /// </summary>
    /// <remarks>
    /// We use <c>Metalama.Framework.RunTime.AsyncEnumerableArray&lt;T&gt;</c> instead of
    /// <c>System.Linq.AsyncEnumerable.Empty&lt;T&gt;()</c> to avoid pulling in the
    /// <c>System.Linq.AsyncEnumerable</c> package as a dependency of user projects.
    /// </remarks>
    private static ExpressionSyntax CreateAsyncEnumerableEmptyExpression( ContextualSyntaxGenerator syntaxGenerator, ITypeSymbol asyncEnumerableType )
    {
        var typeArg = GetTypeArgument( asyncEnumerableType );

        // Build: global::Metalama.Framework.RunTime.AsyncEnumerableArray<T>.Empty
        var qualifiedName = CreateFullyQualifiedName( "Metalama", "Framework", "RunTime" );

        var genericTypeName = GenericName( SyntaxFactoryEx.WellKnownIdentifier( "AsyncEnumerableArray" ) )
            .WithTypeArgumentList(
                TypeArgumentList( SingletonSeparatedList( syntaxGenerator.TypeSyntax( typeArg ) ) ) );

        var fullType = MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            qualifiedName,
            genericTypeName );

        return MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            fullType,
            SyntaxFactoryEx.WellKnownIdentifierName( "Empty" ) );
    }

    /// <summary>
    /// Gets the type argument from a generic type (e.g., <c>int</c> from <c>IEnumerable&lt;int&gt;</c>).
    /// </summary>
    private static ITypeSymbol GetTypeArgument( ITypeSymbol type )
        => type.Kind == SymbolKind.NamedType && type is INamedTypeSymbol { TypeArguments.Length: > 0 } namedType
            ? namedType.TypeArguments[0]
            : throw new AssertionFailedException( $"Expected a generic type with at least one type argument, got: {type}" );

    /// <summary>
    /// Constructs the IEnumerable&lt;T&gt; type from IEnumerator&lt;T&gt; by looking up IEnumerable&lt;T&gt; in the compilation.
    /// </summary>
    private ITypeSymbol GetEnumerableTypeFromEnumerator( ITypeSymbol enumeratorType )
    {
        var typeArg = GetTypeArgument( enumeratorType );

        var enumerableType = this.CompilationContext.Compilation.GetSpecialType( RoslynSpecialType.System_Collections_Generic_IEnumerable_T );

        return enumerableType.Construct( typeArg );
    }

    /// <summary>
    /// Constructs the IAsyncEnumerable&lt;T&gt; type from IAsyncEnumerator&lt;T&gt;.
    /// </summary>
    private ITypeSymbol GetAsyncEnumerableTypeFromEnumerator( ITypeSymbol asyncEnumeratorType )
    {
        var typeArg = GetTypeArgument( asyncEnumeratorType );
        var asyncEnumerableType = this.CompilationContext.Compilation.GetTypeByMetadataName( "System.Collections.Generic.IAsyncEnumerable`1" );

        if ( asyncEnumerableType == null )
        {
            throw new AssertionFailedException( "Could not find IAsyncEnumerable<T> in the compilation." );
        }

        return asyncEnumerableType.Construct( typeArg );
    }

    /// <summary>
    /// Creates a fully qualified name expression like <c>global::System.Linq.Enumerable</c>.
    /// </summary>
    private static ExpressionSyntax CreateFullyQualifiedName( params string[] parts )
    {
        ExpressionSyntax result = AliasQualifiedName(
            SyntaxFactoryEx.WellKnownIdentifierName( Token( SyntaxKind.GlobalKeyword ) ),
            SyntaxFactoryEx.WellKnownIdentifierName( parts[0] ) );

        for ( var i = 1; i < parts.Length; i++ )
        {
            result = MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                result,
                SyntaxFactoryEx.WellKnownIdentifierName( parts[i] ) );
        }

        return result;
    }
}
