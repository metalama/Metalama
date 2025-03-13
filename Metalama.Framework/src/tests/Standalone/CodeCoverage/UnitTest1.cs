// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#pragma warning disable CA1822 // Mark members as static

namespace CodeCoverage
{
    public class UnitTest1
    {
        [Fact]
        [InlineableMethodAspect]
        public void MethodWithInlineableAspect()
        {
            Assert.True( true );
        }

        [Fact]
        [NonInlineableMethodAspect]
        public void MethodWithNotInlineableAspect()
        {
            Assert.True( true );
        }

        [Fact]
        public void TestPropertyWithInlineableAspect()
        {
            this.PropertyWithInlineableAspect = this.PropertyWithInlineableAspect;
        }

        [Fact]
        public void TestPropertyWithNonInlineableAspect()
        {
            this.PropertyWithNonInlineableAspect = this.PropertyWithNonInlineableAspect;
        }


        [InlineablePropertyAspect]
        public int PropertyWithInlineableAspect
        {
            get {
                Assert.True( true );
                return 0; 
            }
            set
            {
                Assert.True( true );
            }
        }

        [NonInlineablePropertyAspect]
        public int PropertyWithNonInlineableAspect
        {
            get
            {
                Assert.True( true );
                return 0;
            }
            set
            {
                Assert.True( true );
            }
        }
    }

}