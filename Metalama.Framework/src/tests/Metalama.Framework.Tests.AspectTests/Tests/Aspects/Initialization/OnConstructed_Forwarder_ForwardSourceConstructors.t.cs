[TheAspect]
public class TargetCode
{
  public TargetCode([AspectGenerated] InitializationContext context)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode(string name, [AspectGenerated] InitializationContext context)
  {
    _ = name;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode() : this(context: default)
  {
  }
  public TargetCode(string name) : this(name: name, context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed TargetCode");
  }
}