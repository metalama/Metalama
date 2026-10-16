public class Caller
{
  [UsingAspect]
  public TargetCode Inlined()
  {
    using (new Scope())
    {
      return new TargetCode
      {
        Value = 1
      }.WithInitialize();
    }
  }
  [UsingAspect]
  [ProceedTwiceAspect]
  public TargetCode TwoAspects()
  {
    if (Environment.ProcessorCount > 0)
    {
      return this.TwoAspects_UsingAspect();
    }
    return TwoAspects_UsingAspect();
  }
  private TargetCode TwoAspects_UsingAspect()
  {
    using (new Scope())
    {
      return new TargetCode().WithInitialize();
    }
  }
  [ProceedTwiceAspect]
  public TargetCode NotInlined()
  {
    if (Environment.ProcessorCount > 0)
    {
      return this.NotInlined_Source();
    }
    return NotInlined_Source();
  }
  private TargetCode NotInlined_Source()
  {
    return new TargetCode().WithInitialize();
  }
  [CreatingAspect]
  public void CreatedByTemplate()
  {
    _ = new TargetCode().WithInitialize();
  }
  public TargetCode NotOverridden()
  {
    return new TargetCode().WithInitialize();
  }
}