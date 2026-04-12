[TheAspect]
public struct TargetCode
{
  public int Value;
  public TargetCode(int value, InitializationContext context = default)
  {
    this.Value = value;
    this.OnConstructed(context);
  }
  public TargetCode(InitializationContext context = default)
  {
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}