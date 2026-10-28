// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Testing.UnitTesting;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel;

/// <summary>
/// Regression tests for #2303. <c>OfName</c> must find a declared member while another thread completes the collection.
/// </summary>
public sealed class OfNameConcurrencyTests : UnitTestClass
{
    private const int _memberCount = 80;
    private const int _threadCount = 16;
    private const int _iterationCount = 100;

    [Fact]
    public void NamespaceTypesOfName_ConcurrentWithEnumeration()
    {
        var code = new StringBuilder();
        code.AppendLine( "namespace N1 {" );

        for ( var i = 0; i < _memberCount; i++ )
        {
            code.AppendLine( $"  public class M{i} {{ }}" );
        }

        code.AppendLine( "}" );

        this.RunConcurrentLookups(
            code.ToString(),
            compilation =>
            {
                var types = compilation.GlobalNamespace.Namespaces.OfName( "N1" )!.Types;

                return ( name => types.OfName( name ).Any(), name => types.Any( t => t.Name == name + "Extensions" ) );
            } );
    }

    [Fact]
    public void TypeMethodsOfName_ConcurrentWithEnumeration()
    {
        var code = new StringBuilder();
        code.AppendLine( "public class C {" );

        for ( var i = 0; i < _memberCount; i++ )
        {
            code.AppendLine( $"  public void M{i}() {{ }}" );
        }

        code.AppendLine( "}" );

        this.RunConcurrentLookups(
            code.ToString(),
            compilation =>
            {
                var methods = compilation.Types.OfName( "C" ).Single().Methods;

                return ( name => methods.OfName( name ).Any(), name => methods.Any( m => m.Name == name + "Extensions" ) );
            } );
    }

    /// <summary>
    /// Looks up members named <c>M0</c> to <c>M79</c> from several threads, and enumerates the collection after each lookup.
    /// </summary>
    /// <param name="code">The source code, which must declare the members <c>M0</c> to <c>M79</c> in a single collection.</param>
    /// <param name="getOperations">A function that returns, for a compilation, a delegate that looks up a name with <c>OfName</c>,
    /// and a delegate that enumerates the same collection.</param>
    private void RunConcurrentLookups(
        string code,
        Func<ICompilation, (Func<string, bool> OfNameAny, Func<string, bool> Scan)> getOperations )
    {
        using var testContext = this.CreateTestContext();

        var roslynCompilation = testContext.CreateCSharpCompilation( code );
        var misses = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();

        for ( var iteration = 0; iteration < _iterationCount; iteration++ )
        {
            // Each iteration uses a new compilation model, so the collection starts incomplete.
            var compilation = testContext.CreateCompilationModel( roslynCompilation );
            var (ofNameAny, scan) = getOperations( compilation );

            using var barrier = new Barrier( _threadCount );

            var threads = Enumerable.Range( 0, _threadCount )
                .Select(
                    index => new Thread(
                        () =>
                        {
                            try
                            {
                                barrier.SignalAndWait();

                                for ( var i = index; i < _memberCount; i += _threadCount )
                                {
                                    var name = $"M{i}";

                                    if ( !ofNameAny( name ) )
                                    {
                                        misses.Add( name );
                                    }

                                    // Enumerating the collection completes it.
                                    _ = scan( name );
                                }
                            }
                            catch ( Exception e )
                            {
                                exceptions.Add( e );
                            }
                        } ) )
                .ToArray();

            foreach ( var thread in threads )
            {
                thread.Start();
            }

            foreach ( var thread in threads )
            {
                thread.Join();
            }

            // After all threads are done, every lookup must succeed.
            for ( var i = 0; i < _memberCount; i++ )
            {
                var name = $"M{i}";

                if ( !ofNameAny( name ) )
                {
                    misses.Add( name + " (after completion)" );
                }
            }
        }

        Assert.Empty( exceptions );
        Assert.Empty( misses );
    }
}
