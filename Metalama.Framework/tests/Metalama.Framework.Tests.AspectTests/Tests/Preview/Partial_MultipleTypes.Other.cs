using System;
using Metalama.Framework.Aspects;

namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_MultipleTypes;

internal partial class TargetClass
{
    partial class NestedClass2
    {
        [TestAspect]
        public void Foo() { }
    }
}