internal class C
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.MyAttribute("x", 1, 2, 3, 4, 5)]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.MyAttribute("x", null)]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.YourAttribute("x", null)]
  private void M()
  {
  }
}
