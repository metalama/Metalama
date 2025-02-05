[ShowOptionsAspect]
[ModifyOptionsAspect("FromAspect.C1")]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.Project->FromAspect.C1")]
public class C1
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.Project->FromAspect.C1")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.Project->FromAspect.C1")] int p)
  {
  }
}
[ShowOptionsAspect]
[ModifyOptionsAspect("FromAspect.C2")]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
public class C2
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")] int p)
  {
  }
  [ShowOptionsAspect]
  [ModifyOptionsAspect("FromAspect.P")]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2->FromFabric.M2->FromFabric.M2->FromAspect.P")]
  public void M2([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2->FromFabric.M2->FromFabric.M2->FromAspect.P")] int p)
  {
  }
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
  public int P {[ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
    get; [ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
    set; }
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
  public int F;
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
  public event EventHandler? E;
  public class N
  {
    [ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->->->FromFabric.C2->FromFabric.C2->FromAspect.C2")]
    public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->FromFabric.Project->->->FromFabric.C2->FromFabric.C2->FromAspect.C2")] int p)
    {
    }
  }
}