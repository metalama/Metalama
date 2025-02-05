// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Engine.Diagnostics;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.Diagnostics
{
    public sealed class ValueTupleAdapterTests
    {
        [Fact]
        public void TestValueTupleAdapter()
        {
            Assert.Equal( [1, "2"], ValueTupleAdapter.ToArray( (1, "2") ) );

            Assert.Equal( [1, "2", 3, 4, 5, 6, 7, 8, 9, 10], ValueTupleAdapter.ToArray( (1, "2", 3, 4, 5, 6, 7, 8, 9, 10) ) );

            Assert.Equal(
                [1, "2", 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17],
                ValueTupleAdapter.ToArray( (1, "2", 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17) ) );
        }
    }
}