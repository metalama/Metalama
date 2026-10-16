[TheAspect]
public class TargetCode
{
  public TargetCode([AspectGenerated] InitializationContext context) : this(42, context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode(int value, [AspectGenerated] InitializationContext context = default)
  {
    this.Value = value;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public int Value { get; }
  public TargetCode() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed TargetCode");
  }
}