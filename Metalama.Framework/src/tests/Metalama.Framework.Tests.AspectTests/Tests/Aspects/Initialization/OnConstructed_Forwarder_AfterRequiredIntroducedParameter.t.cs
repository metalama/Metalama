[RequiredParameterAspect]
[InitializerAspect]
public class TargetCode
{
  public TargetCode([AspectGenerated] int p, [AspectGenerated] InitializationContext context)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode() : this(p: default, context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed TargetCode");
  }
}