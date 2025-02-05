internal class C
{
  [MyAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.MyAttribute("x", 1, 2, 3, 4, 5)]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.MyAttribute("x")]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Attributes.Params.YourAttribute("x")]
  private void M()
  {
  }
}