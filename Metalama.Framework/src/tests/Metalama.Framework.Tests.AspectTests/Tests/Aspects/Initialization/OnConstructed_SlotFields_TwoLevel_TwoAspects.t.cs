[FirstAspect]
[SecondAspect]
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
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    if (!context.IsHandled(Slots.SecondSlot))
    {
      Console.WriteLine("Second on BaseClass");
    }
    if (!context.IsHandled(Slots.FirstSlot))
    {
      Console.WriteLine("First on BaseClass");
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
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context.Descend(Slots.SecondSlot | Slots.FirstSlot));
    if (!context.IsHandled(Slots.SecondSlot))
    {
      Console.WriteLine("Second on DerivedClass");
    }
    if (!context.IsHandled(Slots.FirstSlot))
    {
      Console.WriteLine("First on DerivedClass");
    }
  }
}