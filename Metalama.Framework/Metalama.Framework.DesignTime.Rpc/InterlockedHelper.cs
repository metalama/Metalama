// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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