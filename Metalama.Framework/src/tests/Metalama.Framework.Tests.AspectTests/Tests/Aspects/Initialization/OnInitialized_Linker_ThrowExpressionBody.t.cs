public class Caller
{
  public int Property
  {
    get
    {
      throw new InvalidOperationException(new TargetCode().WithInitialize().ToString());
    }
  }
  public int this[int index]
  {
    get
    {
      throw new InvalidOperationException(new TargetCode { Value = index }.WithInitialize().ToString());
    }
  }
  public int Method()
  {
    throw new InvalidOperationException(new TargetCode().WithInitialize().ToString());
  }
  public void VoidMethod()
  {
    throw new InvalidOperationException(new TargetCode().WithInitialize().ToString());
  }
}