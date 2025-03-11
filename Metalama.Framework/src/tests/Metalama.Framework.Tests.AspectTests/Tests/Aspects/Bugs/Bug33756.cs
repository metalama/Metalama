// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if TEST_OPTIONS
// @IgnoredDiagnostic(CS0162)
// @IgnoredDiagnostic(CS8605)
#endif

using Metalama.Framework.Advising;
using Metalama.Framework.Aspects;
using System;
using System.Collections.Concurrent;

namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33756;

public class CacheAttribute : OverrideMethodAspect
{
    [Introduce( WhenExists = OverrideStrategy.Ignore )]
    private ConcurrentDictionary<string, object?> _cache = new();

    public override dynamic? OverrideMethod()
    {
        AroundCaching( new TemplateInvocation( nameof(CacheOrExecuteCore) ) );

        // This should be unreachable.
        return default;
    }

    [Template]
    protected virtual void AroundCaching( TemplateInvocation templateInvocation )
    {
        meta.InvokeTemplate( templateInvocation );
    }

    [Template]
    private void CacheOrExecuteCore()
    {
        // Naive implementation of a caching key.
        var cacheKey = $"{meta.Target.Method.Name}({string.Join( ", ", meta.Target.Method.Parameters.ToValueArray() )})";

        if (!_cache.TryGetValue( cacheKey, out var returnValue ))
        {
            returnValue = meta.Proceed();
            _cache.TryAdd( cacheKey, returnValue );
        }

        meta.Return( returnValue );
    }
}

public class CacheAndRetryAttribute : CacheAttribute
{
    public bool IncludeRetry { get; set; }

    protected override void AroundCaching( TemplateInvocation templateInvocation )
    {
        if (IncludeRetry)
        {
            for (var i = 0;; i++)
            {
                try
                {
                    meta.InvokeTemplate( templateInvocation );
                }
                catch (Exception ex) when (i < 10)
                {
                    Console.WriteLine( ex.ToString() );

                    continue;
                }
            }
        }
        else
        {
            meta.InvokeTemplate( templateInvocation );
        }
    }
}

// <target>
public class SelfCachedClass
{
    [Cache]
    public int Add( int a, int b ) => a + b;

    [CacheAndRetry]
    public int Rmove( int a, int b ) => a - b;
}