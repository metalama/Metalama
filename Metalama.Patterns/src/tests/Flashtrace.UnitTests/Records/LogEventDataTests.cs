// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;
using Flashtrace.UnitTests.Assets;
using Xunit;

namespace Flashtrace.UnitTests.Records;

public sealed class LogEventDataTests
{
    [Fact]
    public void TestAnonymous()
    {
        var data = LogEventData.Create( new { Name1 = "Value", Name2 = 2 } );

        var properties = data.ToDictionary();

        Assert.Equal( 2, properties.Count );
        Assert.Equal( "Value", properties["Name1"] );
        Assert.Equal( 2, properties["Name2"] );
    }

    [Fact]
    public void TestLegacy()
    {
        var inputProperties = new[] { new LoggingProperty( "Name1", "Value" ), new LoggingProperty( "Name2", 2 ) };
        var data = new LogEventData( inputProperties );

        var properties = data.ToDictionary();

        Assert.Equal( 2, properties.Count );
        Assert.Equal( "Value", properties["Name1"] );
        Assert.Equal( 2, properties["Name2"] );
    }

    [Fact]
    public void TestWithExpressionModel()
    {
        var data = LogEventData.Create( new object(), new TestMetadata() );

        Assert.NotNull( data.GetExpressionModel<TestExpressionModel>() );
    }

    [Fact]
    public void TestAugmented()
    {
        var data = LogEventData.Create( new { Name1 = "Value", Name2 = 2 } ).WithAdditionalProperty( "Name3", 1.2 );

        var properties = data.ToDictionary();

        Assert.Equal( 3, properties.Count );
        Assert.Equal( "Value", properties["Name1"] );
        Assert.Equal( 2, properties["Name2"] );
        Assert.Equal( 1.2, properties["Name3"] );
    }

    [Fact]
    public void TestAugmentedEmpty()
    {
        var data = default(LogEventData).WithAdditionalProperty( "Name3", 1.2 );

        var properties = data.ToDictionary();

        Assert.Single( properties );
        Assert.Equal( 1.2, properties["Name3"] );
    }

    [Fact]
    public void TestAugmentedExpressionModel()
    {
        var rawData = new object();
        var data = LogEventData.Create( rawData, new TestMetadata() ).WithAdditionalProperty( "Name3", 1.2 );

        var expressionModel = data.GetExpressionModel<TestExpressionModel>();
        Assert.NotNull( expressionModel );
        Assert.Same( rawData, expressionModel.Data );
    }

    [Fact]
    public void TestAnnotatedProperties()
    {
        var o = new PropertiesWithAttributes();
        var eventData = LogEventData.Create( o );

        // Test the dynamic factory.
        Assert.Same( eventData.Metadata, LogEventData.Create( o, null ).Metadata );

        Assert.True( eventData.Metadata!.GetPropertyOptions( "Inherited" ).IsInherited );
        Assert.True( eventData.Metadata!.GetPropertyOptions( "Ignored" ).IsIgnored );

        var properties = eventData.ToDictionary();

        Assert.Single( properties );
    }
}