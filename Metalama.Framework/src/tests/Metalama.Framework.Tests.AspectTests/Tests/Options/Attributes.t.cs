using Metalama.Framework.Tests.AspectTests.Tests.Options;
[assembly: MyOptions("Attribute.Assembly")]
namespace Metalama.Framework.Tests.AspectTests.Tests.Options.Attributes;
[MyOptions("Attribute.C")]
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Attribute.Assembly->Attribute.C")]
public class C
{
  [ShowOptionsAspect]
  [MyOptions("Attribute.M")]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Attribute.Assembly->Attribute.C->Attribute.M")]
  public void M(int p)
  {
  }
}
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Attribute.Assembly")]
public class D
{
}