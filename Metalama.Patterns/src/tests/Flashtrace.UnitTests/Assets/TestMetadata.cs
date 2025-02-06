// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Records;

namespace Flashtrace.UnitTests.Assets;

internal sealed class TestMetadata : LogEventMetadata<TestExpressionModel>
{
    public TestMetadata() : base( "Test" ) { }

    public override TestExpressionModel GetExpressionModel( object? data )
    {
        return new TestExpressionModel( data );
    }
}