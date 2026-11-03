[InitializerAspect]
public class BaseClass
{
  public BaseClass(int count = 0, [AspectGenerated] InitializationContext context = default)
  {
    _ = count;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public BaseClass(string name, [AspectGenerated] InitializationContext context)
  {
    _ = name;
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  public BaseClass(string name) : this(name: name, context: default)
  {
  }
  protected virtual void OnConstructed(InitializationContext context = default)
  {
    Console.WriteLine("OnConstructed BaseClass");
  }
}
public class DerivedClass : BaseClass
{
  public DerivedClass(int count = 0, [AspectGenerated] InitializationContext context = default) : base(count, context: context.Descend(InitializationSlot.OnConstructed))
  {
    if (!context.IsHandled(InitializationSlot.OnConstructed))
    {
      this.OnConstructed(context);
    }
  }
  protected override void OnConstructed(InitializationContext context = default)
  {
    base.OnConstructed(context);
    Console.WriteLine("OnConstructed DerivedClass");
  }
}