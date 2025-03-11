// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Testing;
using System.Collections.Immutable;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Diagnostics;

public sealed class ConsoleLoggerTests : TestsBase
{
    public ConsoleLoggerTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void BasicTest()
    {
        var textWriter = new StringWriter();
        var enabledCategories = ImmutableHashSet<string>.Empty.Add( "Cat1" );
        var loggerFactory = new ConsoleLoggerFactory( textWriter, enabledCategories, false );

        var logger1 = loggerFactory.GetLogger( "Cat1" );
        Assert.NotNull( logger1.Trace );
        Assert.NotNull( logger1.Warning );
        Assert.NotNull( logger1.Error );
        Assert.NotNull( logger1.Info );
        logger1.Trace!.Log( "TraceMessage" );

        var logger2 = loggerFactory.GetLogger( "Cat2" );
        Assert.Null( logger2.Trace );
        Assert.NotNull( logger2.Warning );
        Assert.NotNull( logger2.Error );
        Assert.NotNull( logger2.Info );
        logger2.Warning!.Log( "WarningMessage" );

        var text = textWriter.ToString();

        const string expected = """
                                # Metalama TRACE, Cat1: TraceMessage
                                # Metalama WARNING, Cat2: WarningMessage
                                """;

        Assert.Equal( expected.Trim(), text.Trim() );
    }
}