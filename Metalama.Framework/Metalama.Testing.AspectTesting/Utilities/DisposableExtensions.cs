// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Testing.AspectTesting.Utilities
{
    internal static class DisposableExtensions
    {
        /// <summary>
        /// Removes <c>IAsyncDisposable</c> from the resulting type, so we can use <c>using</c> instead of <c>await using</c> without a warning.
        /// </summary>
        public static IDisposable IgnoreAsyncDisposable( this IDisposable disposable ) => disposable;
    }
}