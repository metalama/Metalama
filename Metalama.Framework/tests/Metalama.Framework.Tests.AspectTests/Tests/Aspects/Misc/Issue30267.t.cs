namespace Metalama.Framework.Tests.AspectTests.Tests.Aspects.Misc.Issue30267;
public class C
{
  public void M()
  {
    var x = new
    {
      x1 = new C(),
      x2 = new C()
    };
  }
}