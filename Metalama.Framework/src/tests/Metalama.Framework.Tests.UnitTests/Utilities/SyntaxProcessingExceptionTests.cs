// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities.Roslyn;
using Metalama.Testing.UnitTesting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Linq;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Utilities
{
    /// <summary>
    /// Tests for the message that <see cref="SyntaxProcessingException"/> renders.
    /// </summary>
    public sealed class SyntaxProcessingExceptionTests : UnitTestClass
    {
        // The attribute is on the second line because only the positions after the edited line ending have no line
        // mapping in the text returned by InconsistentLineIndexSourceText.
        private const string _code = "// The attribute is on the next line.\r\n[My]\r\nclass Target { }\r\n";

        /// <summary>
        /// Verifies that the message names the node kind, the code, the node path, the file and the position.
        /// </summary>
        [Fact]
        public void MessageDescribesTheNode()
        {
            var exception = GetExceptionForAttributeOf( SourceText.From( _code ) );

            AssertMessageDescribesTheNode( exception.Message );
            Assert.Contains( "(2,2-2,4)", exception.Message, StringComparison.Ordinal );
        }

        /// <summary>
        /// Verifies that the message still names the node kind, the code, the node path and the file when the
        /// position of the node cannot be computed. This is the state reported by issue #1858: the failure that the
        /// exception reports also prevented the exception from describing it, so every crash report carried the same
        /// fallback text and no information about the code that caused it.
        /// </summary>
        [Fact]
        public void MessageDescribesTheNodeWhenThePositionIsNotAvailable()
        {
            var exception = GetExceptionForAttributeOf( InconsistentLineIndexSourceText.Create( _code ), expectNoPosition: true );

            AssertMessageDescribesTheNode( exception.Message );
            Assert.Contains( "the position is not available", exception.Message, StringComparison.Ordinal );
        }

        private static void AssertMessageDescribesTheNode( string message )
        {
            Assert.Contains( nameof(SyntaxKind.Attribute), message, StringComparison.Ordinal );
            Assert.Contains( "`My`", message, StringComparison.Ordinal );
            Assert.Contains( "CompilationUnit/ClassDeclaration[Target]/AttributeList/Attribute", message, StringComparison.Ordinal );
            Assert.Contains( "Target.cs", message, StringComparison.Ordinal );
            Assert.Contains( nameof(InvalidOperationException), message, StringComparison.Ordinal );
            Assert.Contains( ThrowOnAttributeWalker.ExceptionMessage, message, StringComparison.Ordinal );
        }

        private static SyntaxProcessingException GetExceptionForAttributeOf( SourceText text, bool expectNoPosition = false )
        {
            var tree = CSharpSyntaxTree.ParseText( text, path: "Target.cs" );
            var attribute = tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>().Single();

            if ( expectNoPosition )
            {
                // The test is meaningless unless the position of that specific node cannot be computed.
                Assert.Throws<ArgumentOutOfRangeException>( () => attribute.GetLocation().GetMappedLineSpan() );
            }

            return Assert.Throws<SyntaxProcessingException>( () => new ThrowOnAttributeWalker().Visit( tree.GetRoot() ) );
        }

        private sealed class ThrowOnAttributeWalker : SafeSyntaxWalker
        {
            public const string ExceptionMessage = "Simulated failure of attribute processing.";

            protected override void VisitCore( SyntaxNode? node )
            {
                if ( node is AttributeSyntax )
                {
                    throw new InvalidOperationException( ExceptionMessage );
                }

                base.VisitCore( node );
            }
        }
    }
}
