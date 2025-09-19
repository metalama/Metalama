using System;
namespace Metalama.Framework.Tests.AspectTests.Tests.Preview.Partial_MultipleTypes;
internal partial class TargetClass
{
  private partial class NestedClass1
  {
    [TestAspect]
    public void Bar()
    {
      Console.WriteLine("Transformed");
    }
  }
  private partial class NestedClass2
  {
    [TestAspect]
    public void Bar()
    {
      Console.WriteLine("Transformed");
    }
  }
}