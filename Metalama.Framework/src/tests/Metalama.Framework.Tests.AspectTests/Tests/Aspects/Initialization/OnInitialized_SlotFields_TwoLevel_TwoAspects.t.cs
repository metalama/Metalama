[FirstAspect]
[SecondAspect]
public class BaseClass : IInitializable
{
  public virtual void Initialize(InitializationContext context = default)
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
  public override void Initialize(InitializationContext context = default)
  {
    base.Initialize(context.Descend(Slots.SecondSlot | Slots.FirstSlot));
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