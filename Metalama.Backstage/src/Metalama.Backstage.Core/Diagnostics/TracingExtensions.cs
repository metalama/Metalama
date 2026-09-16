// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Diagnostics
{
    [PublicAPI]
    public static class TracingExtensions
    {
        public static ILoggerFactory GetLoggerFactory( this IServiceProvider services )
            => services.GetBackstageService<ILoggerFactory>() ?? NullLogger.Instance;

        public static void LogException( this ILogger? logger, in ClassifiedException classifiedException, string? caption = null )
        {
            if ( logger == null )
            {
                return;
            }

            var writer = classifiedException.IsError ? logger.Error : logger.Warning;

            if ( writer != null )
            {
                var exceptionString = classifiedException.Exception.ToString();
                var message = string.IsNullOrEmpty( caption ) ? exceptionString : caption + ": " + exceptionString;
                writer.Log( message );
            }
        }

        public static void LogException( this ILogger? logger, Exception exception, string? caption = null )
            => logger.LogException( ExceptionClassifier.Classify( exception ), caption );
    }
}