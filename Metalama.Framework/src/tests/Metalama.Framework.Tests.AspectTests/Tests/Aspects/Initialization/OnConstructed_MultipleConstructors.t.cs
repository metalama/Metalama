[TheAspect]
public class TargetCode
{
  public int Value { get; }
  public TargetCode([AspectGenerated] InitializationContext context) : this(0, context.Descend(InitializationSlot.OnConstructed))
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
  public TargetCode(string s, [AspectGenerated] InitializationContext context = default)
  {
    this.Value = s.Length;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public TargetCode() : this(context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed!");
  }
}