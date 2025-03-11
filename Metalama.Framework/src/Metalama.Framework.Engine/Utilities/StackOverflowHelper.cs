// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities;

internal static class StackOverflowHelper
{
#if DEBUG
    private static readonly ThreadLocal<int> _threadLocal = new();
#endif

    internal static Cookie Detect()
    {
#if DEBUG
        _threadLocal.Value++;

        if ( _threadLocal.Value > 32 )
        {
            throw new AssertionFailedException( "Potential infinite recursion." );
        }
#endif

        return default;
    }

    public struct Cookie : IDisposable
    {
        public void Dispose()
        {
#if DEBUG
            _threadLocal.Value--;
#endif
        }
    }
}