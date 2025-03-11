// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Metalama.Patterns.TestHelpers;

public static class XUnitLoggerFactoryExtensions
{
    public static ILoggingBuilder AddXUnitLogger( this ILoggingBuilder builder, ITestOutputHelper testOutputHelper )
    {
        var observer = new LogObserver();
        builder.AddProvider( new XUnitLoggerProvider( testOutputHelper, observer ) );
        builder.Services.AddSingleton( observer );

        return builder;
    }
}