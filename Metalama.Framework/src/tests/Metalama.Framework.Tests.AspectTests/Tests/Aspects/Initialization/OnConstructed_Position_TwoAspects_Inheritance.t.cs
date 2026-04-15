[FirstAspect]
[SecondAspect]
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
    Console.WriteLine("First.BeforeBase");
    Console.WriteLine("Second.BeforeBase");
    Console.WriteLine("Second.AfterBase");
    Console.WriteLine("First.AfterBase");
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
    Console.WriteLine("First.BeforeBase");
    Console.WriteLine("Second.BeforeBase");
    base.OnConstructed(context);
    Console.WriteLine("Second.AfterBase");
    Console.WriteLine("First.AfterBase");
  }
}