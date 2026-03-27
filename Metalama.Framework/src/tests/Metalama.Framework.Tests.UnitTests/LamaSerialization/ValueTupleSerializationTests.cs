// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using Xunit;

namespace Metalama.Framework.Tests.UnitTests.LamaSerialization
{
    public sealed class ValueTupleSerializationTests : SerializationTestsBase
    {
        [Fact]
        public void ValueTuple2_IntString()
        {
            this.TestSerialization( (42, "hello") );
        }

        [Fact]
        public void ValueTuple2_IntInt()
        {
            this.TestSerialization( (1, 2) );
        }

        [Fact]
        public void ValueTuple3_IntStringBool()
        {
            this.TestSerialization( (1, "test", true) );
        }

        [Fact]
        public void ValueTuple4()
        {
            this.TestSerialization( (1, "a", 3.14, true) );
        }

        [Fact]
        public void ValueTuple5()
        {
            this.TestSerialization( (1, 2, 3, 4, 5) );
        }

        [Fact]
        public void ValueTuple6()
        {
            this.TestSerialization( (1, 2, 3, 4, 5, 6) );
        }

        [Fact]
        public void ValueTuple7()
        {
            this.TestSerialization( (1, 2, 3, 4, 5, 6, 7) );
        }

        [Fact]
        public void ValueTuple8_LargeTuple()
        {
            // Tuples with more than 7 elements use a nested rest tuple.
            this.TestSerialization( (1, 2, 3, 4, 5, 6, 7, 8) );
        }

        [Fact]
        public void ValueTuple9_NestedRestTuple()
        {
            // Validates nested rest tuples with more than one element.
            this.TestSerialization( (1, 2, 3, 4, 5, 6, 7, 8, 9) );
        }

        [Fact]
        public void ValueTuple10_DeeperNestedRestTuple()
        {
            // Ensures the ValueTupleRestSerializer composes correctly with deeper nesting.
            this.TestSerialization( (1, 2, 3, 4, 5, 6, 7, 8, 9, 10) );
        }

        [Fact]
        public void ValueTuple2_WithNullableString()
        {
            this.TestSerialization( (42, (string?) null) );
        }

        [Fact]
        public void ValueTuple2_WithDateTime()
        {
            this.TestSerialization( (DateTime.Now, 42) );
        }

        [Fact]
        public void ValueTuple1()
        {
            this.TestSerialization( ValueTuple.Create( 42 ) );
        }

        [Fact]
        public void ValueTuple0()
        {
            this.TestSerialization( ValueTuple.Create() );
        }
    }
}
