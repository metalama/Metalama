// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.CompileTime;
using Metalama.Testing.UnitTesting;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.CompileTime
{
    public sealed class CompileTimeCodeDetectorTests : UnitTestClass
    {
        [Fact]
        public void NotCompileTime()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( @"using Metalama.Framework.RunTime; namespace X { class Y {} } " );
            Assert.False( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void EmptyFile()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( @"" );
            Assert.False( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void InvalidFile()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                @"using Metalama.Framework.Aspects;  namespace X class Y {} ",
                ignoreErrors: true );

            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void TopLevelUsingMetalamaFramework()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( @"using Metalama.Framework; namespace X {class Y {} }" );
            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void TopLevelUsingMetalamaFrameworkAspects()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( @"using Metalama.Framework.Aspects;  namespace X {class Y {} }" );

            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void TopLevelUsingMetalamaFrameworkProjects()
        {
            using var testContext = this.CreateTestContext();
            var compilation = testContext.CreateCSharpCompilation( @"using Metalama.Framework.Fabrics; namespace X {class Y {} }" );
            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void Level1NamespaceUsing()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                @"namespace X { using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects;  class Y {} }" );

            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }

        [Fact]
        public void Level2NamespaceUsing()
        {
            using var testContext = this.CreateTestContext();

            var compilation = testContext.CreateCSharpCompilation(
                @"namespace X { namespace Y { using Metalama.Framework.Advising; 
using Metalama.Framework.Aspects;  } }" );

            Assert.True( CompileTimeCodeFastDetector.HasCompileTimeCode( compilation.SyntaxTrees.Single().GetRoot() ) );
        }
    }
}