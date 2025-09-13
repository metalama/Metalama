using Metalama.Framework.Aspects;
namespace Metalama.Framework.IntegrationTests.Aspects.DesignTime.FileNameConflict
{
  // Tests that the pipeline handles types with the same full name.
  public class IntroductionAttribute : TypeAspect
  {
    [Introduce]
    public void Foo()
    {
    }
  }
  namespace X
  {
    [Introduction]
    internal partial class Y
    {
    }
    [Introduction]
    internal partial class Y<T>
    {
    }
  }
  internal partial class X<T>
  {
    [Introduction]
    private partial class Y
    {
    }
    [Introduction]
    private partial class Y<U>
    {
    }
  }
}