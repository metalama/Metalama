[FirstAspect]
[SecondAspect]
public class BaseClass
{
  public BaseClass([AspectGenerated] InitializationContext context)
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public BaseClass() : this(context: default)
  {
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
  public DerivedClass([AspectGenerated] InitializationContext context) : base(context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public DerivedClass() : this(context: default)
  {
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