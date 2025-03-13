public partial class TestClass : ITestInterface
{
  int ITestInterface.Bar { get; set; }
  [TestAspect]
  public void Foo()
  {
    _ = ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.ExplicitInterfaceMember.ITestInterface)this).Bar;
    ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Properties.ExplicitInterfaceMember.ITestInterface)this).Bar = 42;
    return;
  }
}