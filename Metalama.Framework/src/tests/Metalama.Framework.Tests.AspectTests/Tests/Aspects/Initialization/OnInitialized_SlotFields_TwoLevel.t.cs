[TheAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
  {
    if (!context.IsHandled(TheAspectSlots.Slot))
    {
      Console.WriteLine("Initialized BaseClass");
    }
  }
}
public class DerivedClass : BaseClass
{
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context.Descend(TheAspectSlots.Slot));
    if (!context.IsHandled(TheAspectSlots.Slot))
    {
      Console.WriteLine("Initialized DerivedClass");
    }
  }
}