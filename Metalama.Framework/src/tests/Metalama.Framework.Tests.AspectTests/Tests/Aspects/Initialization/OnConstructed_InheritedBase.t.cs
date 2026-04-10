[TheAspect]
public class TargetCode : BaseClass
{
  public TargetCode([AspectGenerated] InitializationContext context = default) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed!");
  }
}