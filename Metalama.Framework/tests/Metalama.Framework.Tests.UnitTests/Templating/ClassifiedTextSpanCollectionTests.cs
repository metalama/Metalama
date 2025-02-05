// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Formatting;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Templating
{
    public sealed class ClassifiedTextSpanCollectionTests
    {
        [Fact]
        public void ZeroSpan()
        {
            var c = new ClassifiedTextSpanCollection();

            Assert.Equal( "{ [0..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void OneSpan()
        {
            var c = new ClassifiedTextSpanCollection { { new TextSpan( 0, 10 ), TextSpanClassification.TemplateKeyword } };

            Assert.Equal( "{ [0..10)=>TemplateKeyword, [10..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_Disjoint()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 0, 10 ), TextSpanClassification.TemplateKeyword }, { new TextSpan( 15, 10 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal( "{ [0..10)=>TemplateKeyword, [10..15)=>Default, [15..25)=>TemplateKeyword, [25..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_SecondOverlapping()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 0, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 5, 10 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal( "{ [0..5)=>CompileTimeVariable, [5..10)=>TemplateKeyword, [10..15)=>TemplateKeyword, [15..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_SecondSuperset()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 5, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 0, 20 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal( "{ [0..5)=>TemplateKeyword, [5..15)=>TemplateKeyword, [15..20)=>TemplateKeyword, [20..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_Subset_Inner()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 10, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 12, 3 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal(
                "{ [0..10)=>Default, [10..12)=>CompileTimeVariable, [12..15)=>TemplateKeyword, [15..20)=>CompileTimeVariable, [20..inf)=>Default } ",
                c.ToString() );
        }

        [Fact]
        public void TwoSpans_Subset_LeftAligned()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 10, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 10, 3 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal( "{ [0..10)=>Default, [10..13)=>TemplateKeyword, [13..20)=>CompileTimeVariable, [20..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_Subset_RightAligned()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 10, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 17, 3 ), TextSpanClassification.TemplateKeyword }
            };

            Assert.Equal( "{ [0..10)=>Default, [10..17)=>CompileTimeVariable, [17..20)=>TemplateKeyword, [20..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void TwoSpans_Subset_Weaker()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 10, 10 ), TextSpanClassification.CompileTimeVariable }, { new TextSpan( 12, 3 ), TextSpanClassification.Default }
            };

            Assert.Equal( "{ [0..10)=>Default, [10..20)=>CompileTimeVariable, [20..inf)=>Default } ", c.ToString() );
        }

        [Fact]
        public void OneWideSpan()
        {
            var c = new ClassifiedTextSpanCollection
            {
                { new TextSpan( 10, 10 ), TextSpanClassification.CompileTimeVariable },
                { new TextSpan( 20, 10 ), TextSpanClassification.CompileTimeVariable },
                { new TextSpan( 19, 1 ), TextSpanClassification.CompileTimeVariable, "n", "v" },
                { new TextSpan( 20, 1 ), TextSpanClassification.CompileTimeVariable, "n", "v" }
            };

            Assert.Equal(
                "{ [0..10)=>Default, [10..19)=>CompileTimeVariable, [19..20)=>CompileTimeVariable{n=v}, [20..21)=>CompileTimeVariable{n=v}, [21..30)=>CompileTimeVariable, [30..inf)=>Default } ",
                c.ToString() );
        }
    }
}