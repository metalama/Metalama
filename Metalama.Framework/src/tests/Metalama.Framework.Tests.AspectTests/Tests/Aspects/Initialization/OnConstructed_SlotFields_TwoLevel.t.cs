[TheAspect]
public class BaseClass
{
  public BaseClass(int x, [AspectGenerated] InitializationContext context = default)
  {
    _ = x;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public virtual void OnConstructed(InitializationContext context = default)
  {
    if (!context.IsHandled(TheAspectSlots.Slot))
    {
      Console.WriteLine("OnConstructed on BaseClass!");
    }
  }
}
public class DerivedClass : BaseClass
{
  public DerivedClass([AspectGenerated] InitializationContext context = default) : base(0, context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context.Descend(TheAspectSlots.Slot));
    if (!context.IsHandled(TheAspectSlots.Slot))
    {
      Console.WriteLine("OnConstructed on DerivedClass!");
    }
  }
}