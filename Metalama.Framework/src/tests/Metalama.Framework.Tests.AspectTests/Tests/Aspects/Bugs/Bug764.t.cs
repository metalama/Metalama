public class Target
{
  public void Bar<TValue>(TestData<TValue> data)
  {
  }
  public void Baz<TValue, TExtra>(TestData<TValue> data, List<TExtra> extra)
  {
  }
  [MyAspect]
  public string Foo()
  {
    this.Bar<global::System.String>(new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug764.TestData<global::System.String>());
    this.Bar<global::System.String>(new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug764.TestData<global::System.String>());
    this.Baz<global::System.String, global::System.Int32>(new global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug764.TestData<global::System.String>(), new global::System.Collections.Generic.List<global::System.Int32>());
    return "";
  }
}
