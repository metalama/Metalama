// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Extension methods for the <see cref="IMethod"/> interface.
    /// </summary> 
    [CompileTime]
    public static class MethodExtensions
    {
        /// <summary>
        /// Determines whether a method is a <c>yield</c>-based iterator and returns an <see cref="IteratorInfo"/> value
        /// exposing details about the iterator.
        /// </summary>
        /// <param name="method"></param>
        /// <returns></returns>
        [CompileTime]
        public static IteratorInfo GetIteratorInfo( this IMethod method ) => ((ICompilationInternal) method.Compilation).Helpers.GetIteratorInfo( method );

        [CompileTime]
        public static AsyncInfo GetAsyncInfo( this IMethod method ) => ((ICompilationInternal) method.Compilation).Helpers.GetAsyncInfo( method );

        /// <summary>
        /// Determines whether a method override has a covariant return type with respect to the base implementation.
        /// </summary>
        /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/covariant-returns"/>
        [CompileTime]
        public static bool HasCovariantReturnType( this IMethod method )
        {
            return method.OverriddenMethod != null
                   && !method.ReturnType.Equals( method.OverriddenMethod.ReturnType )
                   && method.ReturnType.IsConvertibleTo( method.OverriddenMethod.ReturnType, ConversionKind.Reference );
        }
        
        /// <summary>
        /// Determines whether a read-only property or indexer override has a covariant type with respect to the base implementation.
        /// </summary>
        /// <seealso href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/covariant-returns"/> 
        [CompileTime]
        public static bool HasCovariantType( this IPropertyOrIndexer propertyOrIndexer )
        {
            return propertyOrIndexer.OverriddenMember is IPropertyOrIndexer overriddenMember
                   && !propertyOrIndexer.Type.Equals( overriddenMember.Type )
                   && propertyOrIndexer.Type.IsConvertibleTo( overriddenMember.Type, ConversionKind.Reference );
        }
    }
}