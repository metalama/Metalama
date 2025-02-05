public class C
{
  [TheAspect]
  public void M()
  {
    object result = null;
    global::Metalama.Framework.Tests.AspectTests.Tests.Aspects.Bugs.Bug32925.ExtensionClass.ExtensionMethod(result);
    return;
  }
}