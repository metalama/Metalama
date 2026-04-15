[TheAspect]
public class TargetCode : IInitializable
{
  public virtual void Initialize(InitializationContext ctx)
  {
    Console.WriteLine($"Hand-authored, intent={ctx.Intent}");
    Console.WriteLine($"From aspect, intent={ctx.Intent}");
  }
}