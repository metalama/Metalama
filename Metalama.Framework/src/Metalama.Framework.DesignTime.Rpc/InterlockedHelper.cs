// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Framework.DesignTime.Rpc;

internal static class InterlockedHelper
{
    public static void Update<T>( ref T field, Func<T, T> setter )
        where T : class
    {
        var spin = default(SpinWait);

        while ( true )
        {
            var original = field;
            var modified = setter( original );

            if ( Interlocked.CompareExchange( ref field, modified, original ) == original )
            {
                return;
            }

            spin.SpinOnce();
        }
    }
}