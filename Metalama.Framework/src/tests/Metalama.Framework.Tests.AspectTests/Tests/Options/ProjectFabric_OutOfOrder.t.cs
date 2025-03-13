[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project")]
public class C1
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project")] int p)
  {
  }
}
[ShowOptionsAspect]
[global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
public class C2
{
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
  public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")] int p)
  {
  }
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2->M2")]
  public void M2([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2->M2")] int p)
  {
  }
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
  public int P {[ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
    get; [ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
    set; }
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
  public int F;
  [ShowOptionsAspect]
  [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->C2")]
  public event EventHandler? E;
  public class N
  {
    [ShowOptionsAspect]
    [global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->->->C2")]
    public void M([ShowOptionsAspect][global::Metalama.Framework.Tests.AspectTests.Tests.Options.ActualOptionsAttribute("->Project->->->C2")] int p)
    {
    }
  }
}