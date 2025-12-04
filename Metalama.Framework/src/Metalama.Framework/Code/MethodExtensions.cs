// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;

namespace Metalama.Framework.Code
{
    /// <summary>
    /// Extension methods for the <see cref="IMethod"/> interface.
    /// </summary>
    /// <seealso cref="AsyncInfo"/>
    /// <seealso cref="IteratorInfo"/>
    /// <seealso cref="EnumerableKind"/>
    [CompileTime]
    public static class MethodExtensions
    {
        /// <summary>
        /// Gets information about whether a method is a <c>yield</c>-based iterator and returns an <see cref="IteratorInfo"/> value
        /// exposing details about the iterator, such as the <see cref="IteratorInfo.ItemType"/> and <see cref="IteratorInfo.EnumerableKind"/>.
        /// </summary>
        /// <param name="method">The method to inspect.</param>
        /// <returns>An <see cref="IteratorInfo"/> containing information about the iterator. The <see cref="IteratorInfo.EnumerableKind"/>
        /// property is set to <see cref="EnumerableKind.None"/> if the method does not return an enumerable or enumerator type.</returns>
        /// <remarks>
        /// <para>This method is useful for aspects that need to inspect iterator method properties at compile time, for example in <c>BuildAspect</c>.</para>
        /// <para>Note that <see cref="IteratorInfo.IsIteratorMethod"/> indicates whether the method uses <c>yield return</c> or <c>yield break</c>,
        /// while <see cref="IteratorInfo.EnumerableKind"/> indicates the return type regardless of the implementation.</para>
        /// </remarks>
        /// <seealso cref="IteratorInfo"/>
        /// <seealso cref="EnumerableKind"/>
        /// <seealso cref="GetAsyncInfo"/>
        /// <seealso href="@overriding-methods#async-iterator-default-template"/>
        [CompileTime]
        public static IteratorInfo GetIteratorInfo( this IMethod method ) => ((ICompilationInternal) method.Compilation).Helpers.GetIteratorInfo( method );

        /// <summary>
        /// Gets information about the async characteristics of a method, including whether it is awaitable and its result type.
        /// </summary>
        /// <param name="method">The method to inspect.</param>
        /// <returns>An <see cref="AsyncInfo"/> containing information about the async characteristics of the method,
        /// including whether it has the <c>async</c> modifier, whether its return type is awaitable, and the unwrapped result type.</returns>
        /// <remarks>
        /// <para>This method is useful for aspects that need to inspect async method properties at compile time, for example in <c>BuildAspect</c>.</para>
        /// <para>The <see cref="AsyncInfo.ResultType"/> property returns the unwrapped result type. For example, for a method returning
        /// <c>Task&lt;string&gt;</c>, the <see cref="AsyncInfo.ResultType"/> is <c>string</c>.</para>
        /// </remarks>
        /// <seealso cref="AsyncInfo"/>
        /// <seealso cref="GetIteratorInfo"/>
        /// <seealso href="@overriding-methods#async-iterator-default-template"/>
        [CompileTime]
        public static AsyncInfo GetAsyncInfo( this IMethod method ) => ((ICompilationInternal) method.Compilation).Helpers.GetAsyncInfo( method );

        /// <summary>
        /// Determines whether a method override has a covariant return type with respect to the base implementation.
        /// </summary>
        /// <param name="method">The method to check.</param>
        /// <returns><c>true</c> if the method has a covariant return type compared to its overridden method; otherwise, <c>false</c>.</returns>
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
        /// <param name="propertyOrIndexer">The property or indexer to check.</param>
        /// <returns><c>true</c> if the property or indexer has a covariant type compared to its overridden member; otherwise, <c>false</c>.</returns>
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