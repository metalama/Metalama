// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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