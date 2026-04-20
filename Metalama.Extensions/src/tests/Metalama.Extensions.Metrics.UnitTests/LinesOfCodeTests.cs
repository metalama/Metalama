// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Metrics;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Extensions.Metrics.UnitTests
{
    public sealed class LinesOfCodeTests : UnitTestClass
    {
        [Fact]
        public void EmptyMethod()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M1() {}
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m1 = type.Methods.OfName( "M1" ).Single();
            var loc = m1.Metrics().Get<LinesOfCode>();

            // Empty method: "void M1() {}" is a single line
            Assert.Equal( 1, loc.Logical );
            Assert.Equal( 1, loc.NonBlank );
            Assert.Equal( 1, loc.Total );
        }

        [Fact]
        public void SimpleMethod()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        var x = 0;
        x++;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Logical (braces excluded): void M(), var x = 0;, x++; = 3 lines
            Assert.Equal( 3, loc.Logical );

            // NonBlank: void M(), {, var x = 0;, x++;, } = 5 lines
            Assert.Equal( 5, loc.NonBlank );

            // Total: line span = 5 lines
            Assert.Equal( 5, loc.Total );
        }

        [Fact]
        public void SingleLineComments_Excluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        // This is a comment
        var x = 0;
        // Another comment
        x++;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Comments and braces excluded: void M(), var x = 0;, x++; = 3 lines
            Assert.Equal( 3, loc.Logical );

            // NonBlank: void M(), {, // comment, var x = 0;, // comment, x++;, } = 7 lines
            Assert.Equal( 7, loc.NonBlank );
            Assert.Equal( 7, loc.Total );
        }

        [Fact]
        public void MultiLineComments_Excluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        /* This is a
           multi-line
           comment */
        var x = 0;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Multi-line comments and braces excluded: void M(), var x = 0; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank: void M(), {, /* comment, multi-line, comment */, var x = 0;, } = 7 lines
            Assert.Equal( 7, loc.NonBlank );
            Assert.Equal( 7, loc.Total );
        }

        [Fact]
        public void XmlDocComments_Excluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    /// <summary>
    /// This is XML doc.
    /// </summary>
    void M()
    {
        var x = 0;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // XML doc comments and braces excluded: void M(), var x = 0; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank: void M(), {, var x = 0;, } = 4 lines (XML doc is leading trivia, not part of method span)
            Assert.Equal( 4, loc.NonBlank );
            Assert.Equal( 4, loc.Total );
        }

        [Fact]
        public void VerbatimString_CountsMultipleLines()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        var s = @""line1
line2
line3"";
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Method with verbatim string (braces excluded): void M(), var s = @"line1, line2, line3"; = 4 lines
            Assert.Equal( 4, loc.Logical );

            // NonBlank: void M(), {, var s = @"line1, line2, line3";, } = 6 lines
            Assert.Equal( 6, loc.NonBlank );
            Assert.Equal( 6, loc.Total );
        }

        [Fact]
        public void InterpolatedString()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        var x = 5;
        var s = $""Value is {x}"";
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Method with interpolated string (braces excluded): void M(), var x = 5;, var s = ...; = 3 lines
            Assert.Equal( 3, loc.Logical );

            // NonBlank: void M(), {, var x = 5;, var s = ...;, } = 5 lines
            Assert.Equal( 5, loc.NonBlank );
            Assert.Equal( 5, loc.Total );
        }

        [Fact]
        public void BlankLines_Excluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {

        var x = 0;

        x++;

    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Logical (braces excluded): void M(), var x = 0;, x++; = 3 lines
            Assert.Equal( 3, loc.Logical );

            // NonBlank: void M(), {, var x = 0;, x++;, } = 5 lines (blank lines excluded)
            Assert.Equal( 5, loc.NonBlank );

            // Total: line span including blank lines = 8 lines
            Assert.Equal( 8, loc.Total );
        }

        [Fact]
        public void TypeAggregation()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M1() {}
    void M2()
    {
        var x = 0;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var loc = type.Metrics().Get<LinesOfCode>();

            // Type (braces excluded): class C, void M1(), void M2(), var x = 0; = 4 lines
            Assert.Equal( 4, loc.Logical );

            // NonBlank: class C, {, void M1() {}, void M2(), {, var x = 0;, }, } = 8 lines
            Assert.Equal( 8, loc.NonBlank );
            Assert.Equal( 8, loc.Total );
        }

        [Fact]
        public void PreprocessorDirectives_InactiveBranchExcluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
#if DEBUG
        var x = 0;
#endif
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Without DEBUG defined, code inside #if DEBUG is excluded from compilation.
            // #if and #endif are trivia and not counted. Braces excluded.
            // Only: void M() = 1 line
            Assert.Equal( 1, loc.Logical );

            // NonBlank includes inactive branches: void M(), {, #if DEBUG, var x = 0;, #endif, } = 6 lines
            Assert.Equal( 6, loc.NonBlank );
            Assert.Equal( 6, loc.Total );
        }

        [Fact]
        public void PreprocessorDirectives_ActiveBranchIncluded()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
#if true
        var x = 0;
#endif
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // With #if true, the code inside is compiled.
            // #if and #endif are trivia and not counted. Braces excluded.
            // Code: void M(), var x = 0; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank: void M(), {, #if true, var x = 0;, #endif, } = 6 lines
            Assert.Equal( 6, loc.NonBlank );
            Assert.Equal( 6, loc.Total );
        }

        [Fact]
        public void PreprocessorDirectives_ElseBranch()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
#if DEBUG
        var x = 0;
#else
        var y = 1;
#endif
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Without DEBUG, only the #else branch is compiled.
            // #if, #else, #endif are trivia and not counted. Braces excluded.
            // Code: void M(), var y = 1; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank includes both branches: void M(), {, #if, var x, #else, var y, #endif, } = 8 lines
            Assert.Equal( 8, loc.NonBlank );
            Assert.Equal( 8, loc.Total );
        }

        [Fact]
        public void PreprocessorDirectives_DirectivesAreTrivia()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
#define FEATURE
class C
{
    void M()
    {
#if FEATURE
        var x = 0;
#endif
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // With FEATURE defined, the code inside is compiled.
            // #define, #if, #endif are all trivia and not counted. Braces excluded.
            // Code: void M(), var x = 0; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank: void M(), {, #if FEATURE, var x = 0;, #endif, } = 6 lines
            Assert.Equal( 6, loc.NonBlank );
            Assert.Equal( 6, loc.Total );
        }

        [Fact]
        public void TypeLevelAggregation()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    int x, y, z;

    void M1()
    {
        var a = 1;
    }

    void M2()
    {
        var b = 2;
        var c = 3;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m1 = type.Methods.OfName( "M1" ).Single();
            var m2 = type.Methods.OfName( "M2" ).Single();
            var locM1 = m1.Metrics().Get<LinesOfCode>();
            var locM2 = m2.Metrics().Get<LinesOfCode>();
            var locType = type.Metrics().Get<LinesOfCode>();

            // M1 (braces excluded): void M1(), var a = 1; = 2 lines
            Assert.Equal( 2, locM1.Logical );
            Assert.Equal( 4, locM1.NonBlank );
            Assert.Equal( 4, locM1.Total );

            // M2 (braces excluded): void M2(), var b = 2;, var c = 3; = 3 lines
            Assert.Equal( 3, locM2.Logical );
            Assert.Equal( 5, locM2.NonBlank );
            Assert.Equal( 5, locM2.Total );

            // Type (braces excluded): class C, int x y z (1 line for all 3 fields), M1 (2), M2 (3) = 7 lines
            // Multi-field declaration on same line counts as 1 line, not 3
            Assert.Equal( 7, locType.Logical );

            // NonBlank: 15 lines - 2 blank lines = 13 lines
            Assert.Equal( 13, locType.NonBlank );

            // Total = 15 lines
            Assert.Equal( 15, locType.Total );
        }

        [Fact]
        public void PartialClass_Aggregation()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
partial class C
{
    void M1()
    {
        var a = 1;
    }
}

partial class C
{
    void M2()
    {
        var b = 2;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m1 = type.Methods.OfName( "M1" ).Single();
            var m2 = type.Methods.OfName( "M2" ).Single();
            var locM1 = m1.Metrics().Get<LinesOfCode>();
            var locM2 = m2.Metrics().Get<LinesOfCode>();
            var locType = type.Metrics().Get<LinesOfCode>();

            // Each method should have its own count (braces excluded)
            Assert.Equal( 2, locM1.Logical );
            Assert.Equal( 4, locM1.NonBlank );
            Assert.Equal( 4, locM1.Total );

            Assert.Equal( 2, locM2.Logical );
            Assert.Equal( 4, locM2.NonBlank );
            Assert.Equal( 4, locM2.Total );

            // Type aggregates from both partial declarations (braces excluded):
            // partial class C, M1 (2), partial class C, M2 (2) = 6 lines
            Assert.Equal( 6, locType.Logical );

            // NonBlank: partial class C (7) + partial class C (7) = 14 lines
            Assert.Equal( 14, locType.NonBlank );
            Assert.Equal( 14, locType.Total );
        }

        [Fact]
        public void PartialMethod()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
partial class C
{
    partial void M();
}

partial class C
{
    partial void M()
    {
        var x = 1;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Partial method aggregates both parts:
            // Declaration: partial void M(); = 1 line
            // Implementation: partial void M(), var x = 1; = 2 lines (braces excluded)
            Assert.Equal( 3, loc.Logical );

            // NonBlank: declaration (1) + implementation (4) = 5 lines
            Assert.Equal( 5, loc.NonBlank );
            Assert.Equal( 5, loc.Total );
        }

        [Fact]
        public void SingleLineMultipleStatements()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    void M()
    {
        var x = 0; x++; x++;
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var m = type.Methods.OfName( "M" ).Single();
            var loc = m.Metrics().Get<LinesOfCode>();

            // Multiple statements on one line count as one line (braces excluded)
            // void M(), var x = 0; x++; x++; = 2 lines
            Assert.Equal( 2, loc.Logical );

            // NonBlank: void M(), {, var x = 0; x++; x++;, } = 4 lines
            Assert.Equal( 4, loc.NonBlank );
            Assert.Equal( 4, loc.Total );
        }

        [Fact]
        public void PropertyWithAccessors()
        {
            var services = CreateAdditionalServiceCollection( new LinesOfCodeMetricProvider() );
            using var testContext = this.CreateTestContext( services );

            const string code = @"
class C
{
    private int _x;
    public int X
    {
        get { return _x; }
        set { _x = value; }
    }
}
";

            var compilation = testContext.CreateCompilation( code );

            var type = compilation.Types.OfName( "C" ).Single();
            var prop = type.Properties.OfName( "X" ).Single();
            var loc = prop.Metrics().Get<LinesOfCode>();

            // Property (braces excluded): public int X, get return _x;, set _x = value; = 3 lines
            Assert.Equal( 3, loc.Logical );

            // NonBlank: public int X, {, get { return _x; }, set { _x = value; }, } = 5 lines
            Assert.Equal( 5, loc.NonBlank );
            Assert.Equal( 5, loc.Total );
        }
    }
}