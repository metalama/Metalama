// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    private const int _typeCount = 80;
    private const int _threadCount = 16;
    private const int _iterationCount = 100;

    [Fact]
    public void NamespaceTypesOfName_ConcurrentWithEnumeration()
    {
        using var testContext = this.CreateTestContext();

        var code = new StringBuilder();
        code.AppendLine( "namespace N1 {" );

        for ( var i = 0; i < _typeCount; i++ )
        {
            code.AppendLine( $"  public class T{i} {{ }}" );
        }

        code.AppendLine( "}" );

        var roslynCompilation = testContext.CreateCSharpCompilation( code.ToString() );
        var misses = new ConcurrentBag<string>();
        var exceptions = new ConcurrentBag<Exception>();

        for ( var iteration = 0; iteration < _iterationCount; iteration++ )
        {
            // Each iteration uses a new compilation model, so the collection starts incomplete.
            var compilation = testContext.CreateCompilationModel( roslynCompilation );
            var ns = compilation.GlobalNamespace.Namespaces.OfName( "N1" )!;
            var types = ns.Types;

            using var barrier = new Barrier( _threadCount );

            var threads = Enumerable.Range( 0, _threadCount )
                .Select(
                    index => new Thread(
                        () =>
                        {
                            try
                            {
                                barrier.SignalAndWait();

                                for ( var i = index; i < _typeCount; i += _threadCount )
                                {
                                    var name = $"T{i}";

                                    if ( !types.OfName( name ).Any() )
                                    {
                                        misses.Add( name );
                                    }

                                    // Enumerating the collection completes it.
                                    _ = types.Any( t => t.Name == name + "Extensions" );
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
            for ( var i = 0; i < _typeCount; i++ )
            {
                var name = $"T{i}";

                if ( !types.OfName( name ).Any() )
                {
                    misses.Add( name + " (after completion)" );
                }
            }
        }

        Assert.Empty( exceptions );
        Assert.Empty( misses );
    }
}
