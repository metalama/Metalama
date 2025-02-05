// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Diagnostics
{
    public static class TracingExtensions
    {
        public static ILoggerFactory GetLoggerFactory( this IServiceProvider services )
            => services.GetBackstageService<ILoggerFactory>() ?? NullLogger.Instance;
    }
}