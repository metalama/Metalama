[TheAspect]
public class TargetCode
{
  // Hand-authored InitializationContext parameter with custom name 'ctx' — the epilogue call must use this name.
  public TargetCode(int value, InitializationContext ctx = default)
  {
    _ = value;
    _ = ctx;
    if (!ctx.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(ctx);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}