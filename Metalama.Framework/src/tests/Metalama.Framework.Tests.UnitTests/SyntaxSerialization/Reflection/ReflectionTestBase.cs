// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Framework.Tests.UnitTests.SyntaxSerialization.Reflection
{
    public abstract class ReflectionTestBase : SerializerTestsBase
    {
        protected ReflectionTestBase( ITestOutputHelper helper ) : base( helper ) { }

        /// <summary>
        /// As <see cref="Assert.Equal{T}(T,T)"/>, except that if they are not equal, it prints the actual string on XUnit output, in verbatim string form,
        /// so that you can easily copy-paste it as the correct expected value into the test.
        /// </summary>
        /// <param name="expected">Expected value.</param>
        /// <param name="actual">Actual value from test.</param>
        protected void AssertEqual( string expected, string actual )
        {
            if ( expected == actual )
            {
                // It's fine.
            }
            else
            {
                this.TestOutput.WriteLine( "Actual result to compare against:" );
                this.TestOutput.WriteLine( "----" );
                this.TestOutput.WriteLine( "@\"" + actual.ReplaceOrdinal( "\"", "\"\"" ) + '\"' );
                this.TestOutput.WriteLine( "----" );
                Assert.Equal( expected, actual );
            }
        }
    }
}