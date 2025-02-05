public partial class TestClass : ITestInterface
{
  event EventHandler? ITestInterface.Bar
  {
    add
    {
    }
    remove
    {
    }
  }
  [TestAspect]
  public void Foo()
  {
    ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.ExplicitInterfaceMember.ITestInterface)this).Bar += null;
    ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Events.ExplicitInterfaceMember.ITestInterface)this).Bar -= null;
    return;
  }
}