// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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