// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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