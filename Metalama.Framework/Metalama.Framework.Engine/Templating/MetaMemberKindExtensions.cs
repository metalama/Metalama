// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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