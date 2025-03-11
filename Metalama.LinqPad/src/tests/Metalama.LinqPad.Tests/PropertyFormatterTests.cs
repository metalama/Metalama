// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using LINQPad;
using System.Linq;
using Xunit;

namespace Metalama.LinqPad.Tests
{
    public sealed class PropertyFormatterTests
    {
        static PropertyFormatterTests()
        {
            Initializer.Initialize();
        }

        [Fact]
        public void GroupingTest()
        {
            var data = new[] { (1, 2), (1, 3) };
            var groupings = data.GroupBy( d => d.Item1 );
            var grouping = groupings.First();
            var facade = FacadePropertyFormatter.FormatPropertyValueTestable( grouping );
            Assert.IsType<DumpContainer>( facade.View );
            Assert.IsType<GroupingFacade<int, (int, int)>>( facade.ViewModel );
        }

        [Fact]
        public void EnumTest()
        {
            var facade = FacadePropertyFormatter.FormatPropertyValueTestable( Framework.Code.Accessibility.Internal );
            Assert.IsType<string>( facade.View );
        }
    }
}