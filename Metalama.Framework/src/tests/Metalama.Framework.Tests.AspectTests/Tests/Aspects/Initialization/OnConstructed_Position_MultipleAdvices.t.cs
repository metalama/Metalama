[TheAspect]
public class BaseClass
{
  public BaseClass([AspectGenerated] InitializationContext context = default)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
  }
}
public class DerivedClass : BaseClass
{
  public DerivedClass([AspectGenerated] InitializationContext context = default) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    base.OnConstructed(context);
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
  }
}