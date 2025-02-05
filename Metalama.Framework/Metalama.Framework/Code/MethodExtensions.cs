// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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