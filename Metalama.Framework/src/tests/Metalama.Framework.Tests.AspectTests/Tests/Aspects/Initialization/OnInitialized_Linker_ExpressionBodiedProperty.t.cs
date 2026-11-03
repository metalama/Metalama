public class Caller
{
  public TargetCode Property
  {
    get
    {
      return new TargetCode().WithInitialize();
    }
  }
  public TargetCode this[int index]
  {
    get
    {
      return new TargetCode
      {
        Value = index
      }.WithInitialize();
    }
  }
  public TargetCode Method()
  {
    return new TargetCode().WithInitialize();
  }
}