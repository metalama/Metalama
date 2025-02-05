public partial class TestClass : ITestInterface
{
  int ITestInterface.Bar()
  {
    return 42;
  }
  int ITestInterface.Bar<T>()
  {
    return 42;
  }
  [TestAspect]
  public void Foo()
  {
    _ = ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ExplicitInterfaceMember.ITestInterface)this).Bar();
    ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ExplicitInterfaceMember.ITestInterface)this).Bar();
    _ = ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ExplicitInterfaceMember.ITestInterface)this).Bar<global::System.Int32>();
    ((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Invokers.Methods.ExplicitInterfaceMember.ITestInterface)this).Bar<global::System.Int32>();
    return;
  }
}