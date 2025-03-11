// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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