// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Mcp.Worker;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class QueryFormatterTests
{
    [Fact]
    public void LongFluentChainGetsOneCallPerLine()
    {
        const string code =
            "workspace.SourceCode.Types.Select( t => new { Type = t, MethodCount = t.Methods.Count } ).OrderByDescending( x => x.MethodCount ).Take( 10 )";

        Assert.Equal(
            """
            workspace.SourceCode.Types
                .Select( t => new { Type = t, MethodCount = t.Methods.Count } )
                .OrderByDescending( x => x.MethodCount )
                .Take( 10 )
            """.ReplaceLineEndings( "\n" ),
            QueryFormatter.Format( code ) );
    }

    [Fact]
    public void GenericCallsAndATrailingSemicolonAreKept()
    {
        const string code = "workspace.SourceCode.Types.OfType<INamedType>().Where( t => t.IsStatic ).Select( t => t.Methods.Count );";

        Assert.Equal(
            """
            workspace.SourceCode.Types
                .OfType<INamedType>()
                .Where( t => t.IsStatic )
                .Select( t => t.Methods.Count );
            """.ReplaceLineEndings( "\n" ),
            QueryFormatter.Format( code ) );
    }

    [Fact]
    public void ReceiverThatIsACallStaysOnTheFirstLine()
    {
        const string code = "D<INamedType>( \"T:FrogHattery.BullFrog\" ).Methods.Where( m => m.Parameters.Count > 1 ).Count()";

        Assert.Equal(
            "D<INamedType>( \"T:FrogHattery.BullFrog\" ).Methods\n    .Where( m => m.Parameters.Count > 1 )\n    .Count()",
            QueryFormatter.Format( code ) );
    }

    [Theory]
    [InlineData( "workspace.SourceCode.Types.Count()" )]
    [InlineData( "workspace.SourceCode.Types\n    .Where( t => t.IsStatic )\n    .Select( t => t.Methods.Count )" )]
    [InlineData( "var types = workspace.SourceCode.Types.Where( t => t.IsStatic ).ToList(); return types.Select( t => t.Methods.Count );" )]
    public void ShortMultiLineOrStatementQueriesAreLeftAsTheyAre( string code )
    {
        // Short queries, queries already laid out, and statements, which the formatter does not handle.
        Assert.Equal( code, QueryFormatter.Format( code ) );
    }
}
