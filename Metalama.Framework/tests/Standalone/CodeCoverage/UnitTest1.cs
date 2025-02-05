// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

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