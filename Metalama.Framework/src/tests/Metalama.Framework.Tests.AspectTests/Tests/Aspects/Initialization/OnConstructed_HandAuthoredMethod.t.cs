[TheAspect]
public class TargetCode
{
  public TargetCode(int value, [AspectGenerated] InitializationContext context = default)
  {
    _ = value;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  // Hand-authored OnConstructed method — the aspect should inject its template into this existing method
  // rather than introducing a new one.
  public virtual void OnConstructed(InitializationContext ctx)
  {
    Console.WriteLine("User-authored.");
    Console.WriteLine("Injected!");
  }
}