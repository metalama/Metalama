// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.Engine.Templating
{
    internal static class MetaMemberKindExtensions
    {
        public static bool IsAnyProceed( this MetaMemberKind kind )
            => kind switch
            {
                MetaMemberKind.Proceed => true,
                MetaMemberKind.ProceedAsync => true,
                MetaMemberKind.ProceedEnumerable => true,
                MetaMemberKind.ProceedEnumerator => true,
                MetaMemberKind.ProceedAsyncEnumerable => true,
                MetaMemberKind.ProceedAsyncEnumerator => true,
                _ => false
            };
    }
}