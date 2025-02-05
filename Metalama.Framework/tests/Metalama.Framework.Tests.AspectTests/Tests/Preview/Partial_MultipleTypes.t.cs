using System;
using Metalama.Framework.Aspects;
namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_MultipleTypes;
internal partial class TargetClass
{
  partial class NestedClass1
  {
    [TestAspect]
    public void Bar()
    {
      Console.WriteLine("Transformed");
    }
  }
  partial class NestedClass2
  {
    [TestAspect]
    public void Bar()
    {
      Console.WriteLine("Transformed");
    }
  }
}