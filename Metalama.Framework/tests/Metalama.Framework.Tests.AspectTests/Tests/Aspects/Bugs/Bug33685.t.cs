internal class Target
{
  [TestAspect]
  public void Foo<T>()
  {
    this.Bar<T>((global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33685.TestData<T>)new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33685.TestData<T>());
    return;
  }
  [TestAspect2]
  public void Foo2<T>()
  {
    new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug33685.TestData<global::System.Int32>();
    return;
  }
  public void Bar<T>(TestData<T> data)
  {
  }
}