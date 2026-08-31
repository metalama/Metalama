// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Code;
using Metalama.Framework.Tests.UnitTests.Utilities;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CodeModel
{
    public sealed class AttributeDiscoveryTests : UnitTestClass
    {
        [Fact]
        public void Resolution()
        {
            using var testContext = this.CreateTestContext();

            const string code = @"
[assembly: MyAttribute(1)]
[module: MyAttribute(2)]

class MyAttribute : System.Attribute { public MyAttribute( int id ) {} }

[MyAttribute(3)]
class C< [MyAttribute(4)]T> 
{
   [MyAttribute(5)]
   [return: MyAttribute(6)]
   void M( [MyAttribute(7)] int p ) {}

   [MyAttribute(8)]
   int f, g;

   [MyAttribute(9)]
   [field: MyAttribute(10)]
   int P 
    {
        get;
        [param: MyAttribute(11)]set; 
    }

    [method: MyAttribute(12)] // Does not seem to work. Roslyn does not represent the attribute.
    string P2 => """";

    [MyAttribute(13)]
    [field: MyAttribute(14)] // Does not seem to work.  Roslyn does not represent the attribute.
    event System.EventHandler ee, ff;

}

";

            var compilation = testContext.CreateCompilationModel( code, name: "test" );
            var myAttribute = compilation.Types.OfName( "MyAttribute" ).Single();

            var targets = compilation.GetAllAttributesOfType( myAttribute )
                .OrderBy( a => a.ConstructorArguments[0].Value )
                .Select( a => a.ContainingDeclaration.ToDisplayString() + ":" + a.ConstructorArguments[0].Value )
                .ToArray();

            Assert.Equal(
                [
                    "test:1",
                    "test:2",
                    "C<T>:3",
                    "C<T>/T:4",
                    "C<T>.M(int):5",
                    "C<T>.M(int)/<return>:6",
                    "C<T>.M(int)/p:7",
                    "C<T>.f:8",
                    "C<T>.g:8",
                    "C<T>.P:9",
                    "C<T>.P.field:10",
                    "C<T>.P.set/value:11",
                    "C<T>.ee:13",
                    "C<T>.ff:13"
                ],
                targets );
        }

        /// <summary>
        /// Verifies that an attribute that the semantic model cannot bind does not abort the construction of the
        /// code model, and that the attributes of the other syntax trees are still discovered.
        /// </summary>
        /// <remarks>
        /// Issue #1858 reports that a single attribute of a single file was costing the user every design-time
        /// service of the project.
        /// </remarks>
        [Fact]
        public void AttributeThatCannotBeBoundDoesNotAbortTheCodeModel()
        {
            using var testContext = this.CreateTestContext();

            // The caller-information parameter makes Roslyn map the span of the attribute to a line while it binds
            // the attribute, and that mapping is the operation that fails on a text with an inconsistent line index.
            const string brokenCode = "using System.Runtime.CompilerServices;\r\n"
                                      + "\r\n"
                                      + "public class MyAttribute : System.Attribute\r\n"
                                      + "{\r\n"
                                      + "    public MyAttribute( [CallerLineNumber] int line = 0 ) { }\r\n"
                                      + "}\r\n"
                                      + "\r\n"
                                      + "[MyAttribute]\r\n"
                                      + "public class Broken { }\r\n";

            const string healthyCode = "[MyAttribute]\r\npublic class Healthy { }\r\n";

            var parseOptions = testContext.GetCompilationParseOptions();

            var brokenTree = CSharpSyntaxTree.ParseText(
                InconsistentLineIndexSourceText.Create( brokenCode ),
                parseOptions,
                "Broken.cs" );

            var healthyTree = CSharpSyntaxTree.ParseText( healthyCode, parseOptions, "Healthy.cs" );

            var roslynCompilation = testContext.CreateEmptyCSharpCompilation( "test" ).AddSyntaxTrees( brokenTree, healthyTree );

            var compilation = testContext.CreateCompilationModel( roslynCompilation );

            var myAttribute = compilation.Types.OfName( "MyAttribute" ).Single();

            var targets = compilation.GetAllAttributesOfType( myAttribute )
                .Select( a => a.ContainingDeclaration.ToDisplayString() )
                .OrderBy( name => name, StringComparer.Ordinal )
                .ToArray();

            // The attribute of the tree that cannot be bound is not discovered, but the attribute of the other
            // tree is.
            Assert.Equal( ["Healthy"], targets );
        }

        [Fact]
        public void GetAllAttributesOfType_Derived()
        {
            using var testContext = this.CreateTestContext();

            const string dependentCode = """
                                         public class MyAttribute : System.Attribute, System.IDisposable {  }
                                         """;

            const string mainCode = """
                                    [assembly: MyAttribute]
                                    """;

            var compilation = testContext.CreateCompilationModel( mainCode, dependentCode );
            Assert.Single( compilation.GetAllAttributesOfType( typeof(IDisposable), true ) );
        }

        [Fact]
        public void ExtensionMethodTests()
        {
            using var testContext = this.CreateTestContext();

            const string code = """
                                public class MyAttribute( System.DayOfWeek dayOfWeek ) : System.Attribute{  }

                                [MyAttribute( System.DayOfWeek.Monday )] class C;
                                """;

            var compilation = testContext.CreateCompilationModel( code );

            var attribute = compilation.Types.OfName( "C" ).Single().Attributes.Single();

            Assert.True( attribute.TryGetArgumentValue<DayOfWeek>( "DayOfWeek", out var dayOfWeek ) );
            Assert.Equal( DayOfWeek.Monday, dayOfWeek );

            Assert.True( attribute.TryGetArgumentValue<int>( "DayOfWeek", out var intDayOfWeek ) );
            Assert.Equal( (int) DayOfWeek.Monday, intDayOfWeek );

            Assert.Equal( DayOfWeek.Monday, attribute.GetArgumentValue<DayOfWeek>( "DayOfWeek" ) );

            Assert.Equal( DayOfWeek.Wednesday, attribute.GetArgumentValue( "X", DayOfWeek.Wednesday ) );
        }
    }
}