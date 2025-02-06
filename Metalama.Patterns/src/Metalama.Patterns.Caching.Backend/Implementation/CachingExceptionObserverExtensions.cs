// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Implementation;

internal static class CachingExceptionObserverExtensions
{
    public static bool OnException( this ICachingExceptionObserver? exceptionObserver, Exception exception, bool affectsCacheConsistency )
    {
        if ( exceptionObserver != null )
        {
            var exceptionInfo = new CachingExceptionInfo( exception, affectsCacheConsistency );
            exceptionObserver.OnException( exceptionInfo );

            return exceptionInfo.Rethrow;
        }
        else
        {
            return false;
        }
    }
}