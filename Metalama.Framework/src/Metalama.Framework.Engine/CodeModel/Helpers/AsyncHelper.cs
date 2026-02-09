// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Engine.SyntaxGeneration;
using Metalama.Framework.Engine.Utilities.Caching;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Metalama.Framework.Engine.CodeModel.Helpers
{
    internal static class AsyncHelper
    {
        public static bool IsAsyncSafe( this IMethodSymbol method )
            => method.MetadataToken == 0
                ? method.IsAsync
                : method.GetAttributes()
                    .Any(
                        a => a.AttributeConstructor?.ContainingType.Name is nameof(AsyncStateMachineAttribute) or nameof(AsyncIteratorStateMachineAttribute) );

        public static AsyncInfo GetAsyncInfoImpl( this IMethod method )
        {
            var isAwaitable = TryGetAsyncInfo( method.ReturnType, out var resultType, out var hasMethodBuilder );

            return new AsyncInfo( method.IsAsync, isAwaitable, resultType ?? method.ReturnType, hasMethodBuilder );
        }

        public static AsyncInfo GetAsyncInfoImpl( this IType type )
        {
            var isAwaitable = TryGetAsyncInfo( type, out var resultType, out var hasMethodBuilder );

            return new AsyncInfo( null, isAwaitable, resultType ?? type, hasMethodBuilder );
        }

        // Caches the result type of an awaitable for a type, or null if the type is not awaitable.
        private static readonly WeakCache<INamedTypeSymbol, AsyncInfoSymbol?> _cache = new( isStaticCache: true );

        /// <summary>
        /// Gets the type of the result of an awaitable.
        /// </summary>
        private static bool TryGetAsyncInfo( IType awaitableType, out IType? awaitableResultType, out bool hasMethodBuilder )
        {
            var returnTypeSymbol = awaitableType.GetSymbol();

            // Introduced types don't have symbols yet. Fall back to the code model.
            if ( returnTypeSymbol == null )
            {
                return TryGetAsyncInfoFromCodeModel( awaitableType, out awaitableResultType, out hasMethodBuilder );
            }

            if ( !TryGetAsyncInfo( returnTypeSymbol, out var resultTypeSymbol, out hasMethodBuilder ) )
            {
                awaitableResultType = null;

                return false;
            }
            else
            {
                awaitableResultType = ((CompilationModel) awaitableType.Compilation).Factory.GetIType( resultTypeSymbol );

                return true;
            }
        }

        /// <summary>
        /// Checks if a type is awaitable by inspecting the Metalama code model instead of Roslyn symbols.
        /// This is used for introduced types that don't have Roslyn symbols yet.
        /// </summary>
        private static bool TryGetAsyncInfoFromCodeModel( IType awaitableType, out IType? awaitableResultType, out bool hasMethodBuilder )
        {
            awaitableResultType = null;
            hasMethodBuilder = false;

            if ( awaitableType is not INamedType namedType )
            {
                return false;
            }

            // Check for GetAwaiter() method with no parameters (same check as the Roslyn path).
            var getAwaiterMethod = namedType.Methods.OfName( "GetAwaiter" ).FirstOrDefault( m => m.Parameters.Count == 0 );

            if ( getAwaiterMethod == null )
            {
                return false;
            }

            var awaiterType = getAwaiterMethod.ReturnType;

            if ( awaiterType is not INamedType awaiterNamedType )
            {
                return false;
            }

            // Check for parameterless GetResult() method on the awaiter (same check as the Roslyn path).
            var getResultMethod = awaiterNamedType.Methods.OfName( "GetResult" ).FirstOrDefault( m => m.Parameters.Count == 0 );

            if ( getResultMethod == null )
            {
                return false;
            }

            awaitableResultType = getResultMethod.ReturnType;

            // Check for AsyncMethodBuilderAttribute on the awaitable type.
            hasMethodBuilder = namedType.Attributes.Any( a => a.Type.Name == nameof(AsyncMethodBuilderAttribute) );

            return true;
        }

        internal static bool TryGetAsyncInfo( ITypeSymbol returnType, [NotNullWhen( true )] out ITypeSymbol? resultType, out bool hasMethodBuilder )
        {
            if ( returnType is not INamedTypeSymbol namedType )
            {
                resultType = null;
                hasMethodBuilder = false;

                return false;
            }

            // We're caching because we're always requesting the same few types.
            // ReSharper disable once InconsistentlySynchronizedField
            var cached = _cache.GetOrAdd( namedType, GetAwaitableResultTypeCore );

            if ( cached != null )
            {
                resultType = cached.ResultType;
                hasMethodBuilder = cached.HasMethodBuilder;

                return true;
            }
            else
            {
                resultType = null;
                hasMethodBuilder = false;

                return false;
            }
        }

        private static AsyncInfoSymbol? GetAwaitableResultTypeCore( INamedTypeSymbol returnType )
        {
            var getAwaiterMethod = returnType.GetMembers( "GetAwaiter" ).OfType<IMethodSymbol>().FirstOrDefault( p => p.Parameters.Length == 0 );

            if ( getAwaiterMethod == null )
            {
                return null;
            }

            // The Task type does not have any AsyncMethodBuilder attribute so they need to be marked manually.
            var isTask = returnType.OriginalDefinition.Name == nameof(Task)
                         && returnType.OriginalDefinition.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks";

            // Other types could have an AsyncMethodBuilderAttribute. 
            var hasBuilder = isTask ||
                             returnType.OriginalDefinition.GetAttributes().Any( a => a.AttributeClass?.Name == nameof(AsyncMethodBuilderAttribute) );

            var awaiterType = getAwaiterMethod.ReturnType;
            var getResultMethod = awaiterType.GetMembers( "GetResult" ).OfType<IMethodSymbol>().FirstOrDefault( p => p.Parameters.Length == 0 );

            if ( getResultMethod == null )
            {
                return null;
            }

            return new AsyncInfoSymbol( getResultMethod.ReturnType, hasBuilder );
        }

        /// <summary>
        /// Gets the return type of intermediate methods introduced by the linker or by transformations. The difficulty is that void async methods
        /// must be transformed into async methods returning a ValueType.
        /// </summary>
        public static TypeSyntax GetIntermediateMethodReturnType(
            IMethodSymbol method,
            TypeSyntax? returnTypeSyntax,
            SyntaxGenerationContext generationContext )
            => method is { IsAsync: true, ReturnsVoid: true }
                ? generationContext.SyntaxGenerator.TypeSyntax( generationContext.ReflectionMapper.GetTypeSymbol( typeof(ValueTask) ) )
                : returnTypeSyntax ?? generationContext.SyntaxGenerator.TypeSyntax( method.ReturnType );

        private sealed record AsyncInfoSymbol( ITypeSymbol ResultType, bool HasMethodBuilder );
    }
}