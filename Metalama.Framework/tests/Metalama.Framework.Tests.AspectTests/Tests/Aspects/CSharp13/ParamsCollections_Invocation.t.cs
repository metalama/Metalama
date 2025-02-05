[TheAspect]
public class Target
{
  public Target(params List<int> ints)
  {
  }
  void Foo(params List<int> ints)
  {
  }
  private static void M()
  {
    var value = new global::Metalama.Framework.Tests.Integration.Tests.Aspects.CSharp13.ParamsCollections_Invocation.Target(1, 2, 3);
    ((global::Metalama.Framework.Tests.Integration.Tests.Aspects.CSharp13.ParamsCollections_Invocation.Target)value).Foo(1, 2, 3);
    value.Foo(1, 2, 3);
  }
}