[TheAspect]
public class TargetCode : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context);
    Console.WriteLine($"From aspect, intent={context.Intent}");
  }
}