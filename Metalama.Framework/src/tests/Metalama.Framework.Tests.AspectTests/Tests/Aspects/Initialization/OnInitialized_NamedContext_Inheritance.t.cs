[TheAspect]
public class TargetCode : BaseClass
{
  public override void Initialize(global::Metalama.Framework.RunTime.Initialization.InitializationContext context = default)
  {
    base.Initialize(context);
    global::System.Console.WriteLine($"From aspect, intent={context.Intent}");
  }
}