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
