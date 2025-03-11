// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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