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
    }
}