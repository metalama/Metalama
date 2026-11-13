// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Workspaces;
using Metalama.Framework.Workspaces.Navigation.Links;
using Metalama.Mcp.Worker;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Mcp.Tests;

public sealed class QueryCompilerTests
{
    private static readonly QueryCompiler _compiler = new();

    private static QueryContext CreateContext()
        => new(
            null,
            ImmutableDictionary<string, Workspace>.Empty,
            null,
            new LinkResolver( new WorkspaceLinkContext( new WorkspaceCollection(), new Dictionary<string, Workspace>(), new Dictionary<string, string>(), null ) ),
            CancellationToken.None );

    [Theory]
    [InlineData( "1 + 1", true )]
    [InlineData( "1 + 1;", true )]
    [InlineData( "Enumerable.Range( 0, 3 ).Select( i => i * 2 )", true )]
    [InlineData( "var x = 1;\nreturn x;", false )]
    [InlineData( "if ( true ) { }", false )]
    public void ExpressionsAreDetected( string code, bool isExpression ) => Assert.Equal( isExpression, QueryCompiler.TryGetExpression( code, out _ ) );

    [Theory]
    [InlineData( "Console.WriteLine( 1 )" )]
    [InlineData( "Console.WriteLine( 1 );" )]
    public async Task VoidExpressionRunsAsAStatement( string code )
    {
        var compiled = _compiler.Compile( code );
        Assert.NotNull( compiled.Assembly );

        var (result, _) = await QueryRunner.RunAsync( compiled.Assembly, CreateContext(), v => v );
        Assert.Null( result );
    }

    [Fact]
    public async Task ExpressionRuns()
    {
        var compiled = _compiler.Compile( "Enumerable.Range( 1, 3 ).Sum()" );
        Assert.NotNull( compiled.Assembly );

        var (result, _) = await QueryRunner.RunAsync( compiled.Assembly, CreateContext(), v => v );
        Assert.Equal( 6, result );
    }

    [Fact]
    public async Task StatementsRun()
    {
        var compiled = _compiler.Compile( "var total = 0;\nforeach ( var i in new[] { 1, 2, 3 } ) total += i;\nreturn total;" );

        var (result, _) = await QueryRunner.RunAsync( compiled.Assembly!, CreateContext(), v => v );
        Assert.Equal( 6, result );
    }

    [Fact]
    public async Task AwaitAndTasksAreSupported()
    {
        var compiled = _compiler.Compile( "await Task.Delay( 1 );\nreturn Task.FromResult( 42 );" );

        var (result, _) = await QueryRunner.RunAsync( compiled.Assembly!, CreateContext(), v => v );
        Assert.Equal( 42, result );
    }

    [Fact]
    public void ErrorsPointAtTheQuery()
    {
        var compiled = _compiler.Compile( "var x = 1;\nreturn x.Nope;" );

        Assert.Null( compiled.Assembly );
        var error = Assert.Single( compiled.Diagnostics, d => d.Severity == "error" );
        Assert.Equal( "CS1061", error.Id );
        Assert.Equal( 2, error.Line );
        Assert.Equal( 10, error.Column );
    }

    [Fact]
    public void CompiledQueriesAreCached() => Assert.Same( _compiler.Compile( "2 + 2" ), _compiler.Compile( "2 + 2" ) );

    [Fact]
    public async Task LoadContextUnloadsAfterTheQuery()
    {
        var loadContext = await RunAndForgetAsync();

        for ( var i = 0; i < 20 && loadContext.IsAlive; i++ )
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        Assert.False( loadContext.IsAlive );
    }

    [MethodImpl( MethodImplOptions.NoInlining )]
    private static async Task<WeakReference> RunAndForgetAsync()
    {
        // The value is a projection to an anonymous type of the query assembly, rendered while the assembly is loaded.
        var compiled = _compiler.Compile( "new[] { 1, 2 }.Select( i => new { i } )" );
        var (count, loadContext) = await QueryRunner.RunAsync( compiled.Assembly!, CreateContext(), v => ((System.Collections.IEnumerable) v!).Cast<object>().Count() );
        Assert.Equal( 2, count );

        return loadContext;
    }
}
