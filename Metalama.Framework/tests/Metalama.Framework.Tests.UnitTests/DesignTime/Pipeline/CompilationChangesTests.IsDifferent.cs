// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.DesignTime.Pipeline.Diff;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.DesignTime.Pipeline
{
    public sealed partial class CompilationChangesTests : UnitTestClass
    {
        private readonly DiffStrategy _strategy = new( true, true, true );
        private readonly DiffStrategy _strategyWithoutPartialTypeDetection = new( true, true, false );

        [Fact]
        public void IsDifferent_SameTrees()
        {
            var syntaxTree = CSharpSyntaxTree.ParseText( @"class C {}" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree, syntaxTree ) );
        }

        [Fact]
        public void IsDifferent_SameContent()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( @"class C {}" );
            var syntaxTree2 = CSharpSyntaxTree.ParseText( @"class C {}" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInCommentLine()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "// Comment 1\nclass C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "// Comment 2\nclass C {}" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_TypingInCommentBlock()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "/* Comment */ class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "/* Comment 2222222222 */ class C {}" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_DeletingInCommentBlock()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "/* Comment 111111111111111111 */ class C {} " );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "/* Comment */ class C {} " );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_CommentOutDeclaration()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {} " );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "// class C {}" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_UncommentDeclaration()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "// class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C {}" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_AddingWhitespace()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C {  }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_RemovingSomeWhitespace()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {   }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_RemovingAllWhitespace()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {   }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C {}" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_RemovingRequiredWhitespace()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "classC {}" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInMethodBody()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M() { return 1; } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M() { return 2; } }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInMethodExpression()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M() => 1; } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M() => 2; } }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInMethodReturnType()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M() => 1; } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { long M() => 1; } }" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInPropertyGetterBody()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M { get { return 1; } } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M { get { return 2; } } }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInPropertyGetterExpression()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M { get => 1; } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M { get => 2; } }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInPropertyExpression()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M => 1; }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M => 2; }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInPropertyInitializer()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M { get; } = 1; }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M { get; } = 2; }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInFieldInitializer()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M = 1; }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M = 2; }" );

            Assert.False( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact( Skip = "Adding aspects to local functions is not yet supported." )]
        public void IsDifferent_AddLocalFunction()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C { int M() {} }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C { int M() { void N() {] } }" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_ChangeInCompileTimeCode()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText(
                "using Metalama.Framework.Aspects;  class C { int M { get => 1; } }" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText(
                "using Metalama.Framework.Aspects;  class C { int M { get => 2; } }" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_AddPartial()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "partial class C {}" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }

        [Fact]
        public void IsDifferent_RemovePartial()
        {
            var syntaxTree1 = CSharpSyntaxTree.ParseText( "partial class C {}" );

            var syntaxTree2 = CSharpSyntaxTree.ParseText( "class C {}" );

            Assert.True( this._strategyWithoutPartialTypeDetection.IsDifferent( syntaxTree1, syntaxTree2 ) );
        }
    }
}