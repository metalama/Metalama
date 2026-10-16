[TheAspect]
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
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
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
    Console.WriteLine("BeforeBase1");
    Console.WriteLine("BeforeBase2");
    base.OnConstructed(context);
    Console.WriteLine("AfterBase1");
    Console.WriteLine("AfterBase2");
  }
}