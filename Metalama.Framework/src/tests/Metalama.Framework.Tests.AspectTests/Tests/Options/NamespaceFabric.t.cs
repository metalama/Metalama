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
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(default(global::System.String))]
public class C3
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(default(global::System.String))]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute(default(global::System.String))] int p)
  {
  }
}