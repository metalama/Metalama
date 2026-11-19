// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Introspection.Navigation.Telemetry;
using SharpCrafters.Backstage.Telemetry;
using SharpCrafters.Backstage.Telemetry.Metrics;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Introspection.Navigation.Tests;

/// <summary>
/// Tests the content of the usage reports of Metalama.Mcp and Metalama.LinqPad, and the selection of the exceptions that
/// they report. See metalama/Metalama#2384 and metalama/Metalama#2386.
/// </summary>
public sealed class IntrospectionTelemetryTests
{
    private static WorkspaceUsage CreateUsage( string? clientName = "claude-code", string? clientVersion = "2.1.0" )
        => new(
            IntrospectionHost.Mcp,
            [".sln", ".CSPROJ"],
            3,
            1,
            ["net8.0", "net10.0", "net8.0"],
            TimeSpan.FromMilliseconds( 1234.5 ),
            2,
            5,
            clientName,
            clientVersion );

    [Fact]
    public void MetricsDescribeTheLoad()
    {
        var metrics = new MetricCollection();

        IntrospectionTelemetry.AddMetrics( metrics, CreateUsage() );

        Assert.Equal( "Mcp", ((StringMetric) metrics["Introspection.Host"]).Value );
        Assert.Equal( "claude-code", ((StringMetric) metrics["Introspection.Client.Name"]).Value );
        Assert.Equal( "2.1.0", ((StringMetric) metrics["Introspection.Client.Version"]).Value );
        Assert.Equal( [".csproj", ".sln"], ((SetMetric) metrics["Introspection.Workspace.Kinds"]).Set.Order() );
        Assert.Equal( 3, ((Int32Metric) metrics["Introspection.Workspace.ProjectCount"]).Value );
        Assert.Equal( 1, ((Int32Metric) metrics["Introspection.Workspace.MetalamaProjectCount"]).Value );
        Assert.Equal( ["net10.0", "net8.0"], ((SetMetric) metrics["Introspection.Workspace.TargetFrameworks"]).Set.Order() );
        Assert.Equal( 1234, ((Int32Metric) metrics["Introspection.Workspace.LoadDurationMilliseconds"]).Value );
        Assert.Equal( 2, ((Int32Metric) metrics["Introspection.Workspace.ErrorCount"]).Value );
        Assert.Equal( 5, ((Int32Metric) metrics["Introspection.Workspace.WarningCount"]).Value );
    }

    [Fact]
    public void MetricsAcceptAnUnknownClient()
    {
        var metrics = new MetricCollection();

        IntrospectionTelemetry.AddMetrics( metrics, CreateUsage( null, null ) );

        Assert.Equal( "", ((StringMetric) metrics["Introspection.Client.Name"]).Value );
        Assert.Equal( "", ((StringMetric) metrics["Introspection.Client.Version"]).Value );
    }

    [Theory]
    [InlineData( null, "" )]
    [InlineData( "claude-code", "claude-code" )]
    [InlineData( "a;b=c,d", "abcd" )]
    [InlineData( "line\nbreak\ttab", "linebreaktab" )]
    [InlineData( "  padded  ", "padded" )]
    public void SanitizeRemovesTheSeparatorsOfTheReport( string? value, string expected ) => Assert.Equal( expected, IntrospectionTelemetry.Sanitize( value ) );

    [Fact]
    public void SanitizeTruncatesLongValues()
    {
        var sanitized = IntrospectionTelemetry.Sanitize( new string( 'x', 200 ) );

        Assert.Equal( IntrospectionTelemetry.MaxValueLength, sanitized.Length );
    }

    [Fact]
    public void AMissingSdkIsAttributableToTheUser()
    {
        var exception = new MSBuildInitializationException( "No .NET SDK." );

        Assert.True( IntrospectionTelemetry.IsUserAttributable( exception ) );
        Assert.True( IntrospectionTelemetry.IsUserAttributable( new AggregateException( exception ) ) );
        Assert.True( IntrospectionTelemetry.IsUserAttributable( new InvalidOperationException( "Wrapper.", exception ) ) );
    }

    [Fact]
    public void AnOrdinaryExceptionIsNotAttributableToTheUser()
        => Assert.False( IntrospectionTelemetry.IsUserAttributable( new InvalidOperationException( "A defect." ) ) );
}
