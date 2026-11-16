// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Tests.UnitTests.Aspects;
using Metalama.Testing.UnitTesting;
using SharpCrafters.Aspects.Introspection.V2;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Introspection;

/// <summary>
/// Tests the introspection file (<c>.mlsym</c>) that the compile-time pipeline writes when the <c>MetalamaWriteIntrospectionFile</c>
/// property is set.
/// </summary>
public sealed class IntrospectionFileWriterTests : AspectTestBase
{
    /// <summary>
    /// The code of the tests: an aspect that overrides a method, introduces a field and reports a warning, applied by an
    /// attribute and by a fabric.
    /// </summary>
    private const string _code = """
                                 using Metalama.Framework.Advising;
                                 using Metalama.Framework.Aspects;
                                 using Metalama.Framework.Code;
                                 using Metalama.Framework.Diagnostics;
                                 using Metalama.Framework.Fabrics;
                                 using System.Linq;

                                 namespace App;

                                 public class LogAttribute : OverrideMethodAspect
                                 {
                                     private static readonly DiagnosticDefinition<IMethod> _logged = new( "APP001", Severity.Warning, "The method '{0}' is logged." );

                                     public override void BuildAspect( IAspectBuilder<IMethod> builder )
                                     {
                                         base.BuildAspect( builder );
                                         builder.Diagnostics.Report( _logged.WithArguments( builder.Target ) );
                                         builder.With( builder.Target.DeclaringType ).IntroduceField( nameof(this.Counter), whenExists: OverrideStrategy.Ignore );
                                     }

                                     [Template]
                                     private int Counter;

                                     public override dynamic? OverrideMethod() => meta.Proceed();
                                 }

                                 public class Fabric : ProjectFabric
                                 {
                                     public override void AmendProject( IProjectAmender amender )
                                         => amender.SelectMany( c => c.Types.Where( t => t.Name == "D" ) ).SelectMany( t => t.Methods ).AddAspect<LogAttribute>();
                                 }

                                 public class C
                                 {
                                     [Log]
                                     public int Add( int a, int b ) => a + b;
                                 }

                                 public class D
                                 {
                                     public void M() { }
                                 }
                                 """;

    /// <summary>
    /// Checks that the file describes the aspect instances of the code, with their predecessors, transformations and diagnostics,
    /// and that the shared reader reads it.
    /// </summary>
    [Fact]
    public async Task TheFileDescribesTheAspectInstances()
    {
        var directory = Path.Combine( Path.GetTempPath(), "Metalama", "IntrospectionFileWriterTests", Guid.NewGuid().ToString( "N" ) );
        var path = Path.Combine( directory, "App.mlsym" );

        try
        {
            using var testContext = this.CreateTestContext( CreateOptions( path ) );

            await CompileAsync( testContext, _code );

            Assert.True( File.Exists( path ) );

            var json = File.ReadAllText( path );

            // The file is indented and does not escape the characters that are safe in a file.
            Assert.Contains( "\n  \"aspectClasses\": [", json.Replace( "\r\n", "\n" ), StringComparison.Ordinal );
            Assert.DoesNotContain( "\\u0027", json, StringComparison.Ordinal );

            using var document = JsonDocument.Parse( json );
            var root = document.RootElement;

            Assert.Equal( 2, root.GetProperty( "schemaVersion" ).GetInt32() );
            Assert.Equal( "Metalama", root.GetProperty( "product" ).GetString() );
            Assert.True( root.GetProperty( "compilationSucceeded" ).GetBoolean() );

            var log = Assert.Single( root.GetProperty( "aspectClasses" ).EnumerateArray(), c => c.GetProperty( "id" ).GetString() == "T:App.LogAttribute" );
            var instances = log.GetProperty( "aspectInstances" ).EnumerateArray().ToList();
            Assert.Equal( 2, instances.Count );

            var add = Assert.Single( instances, i => i.GetProperty( "targetDeclaration" ).GetString() == "M:App.C.Add(System.Int32,System.Int32)~System.Int32" );
            var attribute = Assert.Single( add.GetProperty( "predecessors" ).EnumerateArray() );
            Assert.Equal( "Attribute", attribute.GetProperty( "kind" ).GetString() );
            Assert.Equal( "M:App.C.Add(System.Int32,System.Int32)~System.Int32", attribute.GetProperty( "declaration" ).GetString() );
            Assert.Equal( "T:App.LogAttribute", attribute.GetProperty( "attributeClass" ).GetString() );
            Assert.True( add.TryGetProperty( "location", out var location ) );
            Assert.True( location.GetProperty( "line" ).GetInt32() > 0 );

            // The method is declared on one line.
            Assert.Equal( location.GetProperty( "line" ).GetInt32(), location.GetProperty( "endLine" ).GetInt32() );

            var transformations = add.GetProperty( "transformations" ).EnumerateArray().ToList();
            Assert.Contains( transformations, t => t.GetProperty( "kind" ).GetString() == "OverrideMember" && t.GetProperty( "adviceKind" ).GetString() == "OverrideMethod" );

            Assert.Contains(
                transformations,
                t => t.GetProperty( "kind" ).GetString() == "IntroduceMember"
                     && t.GetProperty( "introducedDeclaration" ).GetString() == "F:App.C.Counter"
                     && t.GetProperty( "targetDeclaration" ).GetString() == "T:App.C" );

            var diagnostic = Assert.Single( add.GetProperty( "diagnostics" ).EnumerateArray() );
            Assert.Equal( "APP001", diagnostic.GetProperty( "id" ).GetString() );
            Assert.Equal( "Warning", diagnostic.GetProperty( "severity" ).GetString() );

            var m = Assert.Single( instances, i => i.GetProperty( "targetDeclaration" ).GetString() == "M:App.D.M" );
            var fabric = Assert.Single( m.GetProperty( "predecessors" ).EnumerateArray() );
            Assert.Equal( "Fabric", fabric.GetProperty( "kind" ).GetString() );
            Assert.Equal( "T:App.Fabric", fabric.GetProperty( "declaration" ).GetString() );

            // The shared reader reads the whole file.
            AssertReadable( path );
        }
        finally
        {
            if ( Directory.Exists( directory ) )
            {
                Directory.Delete( directory, true );
            }
        }
    }

    /// <summary>
    /// Checks that an aspect instance inherited from a base type of the same compilation has an inherited predecessor that refers
    /// to the base type, which the loader reads, and not to the aspect instance of the base type.
    /// </summary>
    [Fact]
    public async Task AnInheritedAspectInstanceRefersToItsBaseDeclaration()
    {
        const string code = """
                            using Metalama.Framework.Aspects;

                            namespace App;

                            [Inheritable]
                            public class TrackAttribute : TypeAspect { }

                            [Track]
                            public class Base { }

                            public class Derived : Base { }
                            """;

        var directory = Path.Combine( Path.GetTempPath(), "Metalama", "IntrospectionFileWriterTests", Guid.NewGuid().ToString( "N" ) );
        var path = Path.Combine( directory, "App.mlsym" );

        try
        {
            using var testContext = this.CreateTestContext( CreateOptions( path ) );

            await CompileAsync( testContext, code );

            using var document = JsonDocument.Parse( File.ReadAllText( path ) );
            var track = Assert.Single(
                document.RootElement.GetProperty( "aspectClasses" ).EnumerateArray(),
                c => c.GetProperty( "id" ).GetString() == "T:App.TrackAttribute" );
            var derived = Assert.Single( track.GetProperty( "aspectInstances" ).EnumerateArray(), i => i.GetProperty( "targetDeclaration" ).GetString() == "T:App.Derived" );
            var inherited = Assert.Single( derived.GetProperty( "predecessors" ).EnumerateArray() );

            // The loader identifies an inherited predecessor by its base declaration, not by the identifier of an aspect instance.
            Assert.Equal( "Inherited", inherited.GetProperty( "kind" ).GetString() );
            Assert.Equal( "T:App.Base", inherited.GetProperty( "declaration" ).GetString() );
            Assert.False( inherited.TryGetProperty( "aspectInstance", out _ ) );
        }
        finally
        {
            if ( Directory.Exists( directory ) )
            {
                Directory.Delete( directory, true );
            }
        }
    }

    /// <summary>
    /// Checks that no file is written when <c>MetalamaWriteIntrospectionFile</c> is not set, even when the path of the file is.
    /// </summary>
    [Fact]
    public async Task NoFileIsWrittenWhenThePropertyIsNotSet()
    {
        var directory = Path.Combine( Path.GetTempPath(), "Metalama", "IntrospectionFileWriterTests", Guid.NewGuid().ToString( "N" ) );
        var path = Path.Combine( directory, "App.mlsym" );

        using var testContext = this.CreateTestContext(
            new MetalamaTestContextOptions { Properties = ImmutableDictionary<string, string>.Empty.Add( "MetalamaIntrospectionFile", path ) } );

        await CompileAsync( testContext, _code );

        Assert.False( File.Exists( path ) );
    }

    /// <summary>
    /// Creates the options of a test context that writes the file at the given path.
    /// </summary>
    private static MetalamaTestContextOptions CreateOptions( string path )
        => new()
        {
            Properties = ImmutableDictionary<string, string>.Empty.Add( "MetalamaWriteIntrospectionFile", "True" ).Add( "MetalamaIntrospectionFile", path )
        };

    /// <summary>
    /// Asserts that the shared reader reads the whole file, and that the file holds a diagnostic.
    /// </summary>
    private static void AssertReadable( string path )
    {
        using var stream = File.OpenRead( path );
        using var reader = IntrospectionV2Reader.Create( stream );

        var kinds = new List<IntrospectionV2NodeKind>();

        while ( reader.Read() )
        {
            kinds.Add( reader.NodeKind );
        }

        Assert.Equal( kinds.Count( k => k == IntrospectionV2NodeKind.AspectInstance ), kinds.Count( k => k == IntrospectionV2NodeKind.EndAspectInstance ) );
        Assert.Contains( IntrospectionV2NodeKind.Diagnostic, kinds );
    }
}
