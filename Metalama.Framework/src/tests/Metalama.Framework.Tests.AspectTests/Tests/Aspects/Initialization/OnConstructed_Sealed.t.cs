[TheAspect]
public sealed class TargetCode
{
  public TargetCode(int value, InitializationContext context = default)
  {
    _ = value;
    this.OnConstructed(context);
  }
  private void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}