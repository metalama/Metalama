[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace")]
public class C1
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace")] int p)
  {
  }
}
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace->C2")]
public class C2
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace->C2")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Namespace->C2")] int p)
  {
  }
}
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(null)]
public class C3
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(null)]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(null)] int p)
  {
  }
}