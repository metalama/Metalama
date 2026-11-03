[TheAspect]
public class TargetCode
{
  public TargetCode([AspectGenerated] InitializationContext context)
  {
    this.Name = "Default";
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode(string name, [AspectGenerated] InitializationContext context = default)
  {
    this.Name = name;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public string Name { get; }
  public TargetCode() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed TargetCode");
  }
}