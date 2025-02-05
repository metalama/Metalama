internal class Foo
{
  [RegisterInstance]
  public Foo(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30662.IInstanceRegistry instanceRegistry = default(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30662.IInstanceRegistry))
  {
    instanceRegistry.Register(this);
  }
}
internal class Bar : Foo
{
  public Bar(global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30662.IInstanceRegistry instanceRegistry = null) : base(instanceRegistry)
  {
  }
}